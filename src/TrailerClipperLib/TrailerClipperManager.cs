using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TrailerClipperLib
{
    internal sealed class TrailerClipperManager
    {
        private const string DefaultOutputDirectoryName = "clipped";
        private static readonly string[] ValidFileExtensions = new[] { "m4b", "wav", "mp3", "mp4", "flv", "avi", "mpg", "mov" };

        private readonly IMediaEngine _engine;

        public TrailerClipperManager() : this(new FFmpegEngine())
        {
        }

        internal TrailerClipperManager(IMediaEngine engine)
        {
            _engine = engine;
        }

        public void ExecuteSingleFileMode(TrailerClipperOptions options)
        {
            var singleFile = options.InputPath;

            if (string.IsNullOrWhiteSpace(singleFile))
                throw new ArgumentOutOfRangeException(nameof(options), " file name: " + singleFile + " is invalid.");

            CheckLengths(options);

            var failures = new ConcurrentQueue<Exception>();
            RemoveTrailerFromSingleFile(singleFile!, options, failures);
            ThrowIfAnyFailed(failures);
        }

        public void ExecuteDirectoryMode(TrailerClipperOptions options)
        {
            CheckLengths(options);

            // In name order: 1.1.0 ran on Windows, where the file system lists names in order; macOS and Linux may not.
            var files = Directory.GetFiles(options.InputPath!).OrderBy(f => f, StringComparer.Ordinal).ToArray();
            var failures = new ConcurrentQueue<Exception>();

            if (options.MultiTaskFiles)
                Parallel.ForEach(files, filePath => RemoveTrailerFromSingleFile(filePath, options, failures));
            else
                foreach (var filePath in files)
                    RemoveTrailerFromSingleFile(filePath, options, failures);

            ThrowIfAnyFailed(failures);
        }

        // 2.0.0 (E6): negative lengths are refused before any file is touched; 1.1.0 treated a negative trailer as none.
        private static void CheckLengths(TrailerClipperOptions options)
        {
            if (options.TrailerLengthInMilliSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(options), "Trailer length must not be negative: " + options.TrailerLengthInMilliSeconds);

            if (options.RemoveIntro && options.IntroLengthInMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(options), "Intro length must not be negative: " + options.IntroLengthInMilliseconds);
        }

        // 2.0.0 (E7): 1.1.0 swallowed ffmpeg's failures; now the rest of the batch runs and then the failures are thrown.
        private static void ThrowIfAnyFailed(ConcurrentQueue<Exception> failures)
        {
            if (failures.IsEmpty)
                return;

            var all = failures.ToArray();
            if (all.Length == 1)
                throw all[0];

            throw new InvalidOperationException(
                all.Length + " files could not be clipped: " + string.Join(" / ", all.Select(e => e.Message)),
                new AggregateException(all));
        }

        private void RemoveTrailerFromSingleFile(string filePath, TrailerClipperOptions options, ConcurrentQueue<Exception> failures)
        {
            if (!options.ProcessEveryFile && IsNotAValidMediaFile(filePath))
                return;

            try
            {
                var durationInMilliseconds = _engine.GetDurationInMilliseconds(filePath, options.FFmpegDirectory);

                var newDuration = durationInMilliseconds - options.TrailerLengthInMilliSeconds;

                if (options.OutputToConsole)
                    Console.WriteLine("Starting on file: " + filePath);

                if (!TrimFileToNewDuration(filePath, newDuration, options))
                    return;

                if (options.DeleteOriginalFiles)
                    File.Delete(filePath);
            }
            catch (ToolNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException e)
            {
                failures.Enqueue(e);
            }
        }

        private bool TrimFileToNewDuration(string inputFilePath, decimal newDurationInMilliseconds, TrailerClipperOptions clipperOptions)
        {
            var seekToPosition = clipperOptions.RemoveIntro
                ? TimeSpan.FromMilliseconds(Convert.ToDouble(clipperOptions.IntroLengthInMilliseconds))
                : TimeSpan.Zero;

            var lengthSpan = TimeSpan.FromMilliseconds(Convert.ToDouble(newDurationInMilliseconds)).Subtract(seekToPosition);

            // 2.0.0 (E6): 1.1.0 handed ffmpeg a negative length here and wrote a file nothing could read.
            if (lengthSpan <= TimeSpan.Zero)
            {
                if (clipperOptions.OutputToConsole)
                    Console.WriteLine("Skipped file, nothing is left after the cut: " + inputFilePath);
                return false;
            }

            var outputFilePath = ComputeOutputFilePath(inputFilePath, clipperOptions.OutputDirectoryPath);

            _engine.Cut(inputFilePath, outputFilePath, seekToPosition, lengthSpan, clipperOptions.FFmpegDirectory);

            if (clipperOptions.OutputToConsole)
                Console.WriteLine("Finished on trimming file, output: " + outputFilePath);

            return true;
        }

        // 2.0.0 (E8): Path.Combine instead of 1.1.0's "\" joins, so it works off Windows; on Windows the paths are the same.
        private static string ComputeOutputFilePath(string inputFilePath, string? outputDirectoryPath)
        {
            var inputFileInfo = new FileInfo(inputFilePath);

            var outputFileName = inputFileInfo.Name;

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                outputDirectoryPath = Path.Combine(inputFileInfo.DirectoryName ?? string.Empty, DefaultOutputDirectoryName);

            if (!Directory.Exists(outputDirectoryPath))
                Directory.CreateDirectory(outputDirectoryPath!);

            return Path.Combine(outputDirectoryPath!, outputFileName);
        }

        private static bool IsNotAValidMediaFile(string file)
        {
            var fileExtension = new FileInfo(file).Extension;

            if (string.IsNullOrWhiteSpace(fileExtension))
                return true;

            var extension = fileExtension
                .Trim()
                .ToLowerInvariant()
                .Substring(1); // remove leading period

            return !ValidFileExtensions.Contains(extension);
        }
    }
}
