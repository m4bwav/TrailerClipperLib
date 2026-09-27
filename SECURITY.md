# Security policy

## Reporting a vulnerability

Report it privately through GitHub: open the repository's **Security** tab and choose **Report a vulnerability**. Please do not open a public issue for a security problem.

A confirmed problem is fixed in a new release, and the advisory is published once the fix is on nuget.org. Affected versions are then marked deprecated on nuget.org with the fixed version as the alternate.

## Supported versions

Only the latest major version (2.x) gets security fixes. 1.x bundles a 2015 ffmpeg and is not supported; upgrade to 2.x.

## What this package is not

TrailerClipper runs the `ffmpeg` and `ffprobe` programs it finds (see the README's "ffmpeg" section) on the files you give it, with arguments passed as a list, never through a shell. It trusts those programs: a folder named in `FFmpegDirectory`, `TRAILERCLIPPER_FFMPEG` or `PATH` that holds a malicious `ffmpeg` will be run. Media files are parsed by ffmpeg, so keep ffmpeg updated; ffmpeg's own vulnerabilities are reported to the ffmpeg project. `tclipper --install-ffmpeg` runs your system's package manager (winget, Homebrew, apt or dnf) and nothing else; the library never downloads anything.
