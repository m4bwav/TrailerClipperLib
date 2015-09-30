namespace TrailerClipperLib
{
    /// <summary>
    ///     The data on the type of clipping and the files to clip for the trailer clipper to use to execute a video file clip.
    /// </summary>
    public class TrailerClipperOptions
    {
        /// <summary>
        ///     Default constructor, defaulst OutputToConsole to true
        /// </summary>
        public TrailerClipperOptions()
        {
            OutputToConsole = true;
        }

        /// <summary>
        ///     When true, some logging messages will be outputted to the console.  True by default.
        /// </summary>
        public bool OutputToConsole { get; set; }

        /// <summary>
        ///     Multithread the batch video clipping.  This will still be largely singled threaded once it hits the ffmpeg wrapper
        ///     and thus is experimental
        /// </summary>
        public bool MultiTaskFiles { get; set; }

        /// <summary>
        ///     The directory to place the output files in.  If none is specificed, "clipped" will be used
        /// </summary>
        public string OutputDirectoryPath { get; set; }

        /// <summary>
        ///     If true don't bother checking the extension of a video file before clipping, otherwise skip unknown extensions
        /// </summary>
        public bool ProcessEveryFile { get; set; }

        /// <summary>
        ///     If true use property 'SingleFileName' as a path to process only a single video file,
        ///     rather than the default batch processing mode which uses the path InputDirectoryPath
        /// </summary>
        public bool SingleFileMode { get; set; }

        /// <summary>
        ///     When 'SingleFileMode' is set to true, this value will be used as the path of the single file to process
        /// </summary>
        public string SingleFileName { get; set; }

        /// <summary>
        ///     If set to true, the 'IntroLengthInMilliseconds' value will be used to remove the front intro up to that many
        ///     milliseconds.
        ///     This can be used with or without the trailer clipping functionality
        /// </summary>
        public bool RemoveIntro { get; set; }

        /// <summary>
        ///     When 'RemoveIntro' is set to the true, this is the lenght in milliseconds of the intro to remove from the front of
        ///     the video
        /// </summary>
        public decimal IntroLengthInMilliseconds { get; set; }

        /// <summary>
        ///     When 'SingleFileMode' is set to false, this is the directory that is used to load files for clipping.  All files
        ///     that match known file extensions will be loaded, unless ' ProcessEveryFile' is set to true.
        /// </summary>
        public string InputDirectoryPath { get; set; }

        /// <summary>
        ///     The length of the trailer in milliseconds to remove from the end of the video/audio file
        /// </summary>
        public decimal TrailerLengthInMilliSeconds { get; set; }
    }
}