using System;
using System.IO;
using System.Linq;

namespace TrailerClipperLib
{
    /// <summary>
    ///     Used to intrepret and process command-line arguments into clipper options
    /// </summary>
    public interface ICommandLineInterpreter
    {
        TrailerClipperOptions ParseCommandLineArgs(string[] args);
    }

    /// <summary>
    ///     Used to intrepret and process command-line arguments into clipper options
    /// </summary>
    public class ClipperCommandLineInterpreter : ICommandLineInterpreter
    {
        /// <summary>
        ///     Process the input args into clipper options
        /// </summary>
        /// <param name="args">The input console app arguments</param>
        /// <returns>Valid clipper options or null if there was an issue</returns>
        public TrailerClipperOptions ParseCommandLineArgs(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                const string errorMessage = "Incorrect parameters, try 'TClipper [<options>] <input_directory_path> <trailer_duration_in_milliseconds>'";
                Console.WriteLine(errorMessage);
                return null;
            }

            var argsLength = args.Length;

            var options = ProcessInputOptions(args);

            var lastArgument = args[argsLength - 1];

            decimal trailerLengthInMilliSeconds;

            if (!decimal.TryParse(lastArgument, out trailerLengthInMilliSeconds))
            {
                if (!options.RemoveIntro || options.IntroLengthInMilliseconds == default(decimal))
                {
                    string errorMessage = $"Incorrect parameter, new duration in milliseconds: '{lastArgument}' cannot be parsed";
                    Console.WriteLine(errorMessage);
                    return null;
                }

                options.InputPath = lastArgument;
            }
            else
            {
                var secondToLastArg = args[argsLength - 2].Trim().ToLower();

                if (secondToLastArg != "-i" && secondToLastArg != "-intro")
                    options.TrailerLengthInMilliSeconds = trailerLengthInMilliSeconds;
            }

            options.InputPath = args[argsLength - 2];

            if (!Directory.Exists(options.InputPath) && !File.Exists(options.InputPath))
            {
                string errorMessage = $"Incorrect parameter, input directory or file: '{options.InputPath}' does not exist";
                Console.WriteLine(errorMessage);
                return null;
            }

            return options;
        }

        public string ReadConfigFilePath(string[] args)
        {
            if (args == null || args.Length < 1)
                return null;

            var currentArg = args[0].Trim().ToLower();

            if (currentArg != "-c" && currentArg != "-config")
                return null;

            return args[1];
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
                    case "-d":
                        options.DeleteOriginalFiles = true;
                        break;
                    case "-i":
                    case "-intro":
                        options.RemoveIntro = true;
                        iii++;
                        if (args.Length <= iii)
                            break;

                        InitializeIntroRemovalOptions(args, iii, options);
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

        public bool ShouldDumpConfigFile(string[] args)
        {
            return args.Any(x => x.Trim().ToLower() == "-o");
        }

        public string ReadConfigOutputPath(string[] args)
        {
            var trimmedArgs = args.Select(x => x.Trim().ToLower()).ToArray();

            if (!trimmedArgs.Contains("-o"))
                return null;

            var index = Array.IndexOf(trimmedArgs, "-o");

            return args[index + 1];
        }

        public bool ShouldDisplayHelp(string[] args)
        {
            return args.Any(x => x.Trim().ToLower() == "-h" || x.Trim().ToLower() == "-help");
        }
    }
}