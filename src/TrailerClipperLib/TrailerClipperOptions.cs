using System.IO;
using System.Text.Json.Serialization;

namespace TrailerClipperLib
{
    /// <summary>
    ///     The data on the type of clipping and the files to clip for the trailer clipper to use to execute a video file clip.
    /// </summary>
    /// <remarks>
    ///     The declaration order of the properties is the property order of the saved config file (1.1.0 wrote it that way);
    ///     keep it.
    /// </remarks>
    public class TrailerClipperOptions
    {
        /// <summary>
        ///     Default constructor, defaults OutputToConsole to true
        /// </summary>
        public TrailerClipperOptions()
        {
            OutputToConsole = true;
        }

        /// <summary>
        ///     Sets input path and OutputToConsole to true
        /// </summary>
        /// <param name="path">Input file path</param>
        public TrailerClipperOptions(string? path) : this()
        {
            InputPath = path;
        }

        /// <summary>
        ///     Sets the input path, the trailer length and OutputToConsole to true
        /// </summary>
        /// <param name="path">Input file or directory path</param>
        /// <param name="milliseconds">The length of the trailer to remove, in milliseconds</param>
        public TrailerClipperOptions(string? path, decimal milliseconds) : this(path)
        {
            TrailerLengthInMilliSeconds = milliseconds;
        }

        /// <summary>
        ///     When true, some logging messages will be outputted to the console.  True by default.
        /// </summary>
        public bool OutputToConsole { get; set; }

        /// <summary>
        ///     Multithread the batch video clipping.  This will still be largely singled threaded once it hits ffmpeg
        ///     and thus is experimental
        /// </summary>
        public bool MultiTaskFiles { get; set; }

        /// <summary>
        ///     The directory to place the output files in.  If none is specified, "clipped" beside the input file is used
        /// </summary>
        public string? OutputDirectoryPath { get; set; }

        /// <summary>
        ///     If true don't bother checking the extension of a video file before clipping, otherwise skip unknown extensions
        /// </summary>
        public bool ProcessEveryFile { get; set; }

        /// <summary>
        ///     If set to true, the 'IntroLengthInMilliseconds' value will be used to remove the front intro up to that many
        ///     milliseconds.
        ///     This can be used with or without the trailer clipping functionality
        /// </summary>
        public bool RemoveIntro { get; set; }

        /// <summary>
        ///     When true, each input file is deleted after its clipped copy is written.
        /// </summary>
        public bool DeleteOriginalFiles { get; set; }

        /// <summary>
        ///     When 'RemoveIntro' is set to the true, this is the length in milliseconds of the intro to remove from the front of
        ///     the video
        /// </summary>
        public decimal IntroLengthInMilliseconds { get; set; }

        /// <summary>
        ///     The path can be a file or a directory.  if it is a directory all valid files will be processed in that directory.
        /// </summary>
        public string? InputPath { get; set; }

        /// <summary>
        ///     The length of the trailer in milliseconds to remove from the end of the video/audio file
        /// </summary>
        public decimal TrailerLengthInMilliSeconds { get; set; }

        /// <summary>
        ///     Kept from 1.x; not read by the clipper.
        /// </summary>
        public bool OutputClippingConfigToFile { get; set; }

        /// <summary>
        ///     Kept from 1.x; not read by the clipper.
        /// </summary>
        public string? ClippingConfigOutputFilePath { get; set; }

        /// <summary>
        ///     True when InputPath names an existing directory.
        /// </summary>
        public bool IsInputPathADirectory
        {
            get
            {
                if (!File.Exists(InputPath) && !Directory.Exists(InputPath))
                    return false;

                var attr = File.GetAttributes(InputPath);

                return attr.HasFlag(FileAttributes.Directory);
            }
        }

        /// <summary>
        ///     The folder that holds ffmpeg and ffprobe. When null, the TRAILERCLIPPER_FFMPEG environment variable, then PATH,
        ///     then the usual install folders (winget, Homebrew, /usr/local/bin) are searched. Added in 2.0.0; not saved in
        ///     config files, because it belongs to the machine.
        /// </summary>
        [JsonIgnore]
        public string? FFmpegDirectory { get; set; }
    }
}
