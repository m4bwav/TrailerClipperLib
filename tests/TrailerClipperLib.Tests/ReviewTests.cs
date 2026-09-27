using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;

namespace TrailerClipperLib.Tests
{
    /// <summary>
    ///     Tests for the Phase 3 review's findings (ai-docs/notes/2026-09-27-phase-3-review-findings.md), each of which the
    ///     golden replay could not catch.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class ReviewTests
    {
        private const char Backslash = (char)92;
        private const char Quote = (char)34;

        // Finding 1: ffmpeg's Duration line, which MediaToolkit parsed, rounds to the nearest hundredth.
        [TestCase(1_237_500L, 1240)]
        [TestCase(1_234_999L, 1230)]
        [TestCase(1_235_000L, 1240)]
        [TestCase(10_000_000L, 10000)]
        [TestCase(10_031_000L, 10030)]
        [TestCase(10_035_000L, 10040)]
        [TestCase(0L, 0)]
        public void Duration_is_rounded_to_hundredths_as_ffmpeg_printed_it(long microseconds, int expectedMilliseconds)
        {
            var duration = TimeSpan.FromTicks(microseconds * 10);
            Assert.That(FFmpegEngine.DurationAsMediaToolkitReadIt(duration), Is.EqualTo((decimal)expectedMilliseconds));
        }

        // Finding 2: JavaScriptSerializer escaped < > & ' and left other characters as they were.
        [Test]
        public void Config_text_escapes_what_JavaScriptSerializer_escaped()
        {
            var text = TrailerClipper.ConfigText(new[] { new TrailerClipperOptions("e&'é") { OutputDirectoryPath = "<>" } });
            string U(string code) => Backslash + "u" + code;
            Assert.That(text, Does.Contain("\"InputPath\":\"e" + U("0026") + U("0027") + "é\""));
            Assert.That(text, Does.Contain("\"OutputDirectoryPath\":\"" + U("003c") + U("003e") + "\""));
        }

        // Finding 3: config files JavaScriptSerializer accepted.
        [Test]
        public void Config_file_accepts_numbers_and_booleans_as_strings_and_trailing_commas()
        {
            var engine = new FakeEngine();
            WithMedia(dir =>
            {
                File.WriteAllText("cfg.json", "[{\"InputPath\":\"media/a.wav\",\"TrailerLengthInMilliSeconds\":\"2000\",\"RemoveIntro\":\"true\",\"IntroLengthInMilliseconds\":\"500\",},]");
                new TrailerClipper(new TrailerClipperManager(engine)).RemoveTrailersWithOptionsFile("cfg.json");
            });
            Assert.That(engine.Cuts.Single().Seek, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
            Assert.That(engine.Cuts.Single().Length, Is.EqualTo(TimeSpan.FromMilliseconds(7500)));
        }

        // Finding 9: a config file holding only null (1.1.0: NullReferenceException).
        [Test]
        public void Config_file_with_null_says_so()
        {
            WithMedia(dir =>
            {
                File.WriteAllText("cfg.json", "null");
                var e = Assert.Throws<InvalidOperationException>(() => new TrailerClipper(new TrailerClipperManager(new FakeEngine())).RemoveTrailersWithOptionsFile("cfg.json"));
                Assert.That(e!.Message, Does.StartWith("Config file holds no clipping settings"));
            });
        }

        // Finding 4: Parallel.ForEach wrapped a missing ffmpeg in AggregateException.
        [Test]
        public void Missing_ffmpeg_surfaces_as_itself_when_multitasking()
        {
            WithMedia(dir =>
            {
                var clipper = new TrailerClipper(new TrailerClipperManager(new MissingFfmpegEngine()));
                Assert.Throws<ToolNotFoundException>(() => clipper.RemoveTrailers(new TrailerClipperOptions("media", 1000m) { MultiTaskFiles = true }));
                Assert.Throws<ToolNotFoundException>(() => clipper.RemoveTrailers(new TrailerClipperOptions("media", 1000m)));
            });
        }

        // Finding 5: FFMpegCore quotes paths without escaping.
        [Test]
        public void File_names_with_a_double_quote_are_refused()
        {
            var name = "x" + Quote + " -f null y.wav";
            var e = Assert.Throws<InvalidOperationException>(() => FFmpegEngine.RefuseQuotes(name));
            Assert.That(e!.Message, Does.StartWith("File names that contain a double quote are not supported"));
            Assert.DoesNotThrow(() => FFmpegEngine.RefuseQuotes("-x 'odd' name.wav"));
        }

        // Finding 6: a relative PATH entry must never be searched.
        [Test]
        public void Relative_path_entries_are_skipped()
        {
            var old = Environment.GetEnvironmentVariable("PATH");
            try
            {
                Environment.SetEnvironmentVariable("PATH", "." + Path.PathSeparator + "relative" + Path.PathSeparator + Path.GetTempPath());
                var candidates = FFmpegLocator.CandidateDirectories().ToList();
                Assert.That(candidates, Does.Not.Contain("."));
                Assert.That(candidates, Does.Not.Contain("relative"));
                Assert.That(candidates, Does.Contain(Path.GetTempPath()));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", old);
            }
        }

        // Finding 8: 1.1.0's NumberStyles.Number, without thousands separators.
        [TestCase("2000-", -2000)]
        [TestCase(" 2000 ", 2000)]
        [TestCase("+2000", 2000)]
        public void Command_line_numbers_parse_as_1_1_0_did(string text, int expected)
        {
            Assert.That(ClipperCommandLineInterpreter.TryParseMilliseconds(text, out var value), Is.True);
            Assert.That(value, Is.EqualTo((decimal)expected));
        }

        [TestCase("1e3")]
        [TestCase("1,000")]
        [TestCase("2000,5")]
        [TestCase("")]
        public void Command_line_numbers_refused(string text)
        {
            Assert.That(ClipperCommandLineInterpreter.TryParseMilliseconds(text, out _), Is.False);
        }

        private static void WithMedia(Action<string> act)
        {
            var dir = Path.Combine(Path.GetTempPath(), "trailerclipper-review-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "media"));
            File.WriteAllText(Path.Combine(dir, "media", "a.wav"), "x");
            File.WriteAllText(Path.Combine(dir, "media", "b.mp3"), "x");
            var oldDirectory = Environment.CurrentDirectory;
            var oldOut = Console.Out;
            Environment.CurrentDirectory = dir;
            Console.SetOut(new StringWriter());
            try
            {
                act(dir);
            }
            finally
            {
                Console.SetOut(oldOut);
                Environment.CurrentDirectory = oldDirectory;
                try { Directory.Delete(dir, true); } catch (IOException) { }
            }
        }

        private sealed class MissingFfmpegEngine : IMediaEngine
        {
            public decimal GetDurationInMilliseconds(string filePath, string? ffmpegDirectory) => throw new ToolNotFoundException();

            public void Cut(string inputFilePath, string outputFilePath, TimeSpan seek, TimeSpan length, string? ffmpegDirectory) =>
                throw new ToolNotFoundException();
        }
    }

    /// <summary>Every public member of the published 1.1.0, with its parameter names, is still there (finding 7).</summary>
    [TestFixture]
    public class PublicApiTests
    {
        [Test]
        public void Every_1_1_0_member_is_still_public_with_the_same_parameter_names()
        {
            var baseline = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "PublicApi-1.1.0.txt"))
                .Where(l => l.Length > 0 && l[0] != '#')
                .ToList();
            var current = new HashSet<string>(Describe(typeof(TrailerClipper).Assembly), StringComparer.Ordinal);
            var missing = baseline.Where(l => !current.Contains(l)).ToList();
            Assert.That(missing, Is.Empty, "missing from 2.x: " + string.Join(Environment.NewLine, missing));
        }

        // The same text the PowerShell snippet printed for 1.1.0.
        private static IEnumerable<string> Describe(Assembly assembly)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    switch (member)
                    {
                        case PropertyInfo p:
                            yield return type.FullName + " property " + p.PropertyType.Name + " " + p.Name;
                            break;
                        case MethodBase m when m is ConstructorInfo || !m.IsSpecialName:
                            var kind = m is ConstructorInfo ? "constructor" : "method";
                            var parameters = string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name));
                            yield return type.FullName + " " + kind + " " + m.Name + "(" + parameters + ")";
                            break;
                    }
                }
            }
        }
    }
}
