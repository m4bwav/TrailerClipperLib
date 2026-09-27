using System;
using System.IO;
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
            if (args.Length == 1 && (args[0] == "--install-ffmpeg" || args[0] == "-install-ffmpeg"))
                return FFmpegInstaller.Install(assumeYes: false) ? 0 : 1;

            try
            {
                Run(args);
                return 0;
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
            catch (Exception e) when (e is InvalidOperationException || e is IOException || e is ArgumentException)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }

        private static int RunOnce(string[] args)
        {
            try
            {
                Run(args);
                return 0;
            }
            catch (Exception e) when (e is InvalidOperationException || e is IOException || e is ArgumentException)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }

        // The console app of 1.1.0, unchanged apart from the exit codes above.
        private static void Run(string[] args)
        {
            if (args.Length < 1)
            {
                Clipper.RemoveTrailersWithOptionsFile();
                return;
            }

            if (CommandProcessor.ShouldDisplayHelp(args))
            {
                var helpFile = HelpReader.ReadHelpFileText();

                Console.Write(helpFile);

                return;
            }

            if (CommandProcessor.ShouldCreateDefaultSampleConfig(args))
                Clipper.CreateDefaultSampleConfig();

            var configPath = CommandProcessor.ReadConfigFilePath(args);

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                Clipper.RemoveTrailersWithOptionsFile(configPath);
                return;
            }

            var options = CommandProcessor.ParseCommandLineArgs(args);

            if (options == null)
                return;

            if (CommandProcessor.ShouldDumpConfigFile(args))
            {
                var configOutputPath = CommandProcessor.ReadConfigOutputPath(args);

                Clipper.RemoveTrailers(new[] { options }, configOutputPath);
            }
            else
            {
                Clipper.RemoveTrailers(options);
            }
        }
    }
}
