using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TrailerClipperLib
{
    /// <summary>
    ///     Finds the folder that holds ffmpeg and ffprobe. Looks only at the file system; never downloads anything.
    /// </summary>
    public static class FFmpegLocator
    {
        /// <summary>The environment variable that may name the ffmpeg folder.</summary>
        public const string EnvironmentVariable = "TRAILERCLIPPER_FFMPEG";

        /// <summary>
        ///     The one-line install instructions for this operating system, used in error messages and by the tclipper tool.
        /// </summary>
        public static string InstallHint =>
            "ffmpeg and ffprobe were not found. Install ffmpeg (Windows: winget install --id Gyan.FFmpeg -e; macOS: brew install ffmpeg; " +
            "Linux: sudo apt install ffmpeg or sudo dnf install ffmpeg), or run 'tclipper --install-ffmpeg', " +
            "or point TrailerClipperOptions.FFmpegDirectory or the " + EnvironmentVariable + " environment variable at the folder that holds them.";

        /// <summary>
        ///     Returns the first folder that holds both ffmpeg and ffprobe, or null. The order: <paramref name="preferredDirectory" />,
        ///     the TRAILERCLIPPER_FFMPEG environment variable, every PATH entry, then the usual install folders.
        /// </summary>
        public static string? Find(string? preferredDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredDirectory))
                return HoldsBoth(preferredDirectory!) ? Path.GetFullPath(preferredDirectory!) : null;

            var found = CandidateDirectories().FirstOrDefault(HoldsBoth);
            return found == null ? null : Path.GetFullPath(found);
        }

        /// <summary>
        ///     The folders <see cref="Find" /> searches when no folder is given, in order.
        /// </summary>
        public static IEnumerable<string> CandidateDirectories()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                yield return fromEnvironment!;

            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            // Only absolute entries: a relative one such as "." would run whatever ffmpeg sits in the current folder.
            foreach (var entry in path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var folder = entry.Trim().Trim('"');
                if (Path.IsPathRooted(folder))
                    yield return folder;
            }

            foreach (var folder in WellKnownDirectories())
                yield return folder;
        }

        private static IEnumerable<string> WellKnownDirectories()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // winget adds links here, but a shell started before the install does not see the new PATH entry.
                yield return Path.Combine(local, "Microsoft", "WinGet", "Links");

                var packages = Path.Combine(local, "Microsoft", "WinGet", "Packages");
                if (Directory.Exists(packages))
                {
                    foreach (var package in SafeDirectories(packages, "Gyan.FFmpeg*").Concat(SafeDirectories(packages, "BtbN.FFmpeg*")))
                    {
                        foreach (var build in SafeDirectories(package, "ffmpeg-*").OrderByDescending(d => d, StringComparer.Ordinal))
                            yield return Path.Combine(build, "bin");
                    }
                }

                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "chocolatey", "bin");
                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "shims");
            }
            else
            {
                yield return "/opt/homebrew/bin";
                yield return "/usr/local/bin";
                yield return "/usr/bin";
                yield return "/snap/bin";
            }
        }

        private static string[] SafeDirectories(string folder, string pattern)
        {
            try
            {
                return Directory.GetDirectories(folder, pattern);
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static bool HoldsBoth(string folder)
        {
            try
            {
                return Executable(folder, "ffmpeg") && Executable(folder, "ffprobe");
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool Executable(string folder, string name) =>
            File.Exists(Path.Combine(folder, name)) || File.Exists(Path.Combine(folder, name + ".exe"));
    }
}
