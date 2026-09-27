# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

Two NuGet packages from one repository: `TrailerClipper` (the library, namespace `TrailerClipperLib`, netstandard2.0 and net10.0) and `TrailerClipper.Tool` (the `tclipper` .NET tool, net10.0). Both cut intros and trailers off video and audio files with ffmpeg, which they find on the machine (FFMpegCore runs it). 1.1.0 (2016, net40, MediaToolkit bundled) is the published version until 2.0.0 ships. The plan is `ai-docs/plans/2026-09-27-modernization-and-v2-release.md`; start with `ai-docs/HANDOFF.md`.

## Rules

- **The promise.** 2.x gives 1.1.0's answers for every case in `tests/Golden/1.1.0.json` except E1 to E10 (the plan, CHANGELOG.md, and the exception table in `tests/TrailerClipperLib.Tests/GoldenTests.cs`, which is the only place a difference may be allowed). A fix that would change an old answer needs a decision entry, a changelog line and a named exception.
- **The golden files never change.** `tests/Golden/1.1.0.json`, `tests/Golden/Capture/` and `tests/Golden/fixtures/` were captured from the published 1.1.0 on 2026-09-27. When the golden test fails, fix `src/`; never regenerate the recording from this repository's code. `GoldenRunner.cs` is generated from the capture program by a mechanical rewrite (its header says how); never edit its cases.
- **Public API.** Every 1.x name stays, including the misspelled parameter names (callers may use named arguments). The public instance methods stay instance methods (CA1822 is off for that reason).
- **The library never touches the network.** It runs only the ffmpeg and ffprobe that `FFmpegLocator` finds. Installing ffmpeg belongs to the tool (`FFmpegInstaller`), and only through the OS package manager.
- **Nothing reaches nuget.org without the maintainer.** No API key is stored anywhere; `release.yml` publishes through Trusted Publishing (policy on nuget.org: owner m4bwav, repository TrailerClipperLib, workflow release.yml, environment nuget, packages `TrailerClipper*`) from a job that waits at the `nuget` environment for the maintainer's approval.
- **Releases follow one ritual.** Update `CHANGELOG.md` (a release heading carries its date), set the same `<Version>` in both `src/*/*.csproj`, merge, wait for `ci` to be green on `master`, then tag `v<version>` and push the tag. `release.yml` checks the tag against both versions, builds, tests, packs, attests, waits for the approval, pushes and creates the GitHub Release. Then run `verify-published` with the version. Tag only after green.
- **Dependencies.** Lock files are committed; after changing a PackageReference run `dotnet restore` and commit the lock files; CI restores with `--locked-mode`. Dependabot runs weekly with a three-day cooldown. Actions are pinned to commit SHAs.
- **Research beats recall.** SDK, package and action versions change; re-verify any version older than three months.
- **Document for handoff.** Anything learned, decided or built goes into `ai-docs/` before you finish; rewrite `ai-docs/HANDOFF.md` when work is left unfinished.
- **No AI attribution anywhere.**
- **Line endings.** Files are LF (`.gitattributes`, `.editorconfig`), except `src/TrailerClipperLib/help.txt`, which keeps 1.1.0's BOM and CRLF bytes (`-text`). On Windows with `core.autocrlf`, check new files with node (count byte 13).

## Commands

```
dotnet restore --locked-mode
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test -c Release                     # net10.0 and net48 (net48 runs only on Windows); needs ffmpeg and ffprobe
dotnet restore -p:AuditPipeline=true --force
dotnet pack -c Release -o artifacts        # TrailerClipper and TrailerClipper.Tool
```

## Layout and traps

- `src/TrailerClipperLib/`: `TrailerClipper` (the service), `TrailerClipperManager` (per-file work), `MediaEngine.cs` (the internal `IMediaEngine` seam and the FFMpegCore implementation), `FFmpegLocator`, `ClipperCommandLineInterpreter`, `TrailerClipperOptions` (property order is the config file's order), `help.txt`.
- `src/TrailerClipper.Tool/`: `Program.cs` (1.1.0's console app plus exit codes and the install offer), `FFmpegInstaller.cs`.
- `tests/TrailerClipperLib.Tests/`: `GoldenTests.cs` (replay and the exception table), `GoldenRunner.cs` (generated), `UnitTests.cs` (a fake engine, no ffmpeg needed).
- Durations are read as 1.1.0 read them, truncated to hundredths of a second (MediaToolkit parsed ffmpeg's `Duration:` line).
- The capture project under `tests/Golden/Capture/` inherits `Directory.Build.props` if an IDE restores it; its lock file is gitignored. Run it only in a scratch folder.
- The publish job has no checkout, so it pins `dotnet-version` instead of reading `global.json`.
- `.editorconfig` sections use paths relative to the repository root (`[tests/**.cs]`); a pattern like `[*Tests/**.cs]` matches nothing here.
