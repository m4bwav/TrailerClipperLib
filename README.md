# TrailerClipper

![A film reel, cut strips of film and a pair of scissors on an editing table under a lamp](https://raw.githubusercontent.com/m4bwav/TrailerClipperLib/master/.github/images/banner.jpg)

[![NuGet](https://img.shields.io/nuget/v/TrailerClipper)](https://www.nuget.org/packages/TrailerClipper)
[![ci](https://github.com/m4bwav/TrailerClipperLib/actions/workflows/ci.yml/badge.svg)](https://github.com/m4bwav/TrailerClipperLib/actions/workflows/ci.yml)
[![Downloads](https://img.shields.io/nuget/dt/TrailerClipper)](https://www.nuget.org/packages/TrailerClipper)

Cuts the intro and the trailer off video and audio files, one file or a whole folder at a time, with ffmpeg. Two packages:

- **`tclipper`**, a command-line tool (`TrailerClipper.Tool`), for clipping from a terminal.
- **`TrailerClipper`**, the library behind it, for .NET code (netstandard2.0 and net10.0, so .NET Framework 4.6.2+ and every current .NET, on Windows, macOS and Linux).

## The tool

```
dotnet tool install -g TrailerClipper.Tool
tclipper --install-ffmpeg        # only if ffmpeg is not installed yet
tclipper <path> <trailer_length_in_milliseconds>
```

`<path>` is a file or a folder; in a folder every `m4b`, `wav`, `mp3`, `mp4`, `flv`, `avi`, `mpg` and `mov` file is clipped. The clipped copies go to a `clipped` folder beside the input.

```
tclipper Episodes 17500                  # drop the last 17.5 s of every file in Episodes
tclipper -i 3500 Episodes 17500          # and the first 3.5 s
tclipper -i 3500 Episodes                # only the first 3.5 s
tclipper -o saved.json Episodes 17500    # clip, then save these settings
tclipper -c saved.json                   # clip again with the saved settings
tclipper -z                              # write a sample TrailerClipperConfig.json
tclipper                                 # clip with TrailerClipperConfig.json from the current folder
```

Options: `-h` help; `-o <file>` save the settings; `-c`, `-config <file>` read them; `-i`, `-intro <ms>` remove an intro; `-cf`, `-consoleoff` no progress lines; `-d` delete each original after clipping; `-m`, `-multi` clip files in parallel; `-a`, `-allfiles` try every file, whatever its extension; `-z` write a sample config. Options go before the path. Lengths are milliseconds with `.` as the decimal point.

### ffmpeg

TrailerClipper needs `ffmpeg` and `ffprobe`. It looks for them in `TrailerClipperOptions.FFmpegDirectory`, the `TRAILERCLIPPER_FFMPEG` environment variable, `PATH`, and then the folders the usual installers use (winget, Homebrew, `/usr/local/bin`, `/usr/bin`, Chocolatey, Scoop), so a fresh install works without opening a new terminal.

When ffmpeg is missing, `tclipper` offers to install it, and `tclipper --install-ffmpeg` does so directly. Either way it runs your system's package manager, which downloads a current build and checks it:

| System | Command tclipper runs |
|---|---|
| Windows | `winget install --id Gyan.FFmpeg --exact` |
| macOS | `brew install ffmpeg` |
| Linux | `sudo apt-get install -y ffmpeg`, or `sudo dnf install -y ffmpeg` |

TrailerClipper never downloads ffmpeg itself.

## The library

```
dotnet add package TrailerClipper
```

```csharp
using TrailerClipperLib;

var clipper = new TrailerClipper();
clipper.RemoveTrailers("Episodes", 17500m);                   // a file or a folder
clipper.RemoveIntros("Episodes", 3500m);
clipper.RemoveIntrosAndTrailers("Episodes", 3500m, 17500m);

clipper.RemoveTrailers(new TrailerClipperOptions("Episodes", 17500m)
{
    RemoveIntro = true,
    IntroLengthInMilliseconds = 3500m,
    OutputDirectoryPath = "Clipped",
    FFmpegDirectory = @"C:\tools\ffmpeg\bin",   // optional; see "ffmpeg" above
    OutputToConsole = false
});
```

`ClipperCommandLineInterpreter` turns the tool's arguments into `TrailerClipperOptions`; `FFmpegLocator.Find()` says where ffmpeg was found (or null).

### What happens at the edges

- A path that does not exist throws `FileNotFoundException`; a negative length throws `ArgumentOutOfRangeException`.
- A file whose intro and trailer add up to its whole length or more is skipped, with a line on the console; nothing is written for it.
- A file ffmpeg cannot read does not stop the batch: the other files are clipped, then an `InvalidOperationException` names the failures.
- Missing ffmpeg throws `ToolNotFoundException` (an `InvalidOperationException`) whose message says how to install it.
- The progress lines go to the console unless `OutputToConsole` is false.

## Upgrading from 1.x

2.0.0 gives the same answers as 1.1.0 for everything 1.1.0 did right, proven against a recording of the published 1.1.0 (`tests/Golden`), and fixes what it did wrong: see [CHANGELOG.md](CHANGELOG.md). The main change: ffmpeg is no longer bundled (1.1.0 carried a 2015 build inside MediaToolkit.dll, Windows only), so install it once as above. The namespace, class and method names are unchanged.

## What it is not

A video editor or an ffmpeg wrapper for general use. File names containing a double quote are refused. it cuts a fixed length off the start and the end of each file, re-encoding with ffmpeg's defaults for the file's extension. It runs only the ffmpeg and ffprobe it finds and never touches the network.

## Licence

MIT. ffmpeg is a separate program under its own licence (LGPL or GPL, depending on the build you install). FFMpegCore, which runs it, is MIT.
