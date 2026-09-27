using System;
using FFMpegCore;
using FFMpegCore.Exceptions;

namespace TrailerClipperLib
{
    /// <summary>
    ///     The two things the clipper asks of ffmpeg. An internal seam, so unit tests can run without ffmpeg.
    /// </summary>
    internal interface IMediaEngine
    {
        /// <summary>The file's duration in milliseconds, as 1.1.0 read it: truncated to hundredths of a second.</summary>
        decimal GetDurationInMilliseconds(string filePath, string? ffmpegDirectory);

        /// <summary>Writes the part of <paramref name="inputFilePath" /> that starts at <paramref name="seek" /> and lasts <paramref name="length" />.</summary>
        void Cut(string inputFilePath, string outputFilePath, TimeSpan seek, TimeSpan length, string? ffmpegDirectory);
    }

    /// <summary>
    ///     Runs ffprobe and ffmpeg through FFMpegCore, with the same input seek and output duration MediaToolkit used in 1.1.0.
    /// </summary>
    internal sealed class FFmpegEngine : IMediaEngine
    {
        public decimal GetDurationInMilliseconds(string filePath, string? ffmpegDirectory)
        {
            RefuseQuotes(filePath);
            var options = Options(ffmpegDirectory);
            IMediaAnalysis analysis;
            try
            {
                analysis = FFProbe.Analyse(filePath, options);
            }
            catch (Exception e) when (e is FFMpegException || e is FormatException || e is InvalidOperationException && e is not ToolNotFoundException)
            {
                throw new InvalidOperationException("ffprobe could not read '" + filePath + "': " + LastLines(e.Message), e);
            }

            return DurationAsMediaToolkitReadIt(analysis.Format.Duration);
        }

        // MediaToolkit parsed ffmpeg's "Duration: hh:mm:ss.cc" line, which ffmpeg rounds to the nearest hundredth
        // (av_dump_format adds 5000 microseconds before cutting the digits), so 1.1.0 saw 1.2375 s as 1.24 s.
        internal static decimal DurationAsMediaToolkitReadIt(TimeSpan duration)
        {
            var microseconds = duration.Ticks / 10;
            var hundredths = (microseconds + 5000) / 10000;
            return hundredths * 10m;
        }

        public void Cut(string inputFilePath, string outputFilePath, TimeSpan seek, TimeSpan length, string? ffmpegDirectory)
        {
            RefuseQuotes(inputFilePath);
            RefuseQuotes(outputFilePath);
            try
            {
                FFMpegArguments
                    .FromFileInput(inputFilePath, true, o => o.Seek(seek))
                    .OutputToFile(outputFilePath, true, o => o.WithDuration(length))
                    .ProcessSynchronously(true, Options(ffmpegDirectory));
            }
            catch (FFMpegException e)
            {
                throw new InvalidOperationException("ffmpeg could not clip '" + inputFilePath + "': " + LastLines(e.Message), e);
            }
        }

        // FFMpegCore puts each path in double quotes without escaping, so a '"' in a file name (legal on Linux and macOS)
        // would end the path and pass the rest to ffmpeg as options.
        internal static void RefuseQuotes(string path)
        {
#pragma warning disable CA2249 // string.Contains(char) does not exist on netstandard2.0
            if (path.IndexOf('"') >= 0)
#pragma warning restore CA2249
                throw new InvalidOperationException("File names that contain a double quote are not supported: '" + path + "'");
        }

        private static FFOptions Options(string? ffmpegDirectory)
        {
            var folder = FFmpegLocator.Find(ffmpegDirectory);
            if (folder == null)
                throw new ToolNotFoundException(FFmpegLocator.InstallHint);

            return new FFOptions { BinaryFolder = folder };
        }

        private static string LastLines(string text)
        {
            var lines = text.Replace("\r\n", "\n").Trim().Split('\n');
            var start = Math.Max(0, lines.Length - 3);
            return string.Join(" | ", lines, start, lines.Length - start);
        }
    }

    /// <summary>ffmpeg or ffprobe is not installed where TrailerClipper looks.</summary>
    [Serializable]
    public sealed class ToolNotFoundException : InvalidOperationException
    {
        /// <summary>Creates the exception with the install hint as its message.</summary>
        public ToolNotFoundException() : base(FFmpegLocator.InstallHint)
        {
        }

        /// <summary>Creates the exception with a message.</summary>
        public ToolNotFoundException(string message) : base(message)
        {
        }

        /// <summary>Creates the exception with a message and a cause.</summary>
        public ToolNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
