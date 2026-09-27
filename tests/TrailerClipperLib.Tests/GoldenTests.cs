using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TrailerClipperLib.Tests
{
    /// <summary>
    ///     Replays the capture of the published 1.1.0 (tests/Golden) against this build and compares every case exactly.
    ///     The deliberate differences are the named exceptions in <see cref="Exceptions" />, and nowhere else.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class GoldenTests
    {
        private static readonly string GoldenFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "Golden");
        private static JsonArray? _actual;

        public static IEnumerable<TestCaseData> Cases()
        {
            var recorded = Recorded();
            for (var i = 0; i < recorded.Count; i++)
                yield return new TestCaseData(i).SetName("Golden " + i.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + " " + Key(recorded[i]!.AsObject()));
        }

        [OneTimeSetUp]
        public void ReplayTheCapture()
        {
            var folder = FFmpegLocator.Find() ?? throw new InvalidOperationException("The golden test needs ffmpeg and ffprobe: " + FFmpegLocator.InstallHint);
            GoldenRunner.FFmpegFolder = folder;

            var work = Path.Combine(Path.GetTempPath(), "trailerclipper-golden-" + Guid.NewGuid().ToString("N"));
            var old = Environment.CurrentDirectory;
            try
            {
                // The resolved form of the folder (on macOS the temp folder is reached through /private), so the
                // runner's <work> token replaces the paths the clipper prints.
                Directory.CreateDirectory(work);
                Environment.CurrentDirectory = work;
                work = Directory.GetCurrentDirectory();
                Environment.CurrentDirectory = old;

                _actual = JsonNode.Parse(GoldenRunner.Replay(Path.Combine(GoldenFolder, "fixtures"), work))!["cases"]!.AsArray();
            }
            finally
            {
                Environment.CurrentDirectory = old;
                try { Directory.Delete(work, true); } catch (IOException) { }
            }
        }

        [Test]
        public void Replay_has_the_recorded_cases_in_the_recorded_order()
        {
            var recorded = Recorded();
            Assert.That(_actual!.Count, Is.EqualTo(recorded.Count));
            for (var i = 0; i < recorded.Count; i++)
                Assert.That(Key(_actual[i]!.AsObject()), Is.EqualTo(Key(recorded[i]!.AsObject())), "case " + i);
        }

        [TestCaseSource(nameof(Cases))]
        public void Answer_matches_1_1_0(int index)
        {
            var expected = Recorded()[index]!.AsObject();
            var actual = _actual![index]!.AsObject();

            var key = Key(expected);
            if (Exceptions.TryGetValue(key, out var exception))
                exception.Apply(expected);
            ExactMp3Lengths(expected);

            Normalize(expected);
            Normalize(actual);

            if (expected["result"] is JsonObject r && r["$throwsStartsWith"] is JsonNode prefix)
            {
                var thrown = actual["result"]?["$throws"]?.GetValue<string>() ?? "(no exception)";
                Assert.That(thrown, Does.StartWith(SeparatorsFor(prefix.GetValue<string>())), key);
                expected["result"] = actual["result"]!.DeepClone();
            }

            Assert.That(actual.ToJsonString(), Is.EqualTo(expected.ToJsonString()), key);
        }

        [Test]
        public void Every_named_exception_matches_a_recorded_case()
        {
            var keys = new HashSet<string>(Recorded().Select(c => Key(c!.AsObject())));
            foreach (var key in Exceptions.Keys)
                Assert.That(keys, Does.Contain(key), "exception for a case that was never recorded");
        }

        // ---------- the named exceptions (the plan's E1-E10; CHANGELOG.md lists them for users) ----------

        private sealed class NamedException(string name, Action<JsonObject> apply)
        {
            public string Name { get; } = name;
            public Action<JsonObject> Apply { get; } = apply;
        }

        private static readonly Dictionary<string, NamedException> Exceptions = BuildExceptions();

        private static Dictionary<string, NamedException> BuildExceptions()
        {
            var table = new Dictionary<string, NamedException>();
            void Add(string key, string name, Action<JsonObject> apply) => table.Add(key, new NamedException(name, apply));

            static JsonObject Options(string input, string trailer, bool introOn, string intro, bool isDir) => new()
            {
                ["ClippingConfigOutputFilePath"] = null,
                ["DeleteOriginalFiles"] = false,
                ["InputPath"] = input,
                ["IntroLengthInMilliseconds"] = intro,
                ["IsInputPathADirectory"] = isDir,
                ["MultiTaskFiles"] = false,
                ["OutputClippingConfigToFile"] = false,
                ["OutputDirectoryPath"] = null,
                ["OutputToConsole"] = true,
                ["ProcessEveryFile"] = false,
                ["RemoveIntro"] = introOn,
                ["TrailerLengthInMilliSeconds"] = trailer,
            };
            static JsonObject Throws(string text) => new() { ["$throws"] = text };

            // E1: RemoveIntros and RemoveIntrosAndTrailers work on a file; a missing path throws.
            Add("RemoveIntros(file, 1500)|en-US|null", "E1", c =>
            {
                c["result"] = "(void)";
                c["console"] = "Starting on file: media/a.wav\nFinished on trimming file, output: <work>\\c{N}\\media\\clipped\\a.wav\n".Replace("{N}", CaseDir(c));
                c["files"]!["media/clipped/a.wav"] = 8500;
            });
            Add("RemoveIntrosAndTrailers(file, 1500, 2000)|en-US|null", "E1", c =>
            {
                c["result"] = "(void)";
                c["console"] = "Starting on file: media/a.wav\nFinished on trimming file, output: <work>\\c{N}\\media\\clipped\\a.wav\n".Replace("{N}", CaseDir(c));
                c["files"]!["media/clipped/a.wav"] = 6500;
            });
            Add("RemoveIntros(missing, 1500)|en-US|null", "E1", c =>
                c["result"] = Throws("FileNotFoundException: File or directory at: missing does not exist, nothing to clip."));

            // E2: RemoveTrailers on a missing path throws instead of doing nothing.
            Add("RemoveTrailers(missing, 2000)|en-US|null", "E2", c =>
                c["result"] = Throws("FileNotFoundException: File or directory at: missing does not exist, nothing to clip."));

            // E3: an intro alone on the command line, in both orders.
            foreach (var culture in new[] { "en-US", "de-DE" })
            {
                Add("ParseCommandLineArgs|" + culture + "|[\"-i\",\"1500\",\"media\"]", "E3", c =>
                {
                    c["result"] = Options("media", "0", true, "1500", true);
                    c["console"] = "";
                });
                Add("ParseCommandLineArgs|" + culture + "|[\"media\",\"-i\",\"1500\"]", "E3", c =>
                {
                    c["result"] = Options("media", "0", true, "1500", true);
                    c["console"] = "";
                });
            }

            // E4: "." is the decimal point in every culture; "2000,5" is refused.
            Add("ParseCommandLineArgs|de-DE|[\"media\",\"2000.5\"]", "E4", c => c["result"]!["TrailerLengthInMilliSeconds"] = "2000.5");
            foreach (var culture in new[] { "en-US", "de-DE" })
            {
                Add("ParseCommandLineArgs|" + culture + "|[\"media\",\"2000,5\"]", "E4", c =>
                {
                    c["result"] = null;
                    c["console"] = "Incorrect parameter, new duration in milliseconds: '2000,5' cannot be parsed\n";
                });

                // E5: an empty argument list and a bad intro length get messages, not exceptions.
                Add("ParseCommandLineArgs|" + culture + "|[]", "E5", c =>
                {
                    c["result"] = null;
                    c["console"] = "Incorrect parameters, try 'TClipper [<options>] <input_directory_path> <trailer_duration_in_milliseconds>'\n";
                });
                Add("ParseCommandLineArgs|" + culture + "|[\"-i\",\"abc\",\"media\",\"2000\"]", "E5", c =>
                {
                    c["result"] = null;
                    c["console"] = "Incorrect parameter, intro length in milliseconds: 'abc' cannot be parsed\n";
                });
            }

            // E6: a negative length is refused; a cut that leaves nothing skips the file.
            Add("RemoveTrailers(file, -1000)|en-US|null", "E6", c =>
            {
                c["result"] = Throws("ArgumentOutOfRangeException: Trailer length must not be negative: -1000 [options]");
                c["console"] = "";
                c["files"]!.AsObject().Remove("media/clipped/a.wav");
            });
            Add("RemoveTrailers(file, 12000) longer than file|en-US|null", "E6", c =>
            {
                c["console"] = "Starting on file: media/a.wav\nSkipped file, nothing is left after the cut: media/a.wav\n";
                c["files"]!.AsObject().Remove("media/clipped/a.wav");
            });

            // E7: ffmpeg's failures are reported after the batch instead of swallowed.
            Add("RemoveTrailers(options: ProcessEveryFile)|en-US|null", "E7", c =>
            {
                c["result"] = new JsonObject { ["$throwsStartsWith"] = "InvalidOperationException: ffprobe could not read 'media\\notes.txt'" };
                var console = c["console"]!.GetValue<string>();
                c["console"] = console.Substring(0, console.IndexOf("Starting on file: media\\notes.txt", StringComparison.Ordinal));
            });

            return table;
        }

        // E9: MP3 output is cut to the requested length. 1.1.0's bundled ffmpeg left the encoder's padding in the file
        // (8 033 ms for an 8 000 ms cut); a current ffmpeg writes the exact length, which is the WAV answer beside it.
        private static void ExactMp3Lengths(JsonObject c)
        {
            if (c["files"] is not JsonObject files)
                return;

            foreach (var mp3 in files.Where(f => f.Key.EndsWith(".mp3", StringComparison.Ordinal) && f.Key != "media/b.mp3").Select(f => f.Key).ToList())
            {
                var wav = mp3.Replace("b.mp3", "a.wav"); // the fixtures are a.wav and b.mp3, both 10 s
                files[mp3] = files[wav] is JsonNode wavLength ? wavLength.DeepClone() : Mp3OnlyCases[Key(c)];
            }
        }

        // Cases that clip the MP3 alone: the length it was asked for.
        private static readonly Dictionary<string, int> Mp3OnlyCases = new()
        {
            ["RemoveTrailers(file mp3, 2000)|en-US|null"] = 8000,
        };

        // ---------- normalization that is not an exception to the promise ----------

        private static void Normalize(JsonObject c)
        {
            // E10: .NET Core words argument exception messages as "message (Parameter 'x')"; .NET Framework as
            // "message\nParameter name: x". Both become "message [x]".
            if (c["result"] is JsonObject result && result["$throws"] is JsonNode thrown)
            {
                var text = thrown.GetValue<string>();
                text = Regex.Replace(text, "\nParameter name: (.+)$", " [$1]");
                text = Regex.Replace(text, " \\(Parameter '([^']+)'\\)$", " [$1]");
                result["$throws"] = SeparatorsFor(text);
            }

            // The recorded file trees are sorted by path; exceptions may add entries at the end.
            if (c["files"] is JsonObject files)
            {
                var sorted = files.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => (f.Key, Value: f.Value?.DeepClone())).ToList();
                var tree = new JsonObject();
                foreach (var (path, value) in sorted)
                    tree[path] = value;
                c["files"] = tree;
            }

            // E8: off Windows, paths are joined with "/" (1.1.0 ran only on Windows).
            if (c["console"] is JsonNode console)
                c["console"] = SeparatorsFor(console.GetValue<string>());
        }

        private static string SeparatorsFor(string text) =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? text : text.Replace('\\', '/');

        private static string CaseDir(JsonObject c)
        {
            var index = Recorded().Select((node, i) => (node, i)).First(p => Key(p.node!.AsObject()) == Key(c)).i;
            return index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string Key(JsonObject c) =>
            c["method"]!.GetValue<string>() + "|" + c["culture"]!.GetValue<string>() + "|" + (c["args"]?.ToJsonString() ?? "null");

        private static JsonArray Recorded() =>
            JsonNode.Parse(File.ReadAllText(Path.Combine(GoldenFolder, "1.1.0.json")))!["cases"]!.AsArray();
    }
}
