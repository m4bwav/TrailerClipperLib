using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace TrailerClipperLib
{
    /// <summary>
    ///     Batch clips the trailers from video and audio files
    /// </summary>
    public interface ITrailerClipperService
    {
        void RemoveTrailers(TrailerClipperOptions options);
        void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, string configFileOutputPath = null);
        void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, bool saveOptionsToFile = false);
        void RemoveTrailers(string path, decimal trailerLenghtInMilliseconds);
        void RemoveTrailersWithOptionsFile(string pathToOptionsFile = null);
        void RemoveIntros(string path, decimal introLenghtInMilliseconds);
        void RemoveIntrosAndTrailers(string path, decimal introLenghtInMilliseconds, decimal trailerLenghtInMilliseconds);
    }

    /// <summary>
    ///     Batch clips the trailers from video and audio files
    /// </summary>
    public class TrailerClipper : ITrailerClipperService
    {
        private const string DefaultTrailerClipperOptionFileName = "TrailerClipperConfig.json";
        private readonly TrailerClipperManager _manager = new TrailerClipperManager();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();

        /// <summary>
        ///     Execute the removal of trailers and/or intros as specificed in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        public void RemoveTrailers(TrailerClipperOptions options)
        {
            CheckIfOptionsAreNotNull(options);

            var optionList = new[] {options};

            RemoveTrailers(optionList, false);
        }

        /// <summary>
        ///     Execute the removal of trailers and/or intros as specificed in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        /// <param name="saveOptionsToFile">
        ///     If true, then the clipper will the config settings so that they don't have to be typed
        ///     in again, next time.
        /// </param>
        public void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, bool saveOptionsToFile = false)
        {
            var outputPath = saveOptionsToFile
                ? DefaultTrailerClipperOptionFileName
                : null;

            RemoveTrailers(options, outputPath);
        }

        /// <summary>
        ///     Execute the removal of trailers and/or intros as specificed in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        /// <param name="configFileOutputPath"></param>
        public void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, string configFileOutputPath = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var queryAbleOptions = options.AsQueryable();

            BeginClipping(queryAbleOptions);

            if (!string.IsNullOrWhiteSpace(configFileOutputPath))
                SerializeOptionsToFile(queryAbleOptions, configFileOutputPath);
        }

        public void RemoveTrailers(string directoryPath, decimal milliseconds)
        {
            var options = new TrailerClipperOptions(directoryPath, milliseconds);

            RemoveTrailers(options);
        }

        public void RemoveTrailersWithOptionsFile(string pathToOptionsFile = null)
        {
            pathToOptionsFile = pathToOptionsFile ?? DefaultTrailerClipperOptionFileName;

            if (!File.Exists(pathToOptionsFile))
                throw new InvalidOperationException("Cannot find config file: " + pathToOptionsFile);

            var text = File.ReadAllText(pathToOptionsFile);

            var options = _serializer.Deserialize<IEnumerable<TrailerClipperOptions>>(text);

            BeginClipping(options);
        }

        public void RemoveIntros(string path, decimal introLenghtInMilliseconds)
        {
            CheckPathForValidity(path);


            if (introLenghtInMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(introLenghtInMilliseconds), "Intro length must be greater than zero, otherwise there is nothing to clip");

            var options = new TrailerClipperOptions(path)
            {
                RemoveIntro = true,
                IntroLengthInMilliseconds = introLenghtInMilliseconds
            };


            RemoveTrailers(options);
        }

        public void RemoveIntrosAndTrailers(string path, decimal introLenghtInMilliseconds, decimal trailerLenghtInMilliseconds)
        {
            CheckPathForValidity(path);

            var options = new TrailerClipperOptions(path)
            {
                RemoveIntro = true,
                IntroLengthInMilliseconds = introLenghtInMilliseconds,
                TrailerLengthInMilliSeconds = trailerLenghtInMilliseconds
            };

            RemoveTrailers(options);
        }

        private static void CheckIfOptionsAreNotNull(TrailerClipperOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
        }

        private void SerializeOptionsToFile(IEnumerable<TrailerClipperOptions> options, string outputFilePath)
        {
            var json = _serializer.Serialize(options);

            File.WriteAllText(outputFilePath, json);
        }

        private void BeginClipping(IEnumerable<TrailerClipperOptions> options)
        {
            foreach (var batchOptions in options)
            {
                if (batchOptions.IsInputPathADirectory)
                    _manager.ExecuteDirectoryMode(batchOptions);
                else
                    _manager.ExecuteSingleFileMode(batchOptions);
            }
        }

        private static void CheckPathForValidity(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentOutOfRangeException(nameof(path));

            if (!Directory.Exists(path) && File.Exists(path))
                throw new ArgumentOutOfRangeException(path, "File or directory at: " + path + " does not exist, nothing to clip.");
        }
    }
}