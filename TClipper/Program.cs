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
            if (args == null || args.Length < 2 || args.Length > 3)
            {
                const string errorMessage = "Incorrect parameters, try 'TClipper <input_directory_path> <trailer_duration_in_milliseconds>'";
                Console.WriteLine(errorMessage);
                return;
            }

            var inputDirectoryPath = args[0];

            if (!Directory.Exists(inputDirectoryPath))
            {
                string errorMessage = $"Incorrect parameter, input directory: '{inputDirectoryPath}' does not exist";
                Console.WriteLine(errorMessage);
                return;
            }

            var durationArg = args[1];

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

        private static TrailerClipperOptions ProcessInputOptions(string[] args)
        {
            var options = new TrailerClipperOptions
            {
                OutputToConsole = true
            };

            var optionsArguments = args.Where(x => x.Trim().StartsWith("-"));

            var trimmedOptionArgs = optionsArguments.Select(argument => argument.ToLower().Trim());

            foreach (var loweredArg in trimmedOptionArgs)
            {
                switch (loweredArg)
                {
                    case "-multi":
                        options.MultiTaskFiles = true;
                        break;
                    case "-cf":
                        options.OutputToConsole = false;
                        break;
                    case "-all":
                        options.ProcessEveryFile = true;
                        break;
                }
            }

            return options;
        }
    }
}