using System.IO;
using System.Reflection;

namespace TrailerClipperLib
{
    /// <summary>Reads the command-line help text embedded in the library.</summary>
    public class TcHelpReader
    {
        private const string ResourceName = "TrailerClipperLib.help.txt";

        /// <summary>The help text, as the tclipper command prints it for -h.</summary>
        public string ReadHelpFileText()
        {
            var assembly = typeof(TcHelpReader).Assembly;

            using var stream = assembly.GetManifestResourceStream(ResourceName)
                ?? throw new FileLoadException("Could not load help file.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
