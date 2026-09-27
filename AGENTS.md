# AGENTS.md

Rules for any AI agent (Claude Code, Copilot, Cursor, Codex) working in this repository. `CLAUDE.md` and `.github/copilot-instructions.md` only point here.

## What this is

The NuGet package `TrailerClipper` (namespace `TrailerClipperLib`): batch removal of intros and trailers from video and audio files through ffmpeg, plus the console app `TClipper`. 1.1.0 (2016-09-29, net40, MediaToolkit bundled) is the published version until 2.0.0 ships. The plan is `ai-docs/plans/2026-09-27-modernization-and-v2-release.md`; start with `ai-docs/HANDOFF.md`.

## Rules

- **The promise.** 2.0.0 gives 1.1.0's answers for every case in `tests/Golden/1.1.0.json` except the exceptions E1 to E10 named in the plan. A fix that would change an old answer needs a decision entry and a changelog line.
- **The golden files never change.** `tests/Golden/1.1.0.json`, `tests/Golden/Capture/` and `tests/Golden/fixtures/` were captured from the published 1.1.0 on 2026-09-27. When the golden test fails, fix the code or name an exception the maintainer ruled on; never regenerate the recording from this repository's code.
- **Nothing reaches nuget.org without the maintainer.** No API key is stored anywhere; publishing goes through Trusted Publishing from `release.yml`, gated by the `nuget` environment. Never push a package from a machine.
- **Research beats recall.** SDK, package and action versions change; re-verify any version older than three months.
- **Document for handoff.** Anything learned, decided or built goes into `ai-docs/` before you finish; rewrite `ai-docs/HANDOFF.md` when work is left unfinished.
- **No AI attribution anywhere.**
- **Line endings.** New files are LF; check with node (count byte 13) on Windows.

## Layout (until the v2 rewrite lands)

- `TrailerClipperLib/` the net40 library, `TClipper/` the console app, `TrailerClipperLib.sln` (VS 2015; does not build on SDK 10 without the .NET Framework 4.0 and 4.5.2 reference packs).
- `tests/Golden/` the behaviour record of 1.1.0: run `Capture/` only in a scratch folder outside the repository (`dotnet run -c Release -- <fixtures dir>`); it needs ffprobe on PATH.
- `ai-docs/` everlast doc set (INDEX, HANDOFF, log, decisions, plans, notes).
