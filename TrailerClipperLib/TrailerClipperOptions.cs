namespace TrailerClipperLib
{
    public class TrailerClipperOptions
    {
        public bool OutputToConsole { get; set; }
        public bool MultiTaskFiles { get; set; }
        public string OutputDirectoryPath { get; set; }
        public bool ProcessEveryFile { get; set; }
        public bool SingleFileMode { get; set; }
        public string SingleFileName { get; set; }
        public bool RemoveIntro { get; set; }
        public decimal IntroLengthInMilliseconds { get; set; }
        public string InputDirectoryPath { get; set; }

        public decimal TrailerLengthInMilliSeconds { get; set; }
    }
}