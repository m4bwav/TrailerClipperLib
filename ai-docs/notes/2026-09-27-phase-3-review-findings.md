---
title: "Phase 3 review findings on v2 (read-only subagent, 2026-09-27)"
kind: note
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: 2027-03-27
tags: [review, v2, phase-3, compatibility, security]
summary: "read before changing duration handling, the config file code, the ffmpeg locator or the tool's error handling: the 12 findings of the independent review (ffmpeg rounds the Duration line, JavaScriptSerializer's escaping and lenient reading, AggregateException from -m, quote injection through FFMpegCore, relative PATH entries, 1.1.0's parameter names, NumberStyles) and what each became"
---

# Phase 3 review findings (2026-09-27)

## Summary

A general-purpose subagent, read-only, ran the published 1.1.0 (net48) and the v2 build side by side for about 4 minutes and reported 12 findings that the 172 passing tests had missed. All 12 were fixed or recorded, and each has a test in `tests/TrailerClipperLib.Tests/ReviewTests.cs`. The golden files did not change. Its probes are in the session scratchpad (review/), not in the repository.

## Findings and dispositions

1. **bug:** the duration was truncated to hundredths, but ffmpeg's `Duration:` line (which MediaToolkit parsed) rounds: `av_dump_format` adds 5000 µs first. A 1.2375 s file came out 10 ms short. **Fixed** in `FFmpegEngine.DurationAsMediaToolkitReadIt`; tested at the rounding edges.
2. **bug:** the written config text differed. JavaScriptSerializer escaped `< > & '` as `\u003c \u003e \u0026 \u0027`. **Fixed** in `TrailerClipper.ConfigText`, which post-processes the JSON; the property names are fixed ASCII, so those characters occur only in values.
3. **bug:** config files 1.1.0 read were rejected: numbers and booleans as strings, trailing commas. **Fixed** with `NumberHandling.AllowReadingFromString`, `AllowTrailingCommas` and a lenient boolean converter. Single quotes and unquoted names cannot be read by System.Text.Json: **named E11** in the CHANGELOG.
4. **bug:** with `-m`, `Parallel.ForEach` wrapped `ToolNotFoundException` in `AggregateException`, so tclipper crashed instead of offering the install. **Fixed:** unwrapped in `ExecuteDirectoryMode`.
5. **risk, security:** FFMpegCore quotes paths without escaping, so a `"` in a file name (legal off Windows) could inject ffmpeg options. **Fixed:** such names are refused with `InvalidOperationException` (collected like other per-file failures).
6. **risk, security:** relative PATH entries (`.`) were searched, which could run an ffmpeg from the current folder. **Fixed:** only rooted entries, and `Find` returns full paths.
7. **risk, API:** the class method `RemoveTrailers(string directoryPath, decimal milliseconds)` had taken the interface's parameter names. **Fixed:** 1.1.0's names restored (CA1725 suppressed there with the reason). New test `PublicApiTests` checks all 41 public members of 1.1.0 (`PublicApi-1.1.0.txt`, listed by reflection from the published DLL) with their parameter names.
8. **risk:** `NumberStyles.Float` differed from 1.1.0's `Number` beyond E4 (`2000-`, `1e3`, `1,000`). **Fixed:** `Number` without `AllowThousands`; E4 now names the refused thousands separators.
9. **nit:** a config holding `null` did nothing silently. It now throws `InvalidOperationException` ("holds no clipping settings"); an empty file throws `JsonException`. **Named E11.**
10. **nit:** the tool crashed with a stack trace on malformed JSON, `-c` alone and `-o` last. **Fixed:** a message and exit code 1.
11. **nit:** `--install-ffmpeg` with redirected stdin ran `sudo apt-get install -y` unasked. **Fixed:** it needs `--yes` when not in a terminal.
12. **test gap:** covered by `ReviewTests` (items 1 to 9) and the tool changes. The golden fixtures cannot change, so the new cases live in unit tests.

## What the review found clean

The `-ss`/`-t` placement matches MediaToolkit's (FFMpegCore emits `-ss … -i "in" -t … "out" -y`). DeleteOriginalFiles is skipped when a cut fails. The winget folders are right. The library makes no network calls. The workflows use least privilege and SHA pins, and publishing runs no repository code. Nullable annotations only warn implementers.

Related: builds on [../plans/2026-09-27-modernization-and-v2-release.md](../plans/2026-09-27-modernization-and-v2-release.md); see also [2026-09-27-phase-0-survey-baseline-and-capture.md](2026-09-27-phase-0-survey-baseline-and-capture.md).
