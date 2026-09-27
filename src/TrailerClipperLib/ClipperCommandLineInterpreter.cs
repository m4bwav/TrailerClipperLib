using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TrailerClipperLib
{
    /// <summary>
    ///     Used to interpret and process command-line arguments into clipper options
    /// </summary>
    public interface ICommandLineInterpreter
    {
        /// <summary>Process the input args into clipper options; null when they are not valid.</summary>
        TrailerClipperOptions? ParseCommandLineArgs(string[]? args);
    }

    /// <summary>
    ///     Used to interpret and process command-line arguments into clipper options
    /// </summary>
    public class ClipperCommandLineInterpreter : ICommandLineInterpreter
    {
        private const string UsageMessage = "Incorrect parameters, try 'TClipper [<options>] <input_directory_path> <trailer_duration_in_milliseconds>'";

        /// <summary>
        ///     Process the input args into clipper options
        /// </summary>
        /// <param name="args">The input console app arguments</param>
        /// <returns>Valid clipper options or null if there was an issue</returns>
        public TrailerClipperOptions? ParseCommandLineArgs(string[]? args)
        {
            // 2.0.0 (E5): an empty array gets the usage message; 1.1.0 threw IndexOutOfRangeException.
            if (args == null || args.Length == 0 || (args.Length == 1 && args[0] != "-z"))
            {
                Console.WriteLine(UsageMessage);
                return null;
            }

            if (args.Length == 1 && args[0] == "-z")
                return null;

            var argsLength = args.Length;

            var options = ProcessInputOptions(args);
            if (options == null)
                return null;

            var lastArgument = args[argsLength - 1];

            if (!TryParseMilliseconds(lastArgument, out var trailerLengthInMilliSeconds))
            {
                if (!options.RemoveIntro || options.IntroLengthInMilliseconds == default)
                {
                    var errorMessage = $"Incorrect parameter, new duration in milliseconds: '{lastArgument}' cannot be parsed";
                    Console.WriteLine(errorMessage);
                    return null;
                }

                // 2.0.0 (E3): intro only, "-i <ms> <path>"; 1.1.0 then took the intro length for the path.
                options.InputPath = lastArgument;
            }
            else
            {
                var secondToLastArg = args[argsLength - 2].Trim().ToLowerInvariant();

                if (secondToLastArg != "-i" && secondToLastArg != "-intro")
                {
                    options.TrailerLengthInMilliSeconds = trailerLengthInMilliSeconds;
                    options.InputPath = args[argsLength - 2];
                }
                else
                {
                    // 2.0.0 (E3): intro only, "<path> -i <ms>"; 1.1.0 then took "-i" for the path.
                    options.InputPath = argsLength >= 3 ? args[argsLength - 3] : args[argsLength - 2];
                }
            }

            if (!Directory.Exists(options.InputPath) && !File.Exists(options.InputPath))
            {
                var errorMessage = $"Incorrect parameter, input directory or file: '{options.InputPath}' does not exist";
                Console.WriteLine(errorMessage);
                return null;
            }

            return options;
        }

        /// <summary>The config file path after a leading -c or -config, or null.</summary>
        public string? ReadConfigFilePath(string[]? args)
        {
            if (args == null || args.Length < 1)
                return null;

            var currentArg = args[0].Trim().ToLowerInvariant();

            if (currentArg != "-c" && currentArg != "-config")
                return null;

            return args[1];
        }

        /// <summary>True when -o is among the arguments.</summary>
        public bool ShouldDumpConfigFile(string[] args)
        {
            return args.Any(x => IsOption(x, "-o"));
        }

        /// <summary>The argument after -o, or null.</summary>
        public string? ReadConfigOutputPath(string[] args)
        {
            var trimmedArgs = args.Select(x => x.Trim().ToLowerInvariant()).ToArray();

            if (!trimmedArgs.Contains("-o"))
                return null;

            var index = Array.IndexOf(trimmedArgs, "-o");

            return args[index + 1];
        }

        /// <summary>True when -h or -help is among the arguments.</summary>
        public bool ShouldDisplayHelp(string[] args)
        {
            return args.Any(x => IsOption(x, "-h") || IsOption(x, "-help"));
        }

        /// <summary>True when -z is among the arguments.</summary>
        public bool ShouldCreateDefaultSampleConfig(string[] args)
        {
            return args.Any(x => IsOption(x, "-z"));
        }

        private static bool IsOption(string argument, string option) =>
            string.Equals(argument.Trim(), option, StringComparison.OrdinalIgnoreCase);

        // 2.0.0 (E4): the decimal point is "." in every culture; 1.1.0 parsed in the current culture. The styles are
        // 1.1.0's (NumberStyles.Number: signs before or after, spaces) without thousands separators, which would
        // otherwise read "2000,5" as 20005 again.
        internal static bool TryParseMilliseconds(string text, out decimal value) =>
            decimal.TryParse(text, NumberStyles.Number & ~NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);

        private static TrailerClipperOptions? ProcessInputOptions(string[] args)
        {
            var options = new TrailerClipperOptions();

            for (var index = 0; index < args.Length; index++)
            {
                var currentArg = args[index].Trim().ToLowerInvariant();

                if (currentArg.Length == 0 || currentArg[0] != '-')
                    continue;

                switch (currentArg)
                {
                    case "-d":
                        options.DeleteOriginalFiles = true;
                        break;
                    case "-i":
                    case "-intro":
                        options.RemoveIntro = true;
                        index++;
                        if (args.Length <= index)
                            break;

                        // 2.0.0 (E5): a bad intro length gets a message; 1.1.0 threw ArgumentOutOfRangeException.
                        if (!TryParseMilliseconds(args[index], out var introLengthInMilliseconds))
                        {
                            Console.WriteLine($"Incorrect parameter, intro length in milliseconds: '{args[index]}' cannot be parsed");
                            return null;
                        }

                        options.IntroLengthInMilliseconds = introLengthInMilliseconds;
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
    }
}
