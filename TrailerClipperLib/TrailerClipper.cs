using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

namespace TrailerClipperLib
{
    public interface ITrailerClipperService
    {
        void RemoveTrailers(string directoryPath, double trailerLengthInMilliSeconds, TrailerClipperOptions options = null);
    }

    public class TrailerClipper : ITrailerClipperService
    {
        private const string DefaultOutputDirectory = @"\clipped";
        private readonly ICollection<string> _validFileExtensions = new[] {"mp4", "flv", "avi", "mpg"};

        public void RemoveTrailers(string directoryPath, double trailerLengthInMilliSeconds, TrailerClipperOptions options = null)
        {
            if (options == null)
                options = new TrailerClipperOptions();

            var files = Directory.GetFiles(directoryPath);

            if (options.MultiTaskFiles)
                Parallel.ForEach(files, filePath => RemoveTrailerFromFile(trailerLengthInMilliSeconds, filePath, options));
            else
                foreach (var filePath in files)
                    RemoveTrailerFromFile(trailerLengthInMilliSeconds, filePath, options);
        }

        private void RemoveTrailerFromFile(double trailerLengthInMilliSeconds, string filePath, TrailerClipperOptions options)
        {
            if (!options.ProcessEveryFile && IsNotAValidMediaFile(filePath))
                return;

            var durationInMilliseconds = GetDurationOfMediaFile(filePath);

            var newDuration = durationInMilliseconds - trailerLengthInMilliSeconds;

            if (options.OutputToConsole)
                Console.WriteLine("Starting on file: " + filePath);

            TrimFileToNewDuration(filePath, newDuration, options);
        }

        private static void TrimFileToNewDuration(string inputFilePath, double newDurationInMilliseconds, TrailerClipperOptions clipperOptions)
        {
            var outputFilePath = ComputeOutputFilePath(inputFilePath, clipperOptions.OutputDirectoryPath);

            var inputFile = new MediaFile {Filename = inputFilePath};

            var outputFile = new MediaFile {Filename = outputFilePath};

            var options = InitializeClippingData(newDurationInMilliseconds);

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

        private static ConversionOptions InitializeClippingData(double newDurationInMilliseconds)
        {
            var options = new ConversionOptions();

            var newDurationTimeSpan = TimeSpan.FromMilliseconds(newDurationInMilliseconds);

            options.CutMedia(TimeSpan.Zero, newDurationTimeSpan);

            return options;
        }

        private static double GetDurationOfMediaFile(string filePath)
        {
            var inputFile = new MediaFile {Filename = filePath};

            using (var engine = new Engine())
                engine.GetMetadata(inputFile);

            return inputFile.Metadata.Duration.TotalMilliseconds;
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