# Log

Append-only. One line per operation: `## [YYYY-MM-DD] op | title` where op is one of add, update, supersede, prune, handoff, index. Newest at the bottom. Never edited, only appended; this is the history the entries themselves do not carry.

## [2026-09-27] init | scaffolded

## [2026-09-27] add | Phase 0: survey, baseline, golden capture
- survey-nuget.sh output: ai-docs/notes/survey-2026-09-27.md; reading: ai-docs/notes/2026-09-27-phase-0-survey-baseline-and-capture.md. 10 versions, 16 908 downloads, no dependents, no issues, pull requests, alerts, webhooks, secrets or workflows. 1.1.0 bundles MediaToolkit.dll (10.8 MB) and System.Net.Http.dll in a TFM-less lib/ with no declared dependency.
- Baseline: `dotnet build TrailerClipperLib.sln` on SDK 10.0.401 fails with MSB3644 (net40, net452 reference packs); no old tests exist.
- Golden capture: net48 scratch project referencing TrailerClipper [1.1.0], fixtures a.wav, b.mp3, notes.txt made with ffmpeg 9.0.1, durations by ffprobe 9.0.1: 148 cases, two runs identical apart from the date line (diff). Committed under tests/Golden/.
- check-readme-images.mjs README.md --registry nuget: 0 images, exit 0. History grep for credentials: none.
- everlast init and project register (mode repo). AGENTS.md, CLAUDE.md (@AGENTS.md import), .github/copilot-instructions.md.

## [2026-09-27] add | Phase 1: plan and proposed decision
- ai-docs/plans/2026-09-27-modernization-and-v2-release.md (D1-D16, E1-E10); ai-docs/decisions/2026-09-27-v2-promise-system-ffmpeg-named-exceptions.md (proposed). Stop for Mark's plan review.

## [2026-09-27] update | Plan ruled; Phase 2 rewrite on branch v2
- Rulings in the plan's "Rulings" section: FFMpegCore for D5, automated ffmpeg install through the OS package manager, D8 and D14 accepted, Trusted Publishing policy added by Mark.
- NUnit 5.0.0 was published on 2026-09-27, inside the three-day cooldown: tests use NUnit 4.6.1.
- Golden replay: GoldenRunner.cs generated from the capture program; first build 138/150 (E9 MP3 lengths, a test-side key order), then 150/150 on net10.0 and net48. Canary: "Starting on file" changed to "Starting file" in TrailerClipperManager.cs, 16 cases red; reverted with git checkout -- src/, 150 green. `git diff --exit-code 8e59145 -- tests/Golden/1.1.0.json tests/Golden/Capture tests/Golden/fixtures`: empty.
- An IDE restore wrote tests/Golden/Capture/packages.lock.json and it was committed in f0f9c94; untracked and gitignored in the next commit.
- help.txt keeps 1.1.0's BOM and CRLF (-text); it was stored LF since 2015, so Linux checkouts would have differed from the recording.
- 172 tests per framework (golden, unit with a fake engine, locator); dotnet format clean; pack: TrailerClipper (lib/netstandard2.0, lib/net10.0, README) and TrailerClipper.Tool; the tool installed from artifacts clipped the fixtures, and an intro-only call works.
- actionlint 1.7.12, check-workflow-shell.py and zizmor 1.30.1 --offline: clean.
## [2026-09-27] index | rebuilt (4 entries)
## [2026-09-27] index | rebuilt (5 entries)

## [2026-09-27] add | CI round 1 and Phase 3 review
- PR #1 opened. CI run 36334471273: Windows green; macOS failed on file order (APFS lists b.mp3 before a.wav) and the /private temp prefix; Ubuntu failed because apt's ffmpeg 6.1 ffprobe measures the untouched MP3 fixture as 10 031 ms. Fixes in 127f399: folders clipped in name order on every OS, the golden test uses the resolved temp path, CI installs the current ffmpeg 9.0 build from BtbN/FFmpeg-Builds (latest release, sha256 checked).
- nuget environment created with gh: required reviewer m4bwav, deployment rule tag v*, secret NUGET_USER.
- Phase 3 review (read-only subagent, 18 tool calls, 4 minutes): 12 findings, all fixed or named (E11); see notes/2026-09-27-phase-3-review-findings.md. PublicApi-1.1.0.txt listed from the published DLL by reflection (PowerShell: Assembly.LoadFile, GetExportedTypes, public declared methods, constructors and properties with parameter names). 193 tests per framework, green on net10.0 and net48. Committed as d033b12.
- The Write and Edit tools decode backslash-u escapes (backslash, u, four hex digits) in their input: a C# escape for "<" became a literal "<" three times, which silently disabled the config escaping. Write such text with Python and chr(92), then grep the file.

## [2026-09-27] add | Phases 4 to 7: merge, beta, release, wrap-up
- PR #1 merged by Mark as ab3a984 (merge commit, 17:03). ci on master 36335507076 green. Tag v2.0.0-beta.1 (admin bypass of the tag ruleset); release 36335667408: build, attest, Mark approved the nuget environment, both packages pushed, GitHub Release v2.0.0-beta.1.
- verify-published 36336494800: Windows green; Linux and macOS failed at dotnet tool install ("version not found") because the registration index committed the tool at 17:23:35, after the flat container listed it. Rerun of the failed jobs: green. PR #4 (b4c6b9d) waits for both indexes.
- PR #3 (versions 2.0.0, changelog dated; the full notes moved under 2.0.0) merged by Mark as 4a41b49; ci 36336712092 green; tag v2.0.0; release 36336887025 approved by Mark and green, GitHub Release v2.0.0. Dependabot #2 (SDK 10.0.401) merged as d57ca52 after the release.
- verify-published 36338281647 for 2.0.0 (with the #4 wait): green on Linux, macOS and Windows. Skill: C-20260927-9 nuget-coverage-complete, L-062 verify-waits-both-indexes (package-modernize 49d227b).
## [2026-09-27] index | rebuilt (5 entries)
