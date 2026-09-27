using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TrailerClipperLib
{
    /// <summary>
    ///     Batch clips the trailers from video and audio files
    /// </summary>
    public interface ITrailerClipperService
    {
        /// <summary>Clips as the options say.</summary>
        void RemoveTrailers(TrailerClipperOptions options);

        /// <summary>Clips as each options entry says, then saves them to a config file when a path is given.</summary>
        void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, string? configFileOutputPath = null);

        /// <summary>Clips as each options entry says, then saves them to TrailerClipperConfig.json when asked.</summary>
        void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, bool saveOptionsToFile = false);

        /// <summary>Removes the last <paramref name="trailerLenghtInMilliseconds" /> from a file or from every media file in a directory.</summary>
        void RemoveTrailers(string path, decimal trailerLenghtInMilliseconds);

        /// <summary>Clips as a saved config file says (TrailerClipperConfig.json in the current directory when no path is given).</summary>
        void RemoveTrailersWithOptionsFile(string? pathToOptionsFile = null);

        /// <summary>Removes the first <paramref name="introLenghtInMilliseconds" /> from a file or from every media file in a directory.</summary>
        void RemoveIntros(string path, decimal introLenghtInMilliseconds);

        /// <summary>Removes an intro and a trailer from a file or from every media file in a directory.</summary>
        void RemoveIntrosAndTrailers(string path, decimal introLenghtInMilliseconds, decimal trailerLenghtInMilliseconds);
    }

    /// <summary>
    ///     Batch clips the trailers from video and audio files
    /// </summary>
    public class TrailerClipper : ITrailerClipperService
    {
        private const string DefaultTrailerClipperOptionFileName = "TrailerClipperConfig.json";

        // The config file keeps 1.1.0's JavaScriptSerializer text: property names as declared, no indentation, and
        // (like JavaScriptSerializer) property names read without regard to case.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        private static readonly TrailerClipperOptions DefaultConfig = new()
        {
            RemoveIntro = true,
            IntroLengthInMilliseconds = 3 * 1000 + 500,
            TrailerLengthInMilliSeconds = 17 * 1000 + 500,
            OutputDirectoryPath = "Clipped",
            DeleteOriginalFiles = false,
            InputPath = "Original"
        };

        private readonly TrailerClipperManager _manager;

        /// <summary>Creates a clipper that runs the ffmpeg it finds (see <see cref="FFmpegLocator" />).</summary>
        public TrailerClipper() : this(new TrailerClipperManager())
        {
        }

        internal TrailerClipper(TrailerClipperManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        ///     Execute the removal of trailers and/or intros as specified in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        public void RemoveTrailers(TrailerClipperOptions options)
        {
            CheckIfOptionsAreNotNull(options);

            var optionList = new[] { options };

            RemoveTrailers(optionList, false);
        }

        /// <summary>
        ///     Execute the removal of trailers and/or intros as specified in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        /// <param name="saveOptionsToFile">
        ///     If true, then the clipper will save the config settings so that they don't have to be typed
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
        ///     Execute the removal of trailers and/or intros as specified in the options
        /// </summary>
        /// <param name="options">Contains the various clipping options and target paths.  Cannot be null</param>
        /// <param name="configFileOutputPath">Where to save the options after clipping; null or blank saves nothing</param>
        public void RemoveTrailers(IEnumerable<TrailerClipperOptions> options, string? configFileOutputPath = null)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var optionList = options.ToList();

            BeginClipping(optionList);

            if (!string.IsNullOrWhiteSpace(configFileOutputPath))
                SerializeOptionsToFile(optionList, configFileOutputPath!);
        }

        /// <summary>Removes the last <paramref name="trailerLenghtInMilliseconds" /> from a file or from every media file in a directory.</summary>
        public void RemoveTrailers(string path, decimal trailerLenghtInMilliseconds)
        {
            var options = new TrailerClipperOptions(path, trailerLenghtInMilliseconds);

            RemoveTrailers(options);
        }

        /// <summary>Clips as a saved config file says (TrailerClipperConfig.json in the current directory when no path is given).</summary>
        public void RemoveTrailersWithOptionsFile(string? pathToOptionsFile = null)
        {
            if (pathToOptionsFile == null)
            {
                if (!File.Exists(DefaultTrailerClipperOptionFileName))
                {
                    Console.WriteLine("No parameters and no config file available, use -h for help");
                    return;
                }

                pathToOptionsFile = DefaultTrailerClipperOptionFileName;
            }

            if (!File.Exists(pathToOptionsFile))
                throw new InvalidOperationException("Cannot find config file: " + pathToOptionsFile);

            var text = File.ReadAllText(pathToOptionsFile);

            var options = JsonSerializer.Deserialize<List<TrailerClipperOptions>>(text, SerializerOptions) ?? new List<TrailerClipperOptions>();

            BeginClipping(options);
        }

        /// <summary>Removes the first <paramref name="introLenghtInMilliseconds" /> from a file or from every media file in a directory.</summary>
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

        /// <summary>Removes an intro and a trailer from a file or from every media file in a directory.</summary>
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

        /// <summary>Writes TrailerClipperConfig.json with sample settings to the current directory, unless it exists.</summary>
        public void CreateDefaultSampleConfig()
        {
            if (File.Exists(DefaultTrailerClipperOptionFileName))
            {
                Console.WriteLine("Cannot create a sample config file because a file with the name: "
                                  + DefaultTrailerClipperOptionFileName + ", already exists");
                return;
            }

            var json = JsonSerializer.Serialize(new[] { DefaultConfig }, SerializerOptions);

            File.WriteAllText(DefaultTrailerClipperOptionFileName, json);
        }

        private static void CheckIfOptionsAreNotNull(TrailerClipperOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
        }

        private static void SerializeOptionsToFile(IEnumerable<TrailerClipperOptions> options, string outputFilePath)
        {
            var json = JsonSerializer.Serialize(options, SerializerOptions);

            File.WriteAllText(outputFilePath, json);
        }

        private void BeginClipping(IEnumerable<TrailerClipperOptions> options)
        {
            foreach (var batchOptions in options)
            {
                // 2.0.0 (E2): 1.1.0 did nothing, silently, for a path that does not exist.
                if (!File.Exists(batchOptions.InputPath) && !Directory.Exists(batchOptions.InputPath))
                    throw new FileNotFoundException("File or directory at: " + batchOptions.InputPath + " does not exist, nothing to clip.", batchOptions.InputPath);

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

            // 2.0.0 (E1): 1.1.0 had this test inverted and refused every existing file.
            if (!Directory.Exists(path) && !File.Exists(path))
                throw new FileNotFoundException("File or directory at: " + path + " does not exist, nothing to clip.", path);
        }
    }
}
