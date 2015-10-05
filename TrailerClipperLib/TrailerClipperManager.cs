using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

namespace TrailerClipperLib
{
    internal class TrailerClipperManager
    {
        private const string DefaultOutputDirectory = @"\clipped";
        private readonly ICollection<string> _validFileExtensions = new[] {"m4b", "wav", "mp3", "mp4", "flv", "avi", "mpg", "mov"};

        public void ExecuteSingleFileMode(TrailerClipperOptions options)
        {
            var singleFile = options.InputPath;

            if (string.IsNullOrWhiteSpace(singleFile))
                throw new ArgumentOutOfRangeException(nameof(options), " file name: " + singleFile + " is invalid.");


            RemoveTrailerFromSingleFile(singleFile, options);
        }

        public void ExecuteDirectoryMode(TrailerClipperOptions options)
        {
            var directoryPath = options.InputPath;

            var files = Directory.GetFiles(directoryPath);

            if (options.MultiTaskFiles)
                Parallel.ForEach(files, filePath => RemoveTrailerFromSingleFile(filePath, options));
            else
                foreach (var filePath in files)
                    RemoveTrailerFromSingleFile(filePath, options);
        }

        private void RemoveTrailerFromSingleFile(string filePath, TrailerClipperOptions options)
        {
            if (!options.ProcessEveryFile && IsNotAValidMediaFile(filePath))
                return;

            var durationInMilliseconds = GetDurationOfMediaFile(filePath);

            var newDuration = durationInMilliseconds - options.TrailerLengthInMilliSeconds;

            if (options.OutputToConsole)
                Console.WriteLine("Starting on file: " + filePath);

            TrimFileToNewDuration(filePath, newDuration, options);
        }

        private static void TrimFileToNewDuration(string inputFilePath, decimal newDurationInMilliseconds, TrailerClipperOptions clipperOptions)
        {
            var outputFilePath = ComputeOutputFilePath(inputFilePath, clipperOptions.OutputDirectoryPath);

            var inputFile = new MediaFile {Filename = inputFilePath};

            var outputFile = new MediaFile {Filename = outputFilePath};

            var options = InitializeClippingData(newDurationInMilliseconds, clipperOptions);

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);

                engine.Convert(inputFile, outputFile, options);
            }

            if (clipperOptions.OutputToConsole)
                Console.WriteLine("Finished on trimming file, output: " + outputFilePath);
        }

        private static string ComputeOutputFilePath(string inputFilePath, string outputDirectoryPath)
        {
            var inputFileInfo = new FileInfo(inputFilePath);

            var outputFileName = inputFileInfo.Name;

            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                outputDirectoryPath = inputFileInfo.DirectoryName + DefaultOutputDirectory;

            if (!Directory.Exists(outputDirectoryPath))
                Directory.CreateDirectory(outputDirectoryPath);

            return outputDirectoryPath + @"\" + outputFileName;
        }

        private static ConversionOptions InitializeClippingData(decimal newDurationInMilliseconds, TrailerClipperOptions clipperOptions)
        {
            var options = new ConversionOptions();

            var newDuration = Convert.ToDouble(newDurationInMilliseconds);

            var newDurationTimeSpan = TimeSpan.FromMilliseconds(newDuration);

            var seekToPosition = TimeSpan.Zero;

            if (clipperOptions.RemoveIntro)
            {
                var introLength = Convert.ToDouble(clipperOptions.IntroLengthInMilliseconds);

                seekToPosition = TimeSpan.FromMilliseconds(introLength);
            }

            var lengthSpan = newDurationTimeSpan.Subtract(seekToPosition);

            options.CutMedia(seekToPosition, lengthSpan);

            return options;
        }

        private static decimal GetDurationOfMediaFile(string filePath)
        {
            var inputFile = new MediaFile {Filename = filePath};

            using (var engine = new Engine())
                engine.GetMetadata(inputFile);

            var durationOfMediaFile = inputFile.Metadata.Duration.TotalMilliseconds;

            return Convert.ToDecimal(durationOfMediaFile);
        }

        private bool IsNotAValidMediaFile(string file)
        {
            var fileObj = new FileInfo(file);

            var fileExtension = fileObj.Extension;

            if (string.IsNullOrWhiteSpace(fileExtension))
                return true;

            var extension = fileExtension
                .Trim()
                .ToLower()
                .Substring(1); //remove leading period

            return !_validFileExtensions.Contains(extension);
        }
    }
}