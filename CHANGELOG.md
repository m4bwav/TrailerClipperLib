# Changelog

All notable changes to TrailerClipper and TrailerClipper.Tool. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.0.0-beta.1]

**Compatibility promise.** 2.0.0 gives the same results as 1.1.0 for every call recorded from the published 1.1.0 (148 cases in `tests/Golden/1.1.0.json`: return values, exceptions, console lines, config files and the length of every clipped file), except the differences listed under Changed and Fixed (E1 to E11). The namespace `TrailerClipperLib` and every public type, method and parameter name stay as they were, including the parameter names `trailerLenghtInMilliseconds` and `introLenghtInMilliseconds`.

### Added

- `TrailerClipper.Tool`, the `tclipper` command (the console app 1.x kept in the repository but never published), installed with `dotnet tool install -g TrailerClipper.Tool`. It exits with 1 when it cannot use its arguments.
- `tclipper --install-ffmpeg`, and an offer to install ffmpeg the first time it is missing, through winget, Homebrew, apt or dnf. From a script (no terminal) it asks for `--install-ffmpeg --yes` instead of installing unasked.
- `FFmpegLocator` and `TrailerClipperOptions.FFmpegDirectory`: ffmpeg is found through the option, the `TRAILERCLIPPER_FFMPEG` environment variable, `PATH`, or the usual install folders. The option is not written to config files.
- `ToolNotFoundException` when ffmpeg or ffprobe cannot be found, with install instructions in its message.
- Windows, macOS and Linux; netstandard2.0 (.NET Framework 4.6.2 and later, .NET Core 2.0 and later) and net10.0 builds; Source Link and symbols.

### Changed

- ffmpeg is no longer bundled. 1.1.0 shipped MediaToolkit.dll (with a 2015 ffmpeg.exe inside, Windows and .NET Framework only) and System.Net.Http.dll in its package without declaring them; 2.0.0 depends on FFMpegCore and runs the ffmpeg you install.
- The config file is read and written with System.Text.Json instead of JavaScriptSerializer. The written text is unchanged (including `\u003c \u003e \u0026 \u0027` for `< > & '`); reading still accepts property names in any letter case, numbers and booleans written as strings, and trailing commas.
- (E11) Config files System.Text.Json cannot read: single-quoted strings or unquoted property names now throw `JsonException` (1.1.0 accepted them); an empty file throws `JsonException` and a file holding only `null` throws `InvalidOperationException` (1.1.0 threw `NullReferenceException` for both).
- File names that contain a double quote are refused with `InvalidOperationException`: FFMpegCore passes paths in quotes, and such a name could smuggle options to ffmpeg. They occur only off Windows.
- Requires .NET Framework 4.6.2 or later (1.1.0 targeted 4.0), because current SDKs can no longer build for 4.0 to 4.6.1.
- (E9) Clipped MP3 files are the requested length: 1.1.0's ffmpeg left the encoder's padding in (8 033 ms for an 8 000 ms cut).
- (E10) On .NET Core and .NET 5+, argument exception messages use that runtime's wording, `message (Parameter 'x')`.

### Fixed

- (E1) `RemoveIntros` and `RemoveIntrosAndTrailers` work on a single file; 1.1.0 threw "does not exist" for every existing file and did nothing for a missing one.
- (E2) Clipping a path that does not exist throws `FileNotFoundException`; 1.1.0 did nothing, silently.
- (E3) The command line can remove an intro alone: `-i 3500 <path>` and `<path> -i 3500`.
- (E4) Lengths on the command line use `.` as the decimal point in every culture; 1.1.0 read `2000.5` as 20005 in German and `2000,5` as 20005 in English. Thousands separators are refused (`2000,5` and `1,000` get the usual message); signs, spaces and a trailing minus are read as before.
- (E5) An empty argument list and an unreadable intro length print a message and return null instead of throwing.
- (E6) A negative length throws `ArgumentOutOfRangeException` before any file is touched (1.1.0 treated a negative trailer as none); a file shorter than its intro plus trailer is skipped with a console line (1.1.0 wrote a file nothing could play).
- (E7) A file ffmpeg cannot read raises `InvalidOperationException` after the rest of the batch; 1.1.0 printed "Finished" and wrote nothing.
- (E8) Output paths are joined for the running system, so clipping works off Windows; on Windows the paths and console lines are the same as 1.1.0's.

### Removed

- The Visual Studio 2015 solution, `packages.config`, the `.nuspec` and the 1.0.x packages that were committed to the repository.

## [1.1.0] - 2016-09-29

- Added a `-z` option to create a default config file.

[Unreleased]: https://github.com/m4bwav/TrailerClipperLib/compare/v2.0.0-beta.1...HEAD
[2.0.0-beta.1]: https://github.com/m4bwav/TrailerClipperLib/releases/tag/v2.0.0-beta.1
[1.1.0]: https://www.nuget.org/packages/TrailerClipper/1.1.0
