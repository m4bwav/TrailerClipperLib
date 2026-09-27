---
title: "Phase 0 survey: registry, repository, baseline, capture and security of TrailerClipper 1.1.0"
kind: note
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: 2027-03-27
tags: [survey, baseline, v2, golden, ffmpeg, mediatoolkit, cli, security]
aliases: [survey, baseline, capture, MediaToolkit, ffprobe, TClipper, JavaScriptSerializer]
summary: "read before the plan or the rewrite: what 1.1.0 is and ships (a net40 DLL with MediaToolkit and its old ffmpeg bundled, no declared dependency), why the old solution cannot build on SDK 10, the golden capture (148 cases on net48 with real WAV and MP3 fixtures) and the bugs it confirmed, no dependents, no dead services or secrets"
---

# Phase 0 survey: TrailerClipper 1.1.0

## Summary

1.1.0 is a net40 DLL with MediaToolkit and its old ffmpeg bundled; it cannot be built on SDK 10, has no tests, no dependents and no security debt; the golden capture confirmed eight bugs listed below.

Raw output of `survey-nuget.sh TrailerClipper m4bwav/TrailerClipperLib`: [survey-2026-09-27.md](survey-2026-09-27.md).

## Registry

- Package `TrailerClipper`, owner `rogersm0`, ten versions 1.0.0 to 1.1.0 (1.0.6 never published), all listed, 2015-09-29 to 2016-09-29. 16 908 downloads in total, spread almost evenly (1 593 to 1 922 per version): the pattern of mirrors and crawlers, not users.
- 1.0.3 to 1.0.9 declare `MediaToolkit [1.0.4.11, )`. 1.1.0 declares nothing and instead ships lib/MediaToolkit.dll (10.8 MB; MediaToolkit embeds an ffmpeg.exe and unpacks it to the temp folder), lib/System.Net.Http.dll and lib/TrailerClipperLib.dll, all in `lib/` with no target framework folder. The nuspec has the placeholder `iconUrl` `http://ICON_URL_HERE_OR_DELETE_THIS_LINE` and the deprecated `licenseUrl`.
- MediaToolkit (AydinAdn/MediaToolkit) was last pushed 2020-09-27, 70 open issues, net40, Windows only. FFMpegCore 5.5.0 (MIT, netstandard2.0, depends on Instances and System.Text.Json) was released 2026-09-20; its repository was pushed 2026-09-23.

## Repository

- m4bwav/TrailerClipperLib, created 2015-09-27, last push 2016-09-29, 1 star, 1 fork (DaveCS1/TrailerClipperLib, pushed the same day as the last release, no changes of its own). One branch `master`; no issues, no pull requests, no alerts, no webhooks, no secrets, no workflows, no tags or releases, no environments. Default workflow permissions `write`; secret scanning, push protection and Dependabot security updates off.
- Two projects in `TrailerClipperLib.sln` (VS 2015): `TrailerClipperLib` (library, net40, packages.config with MediaToolkit 1.0.4.11 targeting net452, `System.Web.Extensions` for `JavaScriptSerializer`, embedded `help.txt`, a `.nuspec`) and `TClipper` (console app, net452, never published: no release, no tool package). The five nupkgs 1.0.0 to 1.0.4 are committed next to the library despite `*.nupkg` in `.gitignore`.
- No tests. No README images or badges (`check-readme-images.mjs --registry nuget`: 0 images, exit 0). The README escapes angle brackets as `&lt;` and pastes the help text.
- No credentials in files or history (`git log -p --all` grep for key, token, password, secret: only the VS .gitignore's comments).
- Dependents: none. GitHub code search for `TrailerClipperLib` finds only this repository.

## Baseline

`dotnet build TrailerClipperLib.sln` on SDK 10.0.401: fails with MSB3644 (no reference assemblies for .NET Framework 4.0 and 4.5.2). Expected by the NuGet reference; the old code has no tests to run anyway, so the golden capture is the only behaviour record.

## Golden capture

- `tests/Golden/Capture/` (Program.cs and Capture.csproj, as run) references the published `TrailerClipper` `[1.1.0]` from nuget.org in a scratch net48 console project (the template's net10.0 cannot load a net40 assembly that needs `System.Web.Extensions`). Fixtures in `tests/Golden/fixtures/` were made with ffmpeg 9.0.1: `a.wav` (10 s, 44.1 kHz mono sine), `b.mp3` (10 s, 64 kb/s), `notes.txt`.
- 148 cases: `ParseCommandLineArgs` on 22 argument lists under en-US and de-DE, the five other interpreter methods on 14 lists, the options constructors, the help text, and 28 clipping cases, each in a fresh copy of the fixtures, recording the result or exception, the console output (scratch paths replaced by `<work>`) and the resulting file tree with each media file's duration from ffprobe in milliseconds. Output `tests/Golden/1.1.0.json`; two runs identical apart from the date line.
- Confirmed quirks, each a row in the plan:
  1. `RemoveIntros` and `RemoveIntrosAndTrailers` on an existing file throw "File or directory at: media/a.wav does not exist" (the check is inverted); on a missing path they do nothing, silently. `RemoveTrailers` on a missing path also does nothing.
  2. Intro-only on the command line cannot work: `-i 1500 media` looks for a path `1500`, `media -i 1500` for `-i`, `-i 0 media` reports that `media` cannot be parsed. The help says `-i` works "with or without the trailer trimming".
  3. Numbers parse in the current culture: `2000.5` is 20005 under de-DE and `2000,5` is 20005 under en-US.
  4. An empty argument array throws `IndexOutOfRangeException`; `-i abc` throws `ArgumentOutOfRangeException` instead of printing a message.
  5. A trailer longer than the file writes an output file ffprobe cannot read; a negative trailer is accepted and keeps the whole file.
  6. `ProcessEveryFile` prints "Finished on trimming file" for `notes.txt` and writes nothing: ffmpeg's failure is swallowed.
  7. The default output folder is `DirectoryName + "\clipped"` and the output path is joined with a backslash: Windows only.
  8. Clipped WAV durations are exact (10 000 minus the trailer, minus the intro); clipped MP3 durations carry the encoder's padding (8 033 for 8 000) and depend on the ffmpeg build.
  9. Unknown options such as `-x` are ignored; `-z` with a path still parses and clips.
- The library writes to the console (`OutputToConsole`, default true) and reads and writes `TrailerClipperConfig.json` in the current directory through `JavaScriptSerializer` (the file's exact text is recorded).

Related: builds on [survey-2026-09-27.md](survey-2026-09-27.md); see also [../plans/2026-09-27-modernization-and-v2-release.md](../plans/2026-09-27-modernization-and-v2-release.md).
