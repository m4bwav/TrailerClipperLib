using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TrailerClipperLib;

namespace TClipper
{
    /// <summary>
    ///     Installs ffmpeg through the operating system's package manager (winget, Homebrew, apt or dnf), which fetches a
    ///     current build and checks its hash. Never downloads a binary itself.
    /// </summary>
    internal static class FFmpegInstaller
    {
        /// <summary>Asks once in an interactive terminal, then installs. False when declined, not interactive or failed.</summary>
        public static bool OfferInstall()
        {
            var command = InstallCommand();
            if (command == null || Console.IsInputRedirected)
                return false;

            Console.Write("Install ffmpeg now with '" + Describe(command) + "'? [Y/n] ");
            var answer = (Console.ReadLine() ?? string.Empty).Trim();
            if (answer.Length > 0 && !answer.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                return false;

            return Install(assumeYes: true);
        }

        /// <summary>Installs ffmpeg (the --install-ffmpeg option). True when ffmpeg and ffprobe are found afterwards.</summary>
        public static bool Install(bool assumeYes)
        {
            var existing = FFmpegLocator.Find();
            if (existing != null)
            {
                Console.WriteLine("ffmpeg and ffprobe are already installed in " + existing);
                return true;
            }

            var command = InstallCommand();
            if (command == null)
            {
                Console.Error.WriteLine("No supported package manager found (winget, brew, apt-get or dnf). " + FFmpegLocator.InstallHint);
                return false;
            }

            if (!assumeYes && Console.IsInputRedirected)
            {
                // Never run a package manager (sudo on Linux) unasked from a script.
                Console.Error.WriteLine("Not a terminal, so not asking: run 'tclipper --install-ffmpeg --yes' to install without a question.");
                return false;
            }

            if (!assumeYes)
            {
                Console.Write("Run '" + Describe(command) + "'? [Y/n] ");
                var answer = (Console.ReadLine() ?? string.Empty).Trim();
                if (answer.Length > 0 && !answer.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            Console.WriteLine("Running: " + Describe(command));
            int exitCode;
            try
            {
                var start = new ProcessStartInfo(command[0]) { UseShellExecute = false };
                foreach (var argument in command.Skip(1))
                    start.ArgumentList.Add(argument);

                using var process = Process.Start(start)!;
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            catch (Win32Exception e)
            {
                Console.Error.WriteLine("Could not start " + command[0] + ": " + e.Message);
                return false;
            }

            var found = FFmpegLocator.Find();
            if (found != null)
            {
                Console.WriteLine("ffmpeg is ready in " + found);
                return true;
            }

            Console.Error.WriteLine("The install finished with exit code " + exitCode + ", but ffmpeg was not found afterwards. " + FFmpegLocator.InstallHint);
            return false;
        }

        private static string[]? InstallCommand()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OnPath("winget")
                    ? new[] { "winget", "install", "--id", "Gyan.FFmpeg", "--exact", "--silent", "--accept-package-agreements", "--accept-source-agreements" }
                    : null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (OnPath("brew"))
                    return new[] { "brew", "install", "ffmpeg" };
                foreach (var brew in new[] { "/opt/homebrew/bin/brew", "/usr/local/bin/brew" })
                {
                    if (File.Exists(brew))
                        return new[] { brew, "install", "ffmpeg" };
                }

                return null;
            }

            if (OnPath("apt-get"))
                return new[] { "sudo", "apt-get", "install", "-y", "ffmpeg" };
            if (OnPath("dnf"))
                return new[] { "sudo", "dnf", "install", "-y", "ffmpeg" };
            return null;
        }

        private static bool OnPath(string name)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .Any(folder => File.Exists(Path.Combine(folder.Trim('"'), name)) || File.Exists(Path.Combine(folder.Trim('"'), name + ".exe")));
        }

        private static string Describe(string[] command) => string.Join(" ", command);
    }
}
