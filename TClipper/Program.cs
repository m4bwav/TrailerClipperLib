using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var inputDirectoryPath = args[argsLength - 2];

            if (!Directory.Exists(inputDirectoryPath))
            {
                string errorMessage = $"Incorrect parameter, input directory: '{inputDirectoryPath}' does not exist";
                Console.WriteLine(errorMessage);
                return;
            }

            var durationArg = args[argsLength - 1];

            double newDurationInMilliseconds;

            if (!double.TryParse(durationArg, out newDurationInMilliseconds))
            {
                string errorMessage = $"Incorrect parameter, new duration in milliseconds: '{durationArg}' cannot be parsed";
                Console.WriteLine(errorMessage);
                return;
            }

            var options = ProcessInputOptions(args);

            Clipper.RemoveTrailers(inputDirectoryPath, newDurationInMilliseconds, options);
        }

        private static TrailerClipperOptions ProcessInputOptions(IEnumerable<string> args)
        {
            var options = new TrailerClipperOptions
            {
                OutputToConsole = true
            };

            var trimmedOptionArgs = PrepArguments(args);

            foreach (var loweredArg in trimmedOptionArgs)
            {
                switch (loweredArg)
                {
                    case "-m":
                    case "-multi":
                        options.MultiTaskFiles = true;
                        break;
                    case "-cf":
                    case "-consoleoff":
                        options.OutputToConsole = false;
                        break;
                    case "-a":
                    case "-all":
                        options.ProcessEveryFile = true;
                        break;
                }
            }

            return options;
        }

        private static IEnumerable<string> PrepArguments(IEnumerable<string> args)
        {
            var optionsArguments = args.Where(x => x.Trim().StartsWith("-"));

            return optionsArguments.Select(argument => argument.ToLower().Trim());
        }
    }
}