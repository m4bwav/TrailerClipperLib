using TrailerClipperLib;

namespace TClipper
{
    internal class Program
    {
        private static readonly TrailerClipper Clipper = new TrailerClipper();

        private static void Main(string[] args)
        {
            var commandProcessor = new ClipperCommandLineInterpreter();

            var options = commandProcessor.ParseCommandLineArgs(args);

            if (options == null)
                return;

            Clipper.RemoveTrailers(options);
        }
    }
}