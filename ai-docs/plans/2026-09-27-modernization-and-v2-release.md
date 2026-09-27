---
title: Modernization and v2 release
kind: plan
status: active
date: 2026-09-27
verified: 2026-09-27
stale_after: never
tags: [v2, plan, nuget, github-actions, tests, release, ffmpeg]
summary: "the living plan for TrailerClipper 2.0.0: survey, what 1.1.0 gets wrong, decisions D1-D16 and exceptions E1-E10, the v2 API, build and test strategy, phases 0-7 with checkboxes, security, verification checklist"
---

# Modernization and v2.0.0 release plan: TrailerClipper

The first NuGet run of the package-modernize skill (references/nuget.md) on Mark's NuGet package TrailerClipper (repository m4bwav/TrailerClipperLib). Evidence goes to [../log.md](../log.md); the survey is [../notes/2026-09-27-phase-0-survey-baseline-and-capture.md](../notes/2026-09-27-phase-0-survey-baseline-and-capture.md).

## Status

Active. Phase 1 reached on 2026-09-27; waits for Mark's ruling on the decisions table (above all D5, D8 and D14).

## Goal

- One library, `TrailerClipper` 2.0.0, for `netstandard2.0` and `net10.0`, that runs on Windows, Linux and macOS, with the ffmpeg the user already has instead of a bundled 2015 build.
- The same answers as 1.1.0 for every captured case, except the named exceptions E1 to E10, proven by `tests/Golden/1.1.0.json`.
- Current tooling: SDK 10, analyzers as errors, lock files, CI on three OSes, SHA-pinned actions, Dependabot.
- Released through nuget.org Trusted Publishing from `release.yml`, gated by the `nuget` environment with Mark as reviewer, then verified from the registry.

## Where it stands (survey 2026-09-27)

| Fact | Value | Evidence |
|---|---|---|
| Published version, date, downloads, dependents | 1.1.0, 2016-09-29; 16 908 in total, even across versions (mirrors); no dependents found | survey note |
| Source, build, tests, language level | VS 2015 solution, net40 library plus net452 console app `TClipper` (never published); no tests; C# 6 | survey note |
| Entry points | `TrailerClipper` (implements `ITrailerClipperService`), `TrailerClipperOptions`, `ClipperCommandLineInterpreter` (`ICommandLineInterpreter`), `TcHelpReader`; the README documents only the console app | source |
| Runtime dependencies | MediaToolkit 1.0.4.11 (abandoned, last push 2020, net40, Windows only, bundles an old ffmpeg), copied into the 1.1.0 nupkg with System.Net.Http.dll instead of declared; `System.Web.Extensions` (JavaScriptSerializer, .NET Framework only) | survey |
| Issues, pull requests, forks | none, none, DaveCS1 (no own commits) | survey |
| Alerts, webhooks, secrets, security features | 0, 0, 0; scanning, push protection and private reporting off; workflow permissions write | survey |
| Dead services | none | survey |
| README images and badges | none | check-readme-images.mjs, exit 0 |
| Leaked credentials | none in files or history | survey note |
| Baseline | cannot build on SDK 10 (MSB3644, net40 and net452 reference packs) | log |
| Golden capture | 148 cases, net48, WAV and MP3 fixtures, durations by ffprobe; deterministic over two runs | tests/Golden |

## What 1.1.0 gets wrong, confirmed, and what v2 does

1. `RemoveIntros` and `RemoveIntrosAndTrailers` throw "does not exist" for every existing file, and silently do nothing for a missing path (inverted check). **Fix (E1):** existing files work; a missing path throws `FileNotFoundException`.
2. `RemoveTrailers` on a missing path silently does nothing. **Fix (E2):** throws `FileNotFoundException`, as E1.
3. The command line cannot remove an intro alone (`-i 1500 media`, `media -i 1500`). **Fix (E3):** both forms work; the trailer is then 0.
4. Numbers parse in the current culture (`2000.5` is 20005 under de-DE). **Fix (E4):** invariant culture: `.` is the decimal point everywhere, and `2000,5` is refused with the usual message.
5. An empty argument array throws `IndexOutOfRangeException`; `-i abc` throws `ArgumentOutOfRangeException`. **Fix (E5):** both print the usage message and return null, like every other bad input.
6. A trailer plus intro at or past the file's length writes an unreadable file; a negative trailer is taken as 0. **Refuse (E6):** a negative length throws `ArgumentOutOfRangeException` up front; a cut that leaves no media skips the file with a console line and writes nothing.
7. ffmpeg failures are swallowed ("Finished on trimming file" for `notes.txt`, nothing written). **Fix (E7):** a failed ffmpeg run throws `InvalidOperationException` with the file name and the last lines of ffmpeg's error output, after the other files of the batch are done.
8. Paths are joined with `\`, so it only works on Windows. **Fix (E8):** `Path.Combine`; the default output folder stays `clipped` beside the input; on Windows the console text is unchanged.
9. MP3 output durations depend on the ffmpeg build (8 033 ms for an 8 000 ms cut with MediaToolkit's). **Named (E9):** v2 uses whatever ffmpeg the user has; the golden test compares WAV durations exactly and MP3 durations by the value recorded here only when CI's ffmpeg gives it; otherwise the difference is listed in the changelog with the measured values. (Decided in Phase 2 from a real run, not assumed.)
10. .NET Core words exception messages differently (`(Parameter 'x')` instead of a second line `Parameter name: x`). **Named (E10):** the net48 test run compares messages exactly; net10.0 compares type and first line. Runtime formatting, not package behaviour.

Kept exactly: every public name and signature (including the misspelled parameters `trailerLenghtInMilliseconds` and `introLenghtInMilliseconds`, since callers can pass named arguments), the help text, the console lines, the `TrailerClipperConfig.json` file name, its property names and text, the default sample config, unknown options ignored, `-z` behaviour.

## Decisions (recommendation first; the maintainer rules in the plan review, silence means the recommendation stands)

| # | Question | Recommendation | Why | Alternative |
|---|---|---|---|---|
| D1 | Compatibility promise | 2.0.0 gives 1.1.0's answers for every case in `tests/Golden/1.1.0.json` except E1 to E10; a later fix that changes an old answer goes under a new name | The capture is the only record of behaviour (no old tests) | Treat 2.0.0 as a new library with no promise (nothing proves the rewrite then) |
| D2 | Package shape | One assembly `TrailerClipperLib.dll`, namespace `TrailerClipperLib` (unchanged), lib/netstandard2.0 and lib/net10.0, README, icon-free, `PackageLicenseExpression MIT`, Source Link, snupkg | Consumers keep their `using`; the bundled DLLs and TFM-less `lib/` go | Rename the namespace to `TrailerClipper` (breaks every caller for nothing) |
| D3 | Edges | Fix or refuse per list above (E1 to E8), name E9 and E10 | Each old behaviour there is a bug a caller cannot want | Keep them all bit for bit under the old names and add fixed methods (doubles the API for a package with no dependents) |
| D4 | Major? | Yes, 2.0.0 | The ffmpeg model, targets and E1 to E8 are breaking | A 1.1.1 that only fixes metadata (leaves it Windows and net40 only) |
| D5 | How to run ffmpeg | **Zero dependencies: call `ffprobe` and `ffmpeg` as processes** (from `PATH`, or a new `TrailerClipperOptions.FFmpegDirectory` / env var `TRAILERCLIPPER_FFMPEG`), the same `-ss` / `-t` cut MediaToolkit made | About 80 lines replace a 10.8 MB abandoned DLL; nothing to conflict with in a consumer's graph; works wherever ffmpeg does; ffmpeg 9 is current and packaged by winget, brew and apt | FFMpegCore 5.5.0 (maintained, MIT, the current .NET trend, but two transitive packages for two command lines); or keep bundling an ffmpeg per OS (LGPL binaries of 80+ MB each, a security update burden) |
| D6 | Names | Keep all; add only `FFmpegDirectory` (needed by D5). The console app becomes a .NET tool (D14) | No gold-plating; a package with no dependents needs no async API yet | Add `RemoveTrailersAsync` with cancellation (later, on request) |
| D7 | Errors | Keep the old exception types where they exist; E1, E2, E6, E7 add `FileNotFoundException`, `ArgumentOutOfRangeException`, `InvalidOperationException` | Silent failure was the worst of 1.1.0 | A result object per file (new API) |
| D8 | Frameworks and matrix | Library `netstandard2.0;net10.0` (overlay default); tests `net10.0` everywhere and `net48` on Windows; CI on ubuntu, windows and macos | netstandard2.0 keeps .NET Framework 4.6.2+ and every modern .NET: the widest audience that costs nothing. net40 to net461 are dropped because their reference packs are gone from current SDKs and Microsoft ended their support in 2022 | Add `net8.0` (leaves support 2026-11-10; nothing it adds over netstandard2.0 here) |
| D9 | Tooling | SDK 10 `global.json` `latestFeature`, `LangVersion latest`, nullable on, analyzers `latest-recommended` as errors, `dotnet format` in CI, NUnit 4, lock files; config JSON via System.Text.Json (in-box on net10.0, a package reference on netstandard2.0 only), case-insensitive on read like JavaScriptSerializer | Skill defaults; System.Text.Json is Microsoft's own and replaces a Framework-only serializer | Hand-written JSON (risky), Newtonsoft.Json (a third-party dependency) |
| D10 | Lockfile and bot pull requests | `packages.lock.json` committed, `--locked-mode` in CI; no bot pull requests exist | Defaults | none |
| D11 | Dead services, badges | None dead; README gains the three badges (NuGet version, CI, downloads) | Default row | none |
| D12 | Old files to remove | Both `.csproj` in the old format, `.sln` (replaced by `.slnx`), `packages.config`, `.nuspec`, Properties/AssemblyInfo.cs, `App.config`, the five committed nupkgs 1.0.0 to 1.0.4 | Build artefacts and formats SDK 10 cannot use | none |
| D13 | Release | 2.0.0-beta.1 as the rehearsal, then 2.0.0; `nuget` environment with Mark as required reviewer (created by the run with `gh`); **Mark adds the nuget.org Trusted Publishing policy** (owner m4bwav, repository TrailerClipperLib, workflow release.yml, environment nuget, package glob `TrailerClipper*`) | The overlay covers npm publishers only; the glob covers D14's tool package too | Real versions only, as the two earlier NuGet runs did (the reference asks for a prerelease) |
| D14 | The console app | **Publish it as a .NET tool, package `TrailerClipper.Tool`, command `tclipper`, net10.0**, same arguments and help | The README has always described the console app, yet nobody could install it; `dotnet tool install -g` is the current way to ship one | Keep TClipper in the repository as an unpublished sample |
| D15 | Dependents and 1.x | None to move. After 2.0.0, Mark deprecates 1.x on nuget.org (UI only): reason Legacy, alternate `TrailerClipper` 2.0.0, message "1.x bundles an old ffmpeg and runs only on Windows .NET Framework; use 2.x" | Points the few real users at the fix | Unlist 1.x (hides it from search, breaks nobody, but loses the history) |
| D16 | Default branch, repo settings | Keep `master`; ruleset (no deletion, no force push, required check `ci`, admin bypass); tag ruleset admins only; secret scanning, push protection, private vulnerability reporting on; workflow permissions read; homepage the nuget.org page | Skill defaults | none |

## Proposed public API (v2)

Unchanged from 1.1.0: `ITrailerClipperService` and `TrailerClipper` (`RemoveTrailers` x4, `RemoveTrailersWithOptionsFile`, `RemoveIntros`, `RemoveIntrosAndTrailers`, `CreateDefaultSampleConfig`), `TrailerClipperOptions` (three constructors, eleven properties, `IsInputPathADirectory`), `ICommandLineInterpreter` and `ClipperCommandLineInterpreter` (six methods), `TcHelpReader.ReadHelpFileText`. Added: `TrailerClipperOptions.FFmpegDirectory` (string, null means `TRAILERCLIPPER_FFMPEG` or `PATH`). Throws: see E1, E2, E6, E7; when ffmpeg or ffprobe cannot be found, `InvalidOperationException` naming both ways to point at it. Package validation runs against 1.1.0 only as a report (the old assembly is net40), so the breaks are listed by hand in the changelog.

## Build and package specifics

src/TrailerClipperLib/TrailerClipperLib.csproj (PackageId `TrailerClipper`), `src/TrailerClipper.Tool/` (PackAsTool, PackageId `TrailerClipper.Tool`, ToolCommandName `tclipper`), `tests/TrailerClipperLib.Tests/` (NUnit; golden, unit, CLI), `tests/Golden/` (capture as run, JSON, fixtures), `TrailerClipper.slnx`, `Directory.Build.props`, `global.json`, `.editorconfig`, `.gitattributes`; templates from the skill's templates/nuget.

## Phases

### Phase 0: survey and baseline (2026-09-27, no package code changed)
- [x] Cloned to D:\m4bwa\Claude\Projects\Ai\labs\TrailerClipperLib; survey in ai-docs/notes
- [x] Old build: fails on SDK 10 (MSB3644); no old tests
- [x] Golden capture from the published 1.1.0 under tests/Golden (the JSON, Capture/ and fixtures/ stay as they are from the Phase 0 commit on)
- [x] everlast registered (mode repo); AGENTS.md, CLAUDE.md (AGENTS.md import line), Copilot pointer
### Phase 1: plan
- [x] This plan and the decision record. **Stop**: Mark rules on the table, above all D5 (no bundled ffmpeg), D8 (net40 to net461 dropped), D13 (the Trusted Publishing policy is his to add) and D14 (a second package).
### Phase 2: rewrite on branch v2
- [ ] Remove the D12 files; add the templates
- [ ] Golden test first, green on the first build; canary (a planted line in src turns it red, reverted, green; both runs logged); golden files unchanged since the Phase 0 commit
- [ ] Source, unit and CLI tests, README, CHANGELOG, SECURITY.md, AGENTS.md
- [ ] Verified on Windows (net10.0 and net48) locally and from a fresh clone; Linux and macOS in CI
- [ ] Workflows (ci, release, verify-published) and Dependabot, SHA-pinned, actionlint and check-workflow-shell clean
- [ ] Pushed; pull request with a "For review" list. **Stop.**
### Phase 3: review
- [ ] Independent read-only review (prompts/review-subagent.md with the NuGet substitutions); findings fixed or answered
### Phase 4: CI, settings, merge, cleanup
- [ ] CI green; rulesets; `nuget` environment; merge after Mark's review; settings and scanning
### Phase 5: release rehearsal
- [ ] Mark adds the Trusted Publishing policy. **Stop.**
- [ ] v2.0.0-beta.1 tagged after green; **stop** for the approval; verified from nuget.org
### Phase 6: release
- [ ] Changelog dated; v2.0.0 tagged; **stop** for the approval; verified; GitHub Release; 1.x deprecated by Mark
### Phase 7: wrap-up
- [ ] HANDOFF, inventory row, lessons into the skill, kickoff corrections

## Test strategy

| Layer | What it proves | How | Runs where |
|---|---|---|---|
| Golden | 1.1.0's answers except E1 to E10 | NUnit reads tests/Golden/1.1.0.json, replays each case in a temp copy of the fixtures, compares result, console and file tree | net10.0 on three OSes, net48 on Windows; ffmpeg installed in CI |
| Unit | argument parsing, ffmpeg command lines, path joining, config JSON | NUnit, no ffmpeg needed (the process runner is an internal seam) | everywhere |
| Tool | `tclipper` arguments, help, exit codes | runs the built tool against the fixtures | three OSes |
| Package | contents, metadata, both TFMs | `dotnet pack`, nupkg listing test, package validation | CI |
| Consumer | installs and works from the registry | verify-published.yml: fresh net10.0 and net48 projects, `dotnet tool install` | after each release |

## Pull requests, issues and forks: disposition

None open or closed. Fork DaveCS1/TrailerClipperLib: nothing to do.

## Security

No tokens, webhooks or alerts. The old package ships an old ffmpeg.exe inside MediaToolkit.dll that it unpacks and runs: v2 removes it (D5), which is the main security gain. Scanning, push protection, private reporting, read-only workflow permissions, SHA pins, `persist-credentials: false`, id-token set to write only in the publish job, the `nuget` environment gate. The library runs only the ffmpeg and ffprobe the caller points at, passes arguments as a list (no shell), and never downloads anything. SECURITY.md with private reporting.

## Badges and images: disposition

| Image or badge | What it shows now | Decision | New URL or reason |
|---|---|---|---|
| (none in the old README) | | | |
| NuGet version (new) | | add | img.shields.io/nuget/v/TrailerClipper |
| CI (new) | | add | the ci.yml badge |
| Downloads (new) | | add | img.shields.io/nuget/dt/TrailerClipper |

## Verification checklist

The NuGet reference's checklist, plus: `tclipper -h` from an installed tool prints the 1.1.0 help text; the golden test is green on three OSes; `unzip -l` shows no MediaToolkit.dll.

## Risks and open points

- E9 cannot be settled until CI's ffmpeg has run the MP3 cases.
- nuget.org lists the tool package under the same owner; the Trusted Publishing policy must cover both ids.
- A 10-second WAV in the repository (882 KB) is the price of a real clip test; accepted.

## Next single action

Mark rules on the decisions table; then Phase 2 starts on branch v2.
