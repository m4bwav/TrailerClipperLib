using System.IO;
using System.Reflection;

namespace TrailerClipperLib
{
    public class TcHelpReader
    {
        private const string ResourceName = "TrailerClipperLib.help.txt";

        public string ReadHelpFileText()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                    throw new FileLoadException("Could not load help file.");

                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}