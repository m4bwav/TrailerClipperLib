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
