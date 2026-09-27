// Golden capture of the PUBLISHED TrailerClipper 1.1.0 (package-modernize, Phase 0), adapted from
// scripts/golden-capture-nuget.template.cs. 1.1.0 is a net40 assembly that needs System.Web.Extensions,
// so this runs on net48, not net10.0.
//
// Run from the capture folder:  dotnet run -c Release -- <fixtures dir> > 1.1.0.json
// Fixtures (made with ffmpeg 9.0.1, not by 1.1.0): a.wav (10 s, 44.1 kHz mono sine), b.mp3 (10 s, 64 kb/s), notes.txt.
// Every case runs in a fresh copy of the fixtures under work/<case>, with the current directory set there, so the
// recorded paths are relative. After a clipping case the file tree is recorded with each media file's duration from
// ffprobe (on PATH), rounded to milliseconds. Console output of each case is recorded too.
// A throwing call records {"$throws": "Type: message"} (net48 messages; .NET Core words ArgumentException messages differently).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using TrailerClipperLib;

internal static class Program
{
    private static string _fixtures;
    private static string _workRoot;
    private static readonly List<Dictionary<string, object>> Cases = new List<Dictionary<string, object>>();

    private static int Main(string[] argv)
    {
        _fixtures = Path.GetFullPath(argv[0]);
        _workRoot = Path.GetFullPath("work");
        if (Directory.Exists(_workRoot)) Directory.Delete(_workRoot, true);
        Directory.CreateDirectory(_workRoot);

        foreach (var culture in new[] { "en-US", "de-DE" })
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            CliCases(culture);
        }
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

        OptionsCases();
        HelpCases();
        ClipperCases();

        var doc = new Dictionary<string, object>
        {
            ["package"] = "TrailerClipper@1.1.0",
            ["runtime"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ["captured"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["ffprobe"] = Run("ffprobe", "-version").Split('\n')[0].Trim(),
            ["note"] = "Golden outputs of the published TrailerClipper 1.1.0 (bundles MediaToolkit 1.0.4.11 and its ffmpeg). See Capture/Program.cs.",
            ["cases"] = Cases
        };
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        Console.Out.Write(json.Replace("\r\n", "\n") + "\n");
        return 0;
    }

    // ---------- command line interpreter ----------
    private static void CliCases(string culture)
    {
        var argSets = new[]
        {
            null,
            new string[0],
            new[] { "-z" },
            new[] { "media" },
            new[] { "media", "2000" },
            new[] { "media/a.wav", "2000" },
            new[] { "media", "2000.5" },
            new[] { "media", "2000,5" },
            new[] { "media", "-2000" },
            new[] { "media", "abc" },
            new[] { "missing", "2000" },
            new[] { "-d", "-m", "-cf", "-a", "media", "2000" },
            new[] { "-D", " -M ", "-consoleoff", "-allfiles", "-multi", "media", "2000" },
            new[] { "-i", "1500", "media", "2000" },
            new[] { "-intro", "1500", "media", "2000" },
            new[] { "-i", "1500", "media" },
            new[] { "media", "-i", "1500" },
            new[] { "-i", "abc", "media", "2000" },
            new[] { "-i", "0", "media" },
            new[] { "-o", "cfg.json", "media", "2000" },
            new[] { "-x", "media", "2000" },
            new[] { "-z", "media", "2000" },
        };
        foreach (var args in argSets)
        {
            var i = new ClipperCommandLineInterpreter();
            Case("ParseCommandLineArgs", culture, args, () => Dump(i.ParseCommandLineArgs(args)));
        }

        var small = new[]
        {
            null, new string[0], new[] { "-c" }, new[] { "-c", "cfg.json" }, new[] { " -CONFIG ", "x.json", "more" },
            new[] { "media", "-c", "cfg.json" }, new[] { "-o", "out.json", "media", "2000" }, new[] { "media", "-O", "Out.JSON" },
            new[] { "-o" }, new[] { "-h" }, new[] { "media", "-HELP" }, new[] { "--help" }, new[] { "-z" }, new[] { " -Z " }
        };
        if (culture != "en-US") return;
        foreach (var args in small)
        {
            var i = new ClipperCommandLineInterpreter();
            Case("ReadConfigFilePath", culture, args, () => i.ReadConfigFilePath(args));
            Case("ShouldDumpConfigFile", culture, args, () => i.ShouldDumpConfigFile(args));
            Case("ReadConfigOutputPath", culture, args, () => i.ReadConfigOutputPath(args));
            Case("ShouldDisplayHelp", culture, args, () => i.ShouldDisplayHelp(args));
            Case("ShouldCreateDefaultSampleConfig", culture, args, () => i.ShouldCreateDefaultSampleConfig(args));
        }
    }

    // ---------- options ----------
    private static void OptionsCases()
    {
        Case("new TrailerClipperOptions()", "en-US", null, () => Dump(new TrailerClipperOptions()));
        Case("new TrailerClipperOptions(path)", "en-US", new[] { "media" }, () => Dump(new TrailerClipperOptions("media")));
        Case("new TrailerClipperOptions(path, ms)", "en-US", new[] { "media/a.wav", "2000" }, () => Dump(new TrailerClipperOptions("media/a.wav", 2000m)));
        Case("new TrailerClipperOptions(null)", "en-US", new string[] { null }, () => Dump(new TrailerClipperOptions(null)));
        Case("new TrailerClipperOptions(missing)", "en-US", new[] { "missing" }, () => Dump(new TrailerClipperOptions("missing")));
    }

    private static void HelpCases()
    {
        Case("TcHelpReader.ReadHelpFileText", "en-US", null, () => new TcHelpReader().ReadHelpFileText());
    }

    // ---------- clipping ----------
    private static void ClipperCases()
    {
        Clip("RemoveTrailers(file, 2000)", c => c.RemoveTrailers("media/a.wav", 2000m));
        Clip("RemoveTrailers(file mp3, 2000)", c => c.RemoveTrailers("media/b.mp3", 2000m));
        Clip("RemoveTrailers(dir, 2000)", c => c.RemoveTrailers("media", 2000m));
        Clip("RemoveTrailers(dir, 0)", c => c.RemoveTrailers("media", 0m));
        Clip("RemoveTrailers(dir, 2000.5)", c => c.RemoveTrailers("media", 2000.5m));
        Clip("RemoveTrailers(file, 12000) longer than file", c => c.RemoveTrailers("media/a.wav", 12000m));
        Clip("RemoveTrailers(file, -1000)", c => c.RemoveTrailers("media/a.wav", -1000m));
        Clip("RemoveTrailers(missing, 2000)", c => c.RemoveTrailers("missing", 2000m));
        Clip("RemoveTrailers((TrailerClipperOptions)null)", c => c.RemoveTrailers((TrailerClipperOptions)null));
        Clip("RemoveTrailers((IEnumerable)null, false)", c => c.RemoveTrailers((IEnumerable<TrailerClipperOptions>)null, false));
        Clip("RemoveIntros(file, 1500)", c => c.RemoveIntros("media/a.wav", 1500m));
        Clip("RemoveIntros(dir, 1500)", c => c.RemoveIntros("media", 1500m));
        Clip("RemoveIntros(dir, -1)", c => c.RemoveIntros("media", -1m));
        Clip("RemoveIntros(empty, 1500)", c => c.RemoveIntros("", 1500m));
        Clip("RemoveIntros(missing, 1500)", c => c.RemoveIntros("missing", 1500m));
        Clip("RemoveIntrosAndTrailers(dir, 1500, 2000)", c => c.RemoveIntrosAndTrailers("media", 1500m, 2000m));
        Clip("RemoveIntrosAndTrailers(file, 1500, 2000)", c => c.RemoveIntrosAndTrailers("media/a.wav", 1500m, 2000m));
        Clip("RemoveTrailers(options: out dir, delete originals)", c => c.RemoveTrailers(new TrailerClipperOptions("media", 2000m) { OutputDirectoryPath = "out", DeleteOriginalFiles = true }));
        Clip("RemoveTrailers(options: ProcessEveryFile)", c => c.RemoveTrailers(new TrailerClipperOptions("media", 2000m) { ProcessEveryFile = true }));
        Clip("RemoveTrailers(options: MultiTaskFiles, console off)", c => c.RemoveTrailers(new TrailerClipperOptions("media", 2000m) { MultiTaskFiles = true, OutputToConsole = false }));
        Clip("RemoveTrailers(options, saveOptionsToFile true)", c => c.RemoveTrailers(new[] { new TrailerClipperOptions("media/a.wav", 2000m) }, true));
        Clip("RemoveTrailers(options, configFileOutputPath)", c => c.RemoveTrailers(new[] { new TrailerClipperOptions("media/a.wav", 2000m) { RemoveIntro = true, IntroLengthInMilliseconds = 1000m } }, "cfg.json"));
        Clip("RemoveTrailersWithOptionsFile(null) no default file", c => c.RemoveTrailersWithOptionsFile());
        Clip("RemoveTrailersWithOptionsFile(missing)", c => c.RemoveTrailersWithOptionsFile("missing.json"));
        Clip("RemoveTrailersWithOptionsFile(cfg)", c =>
        {
            File.WriteAllText("cfg.json", "[{\"InputPath\":\"media/a.wav\",\"TrailerLengthInMilliSeconds\":2000,\"RemoveIntro\":true,\"IntroLengthInMilliseconds\":1000,\"OutputDirectoryPath\":\"out\"}]");
            c.RemoveTrailersWithOptionsFile("cfg.json");
        });
        Clip("RemoveTrailersWithOptionsFile(null) with default file", c =>
        {
            File.WriteAllText("TrailerClipperConfig.json", "[{\"InputPath\":\"media\",\"TrailerLengthInMilliSeconds\":3000}]");
            c.RemoveTrailersWithOptionsFile();
        });
        Clip("CreateDefaultSampleConfig", c => c.CreateDefaultSampleConfig());
        Clip("CreateDefaultSampleConfig twice", c => { c.CreateDefaultSampleConfig(); c.CreateDefaultSampleConfig(); });
    }

    private static void Clip(string name, Action<TrailerClipper> act)
    {
        var dir = Path.Combine(_workRoot, "c" + Cases.Count.ToString("D3"));
        Directory.CreateDirectory(Path.Combine(dir, "media"));
        foreach (var f in Directory.GetFiles(_fixtures)) File.Copy(f, Path.Combine(dir, "media", Path.GetFileName(f)));
        var old = Environment.CurrentDirectory;
        Environment.CurrentDirectory = dir;
        try
        {
            Case(name, "en-US", null, () => { act(new TrailerClipper()); return "(void)"; },
                () => Tree(dir));
        }
        finally { Environment.CurrentDirectory = old; }
    }

    // ---------- helpers ----------
    private static void Case(string method, string culture, string[] args, Func<object> call, Func<object> after = null)
    {
        var sw = new StringWriter();
        var stdout = Console.Out;
        Console.SetOut(sw);
        object result;
        var old = Environment.CurrentDirectory;
        if (after == null)
        {
            var dir = Path.Combine(_workRoot, "cli");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.Combine(dir, "media"));
                foreach (var f in Directory.GetFiles(_fixtures)) File.Copy(f, Path.Combine(dir, "media", Path.GetFileName(f)));
            }
            Environment.CurrentDirectory = dir;
        }
        try { result = call(); }
        catch (Exception e) { result = new Dictionary<string, string> { ["$throws"] = e.GetType().Name + ": " + e.Message.Replace("\r\n", "\n") }; }
        finally
        {
            Console.SetOut(stdout);
            if (after == null) Environment.CurrentDirectory = old;
        }
        var entry = new Dictionary<string, object>
        {
            ["method"] = method,
            ["culture"] = culture,
            ["args"] = args,
            ["result"] = result,
            ["console"] = Norm(sw.ToString())
        };
        if (after != null) entry["files"] = after();
        Cases.Add(entry);
    }

    private static object Dump(TrailerClipperOptions o)
    {
        if (o == null) return null;
        return new SortedDictionary<string, object>
        {
            ["ClippingConfigOutputFilePath"] = o.ClippingConfigOutputFilePath,
            ["DeleteOriginalFiles"] = o.DeleteOriginalFiles,
            ["InputPath"] = o.InputPath,
            ["IntroLengthInMilliseconds"] = o.IntroLengthInMilliseconds.ToString(CultureInfo.InvariantCulture),
            ["IsInputPathADirectory"] = SafeIsDir(o),
            ["MultiTaskFiles"] = o.MultiTaskFiles,
            ["OutputClippingConfigToFile"] = o.OutputClippingConfigToFile,
            ["OutputDirectoryPath"] = o.OutputDirectoryPath,
            ["OutputToConsole"] = o.OutputToConsole,
            ["ProcessEveryFile"] = o.ProcessEveryFile,
            ["RemoveIntro"] = o.RemoveIntro,
            ["TrailerLengthInMilliSeconds"] = o.TrailerLengthInMilliSeconds.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static object SafeIsDir(TrailerClipperOptions o)
    {
        try { return o.IsInputPathADirectory; }
        catch (Exception e) { return "$throws " + e.GetType().Name; }
    }

    private static object Tree(string root)
    {
        var list = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var f in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var rel = f.Substring(root.Length + 1).Replace('\\', '/');
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext == ".wav" || ext == ".mp3")
            {
                var d = Run("ffprobe", "-v error -show_entries format=duration -of csv=p=0 \"" + f + "\"").Trim();
                list[rel] = double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out var s)
                    ? (object)Math.Round(s * 1000) : "unreadable: " + Norm(d);
            }
            else if (ext == ".json") list[rel] = File.ReadAllText(f);
            else list[rel] = new FileInfo(f).Length;
        }
        return list;
    }

    // Absolute paths of the scratch folder become <work>, so the recording is portable.
    private static string Norm(string text) => text.Replace("\r\n", "\n").Replace(_workRoot, "<work>");

    private static string Run(string exe, string args)
    {
        var p = Process.Start(new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true });
        var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return o;
    }
}
