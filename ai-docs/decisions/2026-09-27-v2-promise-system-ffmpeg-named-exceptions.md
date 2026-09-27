---
title: "v2 keeps 1.1.0's answers except ten named exceptions, and runs the user's ffmpeg instead of a bundled one"
kind: decision
status: accepted
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, compatibility, golden, ffmpeg, dependencies]
summary: "read before changing what 2.x depends on or answers: the compatibility promise (tests/Golden/1.1.0.json except E1-E10), why MediaToolkit goes for direct ffprobe and ffmpeg processes, why netstandard2.0 plus net10.0, and the console app as a .NET tool; ruled by Mark on 2026-09-27: FFMpegCore runs ffmpeg, the tool installs it through the OS package manager"
---

# v2 promise: 1.1.0's answers except named exceptions, on the system ffmpeg

## Context

TrailerClipper 1.1.0 is a net40 library that ships MediaToolkit (abandoned since 2020, Windows only) and its old ffmpeg inside its nupkg. It has no tests; the golden capture of 2026-09-27 (148 cases) is the only record of its behaviour, and it confirmed eight bugs (plan items 1 to 8).

## Decision (accepted 2026-09-27; Mark changed the ffmpeg runner to FFMpegCore and asked for an automated install)

- The promise: every captured answer stays, except E1 to E10 in the plan.
- ffmpeg: FFMpegCore 5.5.0 runs `ffprobe` and `ffmpeg`, found by `FFmpegLocator` through `TrailerClipperOptions.FFmpegDirectory`, `TRAILERCLIPPER_FFMPEG`, `PATH` and the usual install folders. The tool installs ffmpeg through winget, Homebrew, apt or dnf; the library never downloads.
- Targets `netstandard2.0;net10.0`: the widest audience that costs nothing; net40 to net461 are dropped because current SDKs cannot build for them.
- The console app ships as the .NET tool `TrailerClipper.Tool` (`tclipper`).

## Reasons

The capture is the only proof of behaviour; the bundled ffmpeg is the package's largest liability; netstandard2.0 reaches the most callers.

## Rejected

- Direct process calls with zero dependencies (the first recommendation): Mark preferred FFMpegCore, the maintained current library.
- FFMpegCore.Extensions.Downloader: fetches ffmpeg 6.1 (2023) from ffbinaries.com with no hash or signature check.
- Bundling ffmpeg per OS: LGPL binaries of tens of megabytes and a security update duty.
- No promise, a clean-slate library: nothing would prove the rewrite.

Related: builds on [../plans/2026-09-27-modernization-and-v2-release.md](../plans/2026-09-27-modernization-and-v2-release.md); see also [../notes/2026-09-27-phase-0-survey-baseline-and-capture.md](../notes/2026-09-27-phase-0-survey-baseline-and-capture.md).
