using System;
using System.IO;
using TrailerClipperLib;

namespace TClipper
{
    internal class Program
    {
        private static readonly TrailerClipper Clipper = new TrailerClipper();

        private static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                const string errorMessage = "Incorrect parameters, try 'TClipper [<options>] <input_directory_path> <trailer_duration_in_milliseconds>'";
                Console.WriteLine(errorMessage);
                return;
            }

            var argsLength = args.Length;

            var options = ProcessInputOptions(args);

            if (options.SingleFileMode)
            {
                if (!File.Exists(options.SingleFileName))
                {
                    string errorMessage = $"Incorrect parameter, input file: '{options.SingleFileName}' does not exist";
                    Console.WriteLine(errorMessage);
                    return;
                }
            }
            else
            {
                var inputDirectoryPath = args[argsLength - 2];

                if (!Directory.Exists(inputDirectoryPath))
                {
                    string errorMessage = $"Incorrect parameter, input directory: '{inputDirectoryPath}' does not exist";
                    Console.WriteLine(errorMessage);
                    return;
                }

                options.InputDirectoryPath = inputDirectoryPath;
            }
            var durationArg = args[argsLength - 1];

            decimal trailerLengthInMilliSeconds;

            if (!decimal.TryParse(durationArg, out trailerLengthInMilliSeconds))
            {
                if (!options.RemoveIntro || options.IntroLengthInMilliseconds == default(decimal))
                {
                    string errorMessage = $"Incorrect parameter, new duration in milliseconds: '{durationArg}' cannot be parsed";
                    Console.WriteLine(errorMessage);
                    return;
                }
            }
            else
            {
                var argBefore = args[argsLength - 2].Trim().ToLower();

                if (argBefore != "-i" && argBefore != "-intro")
                    options.TrailerLengthInMilliSeconds = trailerLengthInMilliSeconds;
            }


            Clipper.RemoveTrailers(options);
        }

        private static TrailerClipperOptions ProcessInputOptions(string[] args)
        {
            var options = new TrailerClipperOptions();
            
            for (var iii = 0; iii < args.Length; iii++)
            {
                var currentArg = args[iii].Trim().ToLower();

                if (!currentArg.StartsWith("-"))
                    continue;

                switch (currentArg)
                {
                    case "-i":
                    case "-intro":
                        options.RemoveIntro = true;
                        iii++;
                        if (args.Length <= iii)
                            break;

                        InitializeIntroRemovalOptions(args, iii, options);
                        break;
                    case "-f":
                    case "-file":
                        options.SingleFileMode = true;
                        iii++;
                        if (args.Length <= iii)
                            break;
                        options.SingleFileName = args[iii];
                        break;
                    case "-m":
                    case "-multi":
                        options.MultiTaskFiles = true;
                        break;
                    case "-cf":
                    case "-consoleoff":
                        options.OutputToConsole = false;
                        break;
                    case "-a":
                    case "-allfiles":
                        options.ProcessEveryFile = true;
                        break;
                }
            }

            return options;
        }

        private static void InitializeIntroRemovalOptions(string[] args, int iii, TrailerClipperOptions options)
        {
            decimal introLengthInMilliseconds;

            if (!decimal.TryParse(args[iii], out introLengthInMilliseconds))
            {
                throw new ArgumentOutOfRangeException(nameof(args));
            }

            options.IntroLengthInMilliseconds = introLengthInMilliseconds;
        }
    }
}