using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using TrailerClipperLib;

namespace TClipper
{
    internal static class Program
    {
        private static readonly TrailerClipper Clipper = new();
        private static readonly TcHelpReader HelpReader = new();
        private static readonly ClipperCommandLineInterpreter CommandProcessor = new();

        private static int Main(string[] args)
        {
            if (args.Length >= 1 && (args[0] == "--install-ffmpeg" || args[0] == "-install-ffmpeg"))
                return FFmpegInstaller.Install(assumeYes: args.Skip(1).Any(a => a == "--yes" || a == "-y")) ? 0 : 1;

            try
            {
                return Run(args);
            }
            catch (ToolNotFoundException)
            {
                Console.Error.WriteLine("ffmpeg and ffprobe were not found; tclipper needs them to clip.");
                if (!FFmpegInstaller.OfferInstall())
                {
                    Console.Error.WriteLine(FFmpegLocator.InstallHint);
                    return 1;
                }

                return RunOnce(args);
            }
            catch (Exception e) when (IsUserError(e))
            {
                Console.Error.WriteLine(Describe(e));
                return 1;
            }
        }

        // Errors the user can act on get a message and exit code 1 instead of a stack trace.
        private static bool IsUserError(Exception e) =>
            e is InvalidOperationException || e is IOException || e is ArgumentException || e is JsonException || e is IndexOutOfRangeException;

        private static string Describe(Exception e) => e switch
        {
            JsonException => "The config file is not valid JSON: " + e.Message,
            // 1.1.0's interpreter reads the value after -c or -o without checking that there is one.
            IndexOutOfRangeException => "An option is missing its value (-c <config_filepath>, -o <output_filename>). Use -h for help.",
            _ => e.Message
        };

        private static int RunOnce(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception e) when (IsUserError(e))
            {
                Console.Error.WriteLine(Describe(e));
                return 1;
            }
        }

        // The console app of 1.1.0; 2.0.0 adds exit codes (1 for arguments it cannot use, where 1.1.0 always exited 0).
        private static int Run(string[] args)
        {
            if (args.Length < 1)
            {
                Clipper.RemoveTrailersWithOptionsFile();
                return 0;
            }

            if (CommandProcessor.ShouldDisplayHelp(args))
            {
                var helpFile = HelpReader.ReadHelpFileText();

                Console.Write(helpFile);

                return 0;
            }

            if (CommandProcessor.ShouldCreateDefaultSampleConfig(args))
                Clipper.CreateDefaultSampleConfig();

            var configPath = CommandProcessor.ReadConfigFilePath(args);

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                Clipper.RemoveTrailersWithOptionsFile(configPath);
                return 0;
            }

            var options = CommandProcessor.ParseCommandLineArgs(args);

            if (options == null)
                return args.Length == 1 && CommandProcessor.ShouldCreateDefaultSampleConfig(args) ? 0 : 1;

            if (CommandProcessor.ShouldDumpConfigFile(args))
            {
                var configOutputPath = CommandProcessor.ReadConfigOutputPath(args);

                Clipper.RemoveTrailers(new[] { options }, configOutputPath);
            }
            else
            {
                Clipper.RemoveTrailers(options);
            }

            return 0;
        }
    }
}
