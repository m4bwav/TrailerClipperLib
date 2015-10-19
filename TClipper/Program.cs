using System;
using TrailerClipperLib;

namespace TClipper
{
    internal class Program
    {
        private static readonly TrailerClipper Clipper = new TrailerClipper();
        private static readonly TcHelpReader HelpReader = new TcHelpReader();
        private static readonly ClipperCommandLineInterpreter CommandProcessor = new ClipperCommandLineInterpreter();

        private static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
                Clipper.RemoveTrailersWithOptionsFile();

            if (CommandProcessor.ShouldDisplayHelp(args))
            {
                var helpFile = HelpReader.ReadHelpFileText();

                Console.Write(helpFile);

                return;
            }

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

                Clipper.RemoveTrailers(new[] {options}, configOutputPath);
            }
            else
                Clipper.RemoveTrailers(options);
        }
    }
}