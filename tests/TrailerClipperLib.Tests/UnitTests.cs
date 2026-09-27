using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using NUnit.Framework;

namespace TrailerClipperLib.Tests
{
    /// <summary>A stand-in for ffmpeg: fixed durations, recorded cuts, chosen failures.</summary>
    internal sealed class FakeEngine : IMediaEngine
    {
        public decimal Duration { get; set; } = 10_000m;
        public HashSet<string> FailingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Input, string Output, TimeSpan Seek, TimeSpan Length, string? Folder)> Cuts { get; } = new();

        public decimal GetDurationInMilliseconds(string filePath, string? ffmpegDirectory)
        {
            if (FailingFiles.Contains(Path.GetFileName(filePath)))
                throw new InvalidOperationException("ffprobe could not read '" + filePath + "': fake");
            return Duration;
        }

        public void Cut(string inputFilePath, string outputFilePath, TimeSpan seek, TimeSpan length, string? ffmpegDirectory)
        {
            lock (Cuts)
                Cuts.Add((inputFilePath, outputFilePath, seek, length, ffmpegDirectory));
            File.WriteAllText(outputFilePath, "clipped");
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class ClipperTests
    {
        private string _dir = null!;
        private string _oldDirectory = null!;
        private TextWriter _oldOut = null!;
        private StringWriter _console = null!;

        [SetUp]
        public void CreateMediaFolder()
        {
            _dir = Path.Combine(Path.GetTempPath(), "trailerclipper-unit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "media"));
            foreach (var name in new[] { "a.wav", "b.mp3", "c.MP4", "notes.txt" })
                File.WriteAllText(Path.Combine(_dir, "media", name), "x");
            _oldDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _dir;
            _oldOut = Console.Out;
            _console = new StringWriter();
            Console.SetOut(_console);
        }

        [TearDown]
        public void RemoveMediaFolder()
        {
            Console.SetOut(_oldOut);
            Environment.CurrentDirectory = _oldDirectory;
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private static TrailerClipper Clipper(FakeEngine engine) => new(new TrailerClipperManager(engine));

        [Test]
        public void Cuts_each_media_file_with_the_intro_as_seek_and_the_rest_as_length()
        {
            var engine = new FakeEngine();
            Clipper(engine).RemoveIntrosAndTrailers("media", 1500m, 2000m);

            Assert.That(engine.Cuts.Select(c => Path.GetFileName(c.Input)), Is.EqualTo(new[] { "a.wav", "b.mp3", "c.MP4" }));
            Assert.That(engine.Cuts.All(c => c.Seek == TimeSpan.FromMilliseconds(1500) && c.Length == TimeSpan.FromMilliseconds(6500)), Is.True);
            Assert.That(engine.Cuts[0].Output, Is.EqualTo(Path.Combine(Path.GetFullPath("media"), "clipped", "a.wav")));
        }

        [Test]
        public void Passes_the_ffmpeg_folder_through()
        {
            var engine = new FakeEngine();
            Clipper(engine).RemoveTrailers(new TrailerClipperOptions("media/a.wav", 1000m) { FFmpegDirectory = "somewhere" });
            Assert.That(engine.Cuts.Single().Folder, Is.EqualTo("somewhere"));
        }

        [Test]
        public void Multitasking_cuts_the_same_files()
        {
            var engine = new FakeEngine();
            Clipper(engine).RemoveTrailers(new TrailerClipperOptions("media", 1000m) { MultiTaskFiles = true });
            Assert.That(engine.Cuts.Select(c => Path.GetFileName(c.Input)).OrderBy(n => n, StringComparer.Ordinal), Is.EqualTo(new[] { "a.wav", "b.mp3", "c.MP4" }));
        }

        [Test]
        public void One_failure_is_thrown_after_the_other_files_are_done()
        {
            var engine = new FakeEngine();
            engine.FailingFiles.Add("a.wav");
            var e = Assert.Throws<InvalidOperationException>(() => Clipper(engine).RemoveTrailers("media", 1000m));
            Assert.That(e!.Message, Does.StartWith("ffprobe could not read"));
            Assert.That(engine.Cuts, Has.Count.EqualTo(2));
        }

        [Test]
        public void Several_failures_are_reported_together()
        {
            var engine = new FakeEngine();
            engine.FailingFiles.Add("a.wav");
            engine.FailingFiles.Add("b.mp3");
            var e = Assert.Throws<InvalidOperationException>(() => Clipper(engine).RemoveTrailers("media", 1000m));
            Assert.That(e!.Message, Does.StartWith("2 files could not be clipped"));
            Assert.That(e.InnerException, Is.TypeOf<AggregateException>());
        }

        [Test]
        public void A_cut_that_leaves_nothing_skips_the_file_and_keeps_the_original()
        {
            var engine = new FakeEngine();
            Clipper(engine).RemoveTrailers(new TrailerClipperOptions("media/a.wav", 10_000m) { DeleteOriginalFiles = true });
            Assert.That(engine.Cuts, Is.Empty);
            Assert.That(File.Exists("media/a.wav"), Is.True);
            Assert.That(_console.ToString(), Does.Contain("Skipped file, nothing is left after the cut: media/a.wav"));
        }

        [Test]
        public void Negative_intro_is_refused_before_any_file()
        {
            var engine = new FakeEngine();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Clipper(engine).RemoveTrailers(new TrailerClipperOptions("media") { RemoveIntro = true, IntroLengthInMilliseconds = -5m }));
            Assert.That(engine.Cuts, Is.Empty);
        }

        [Test]
        public void Deletes_originals_only_when_asked()
        {
            var engine = new FakeEngine();
            Clipper(engine).RemoveTrailers(new TrailerClipperOptions("media", 1000m) { DeleteOriginalFiles = true, OutputDirectoryPath = "out" });
            Assert.That(Directory.GetFiles("media").Select(Path.GetFileName), Is.EqualTo(new[] { "notes.txt" }));
            Assert.That(Directory.GetFiles("out").Length, Is.EqualTo(3));
        }

        [Test]
        public void Config_file_round_trips_and_ignores_the_ffmpeg_folder()
        {
            var engine = new FakeEngine();
            var options = new TrailerClipperOptions("media/a.wav", 1234.5m) { RemoveIntro = true, IntroLengthInMilliseconds = 10m, FFmpegDirectory = "x" };
            Clipper(engine).RemoveTrailers(new[] { options }, "cfg.json");

            var text = File.ReadAllText("cfg.json");
            Assert.That(text, Does.Not.Contain("FFmpegDirectory"));
            Assert.That(text, Does.StartWith("[{\"OutputToConsole\":true,"));

            engine.Cuts.Clear();
            Clipper(engine).RemoveTrailersWithOptionsFile("cfg.json");
            Assert.That(engine.Cuts.Single().Length, Is.EqualTo(TimeSpan.FromMilliseconds(10_000 - 1234.5 - 10)));
        }

        [Test]
        public void Config_file_reads_property_names_in_any_case()
        {
            File.WriteAllText("cfg.json", "[{\"inputpath\":\"media/a.wav\",\"TRAILERLENGTHINMILLISECONDS\":2000}]");
            var engine = new FakeEngine();
            Clipper(engine).RemoveTrailersWithOptionsFile("cfg.json");
            Assert.That(engine.Cuts.Single().Length, Is.EqualTo(TimeSpan.FromMilliseconds(8000)));
        }

        [Test]
        public void Help_text_is_the_embedded_resource()
        {
            var help = new TcHelpReader().ReadHelpFileText();
            Assert.That(help, Does.StartWith("-h - Displays this help\r\n"));
            Assert.That(help, Does.Contain("TClipper <path> <trailer_length_in_milliseconds>"));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class ParserTests
    {
        private string _dir = null!;
        private string _oldDirectory = null!;
        private TextWriter _oldOut = null!;

        [SetUp]
        public void CreateMediaFolder()
        {
            _dir = Path.Combine(Path.GetTempPath(), "trailerclipper-parse-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "media"));
            _oldDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _dir;
            _oldOut = Console.Out;
            Console.SetOut(new StringWriter());
        }

        [TearDown]
        public void Restore()
        {
            Console.SetOut(_oldOut);
            Environment.CurrentDirectory = _oldDirectory;
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        [TestCase("fr-FR")]
        [TestCase("de-DE")]
        [TestCase("en-US")]
        public void Decimal_point_is_a_dot_in_every_culture(string culture)
        {
            var old = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            try
            {
                var options = new ClipperCommandLineInterpreter().ParseCommandLineArgs(new[] { "media", "1500.25" });
                Assert.That(options!.TrailerLengthInMilliSeconds, Is.EqualTo(1500.25m));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = old;
            }
        }

        [Test]
        public void Intro_and_trailer_together()
        {
            var options = new ClipperCommandLineInterpreter().ParseCommandLineArgs(new[] { "-intro", "3500", "-d", "media", "17500" });
            Assert.That(options!.RemoveIntro, Is.True);
            Assert.That(options.IntroLengthInMilliseconds, Is.EqualTo(3500m));
            Assert.That(options.TrailerLengthInMilliSeconds, Is.EqualTo(17500m));
            Assert.That(options.DeleteOriginalFiles, Is.True);
            Assert.That(options.InputPath, Is.EqualTo("media"));
        }

        [Test]
        public void Intro_flag_at_the_very_end_without_a_value()
        {
            Assert.That(new ClipperCommandLineInterpreter().ParseCommandLineArgs(new[] { "media", "-i" }), Is.Null);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class LocatorTests
    {
        private string _dir = null!;
        private string? _oldVariable;

        [SetUp]
        public void CreateFakeFfmpegFolder()
        {
            _dir = Path.Combine(Path.GetTempPath(), "trailerclipper-ff-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _oldVariable = Environment.GetEnvironmentVariable(FFmpegLocator.EnvironmentVariable);
        }

        [TearDown]
        public void Restore()
        {
            Environment.SetEnvironmentVariable(FFmpegLocator.EnvironmentVariable, _oldVariable);
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private void Touch(params string[] names)
        {
            foreach (var name in names)
                File.WriteAllText(Path.Combine(_dir, name), "");
        }

        [Test]
        public void Needs_both_programs()
        {
            Touch("ffmpeg.exe");
            Assert.That(FFmpegLocator.Find(_dir), Is.Null);
            Touch("ffprobe.exe");
            Assert.That(FFmpegLocator.Find(_dir), Is.EqualTo(Path.GetFullPath(_dir)));
        }

        [Test]
        public void Accepts_names_without_exe()
        {
            Touch("ffmpeg", "ffprobe");
            Assert.That(FFmpegLocator.Find(_dir), Is.Not.Null);
        }

        [Test]
        public void A_given_folder_without_ffmpeg_is_not_replaced_by_a_search()
        {
            Assert.That(FFmpegLocator.Find(_dir), Is.Null);
        }

        [Test]
        public void Environment_variable_comes_first()
        {
            Environment.SetEnvironmentVariable(FFmpegLocator.EnvironmentVariable, _dir);
            Assert.That(FFmpegLocator.CandidateDirectories().First(), Is.EqualTo(_dir));
            Touch("ffmpeg", "ffprobe");
            Assert.That(FFmpegLocator.Find(), Is.EqualTo(_dir));
        }

        [Test]
        public void Missing_ffmpeg_throws_the_install_hint()
        {
            var engine = new FFmpegEngine();
            var e = Assert.Throws<ToolNotFoundException>(() => engine.GetDurationInMilliseconds("x.wav", _dir));
            Assert.That(e!.Message, Does.Contain("winget install --id Gyan.FFmpeg -e"));
            Assert.That(e.Message, Does.Contain("tclipper --install-ffmpeg"));
        }
    }

    [TestFixture]
    public class ConfigTextTests
    {
        [Test]
        public void Sample_config_text_is_1_1_0s()
        {
            // The property order and number format 1.1.0's JavaScriptSerializer wrote (tests/Golden records the file).
            var json = JsonSerializer.Serialize(new[] { new TrailerClipperOptions("x", 17500m) });
            Assert.That(json, Does.Contain("\"TrailerLengthInMilliSeconds\":17500,"));
        }
    }
}
