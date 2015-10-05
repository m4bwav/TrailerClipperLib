using TrailerClipperLib;

namespace TClipper
{
    internal class Program
    {
        private static readonly TrailerClipper Clipper = new TrailerClipper();

        private static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
                Clipper.RemoveTrailersWithOptionsFile();

            var commandProcessor = new ClipperCommandLineInterpreter();

            var configPath = commandProcessor.ReadConfigFilePath(args);

            if (!string.IsNullOrWhiteSpace(configPath))
            {
                Clipper.RemoveTrailersWithOptionsFile(configPath);
                return;
            }

            var options = commandProcessor.ParseCommandLineArgs(args);

            if (options == null)
                return;

            if (commandProcessor.ShouldDumpConfigFile(args))
            {
                var configOutputPath = commandProcessor.ReadConfigOutputPath(args);

                Clipper.RemoveTrailers(new[] {options}, configOutputPath);
            }
            else
                Clipper.RemoveTrailers(options);
        }
    }
}