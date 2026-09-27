# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- Modernizing TrailerClipper (NuGet) to 2.0.0 with the package-modernize skill (first NuGet run of the skill). Branch `v2`.
- Phase 0 done 2026-09-27: survey (ai-docs/notes/2026-09-27-phase-0-survey-baseline-and-capture.md), baseline cannot build on SDK 10, golden capture of the published 1.1.0 in tests/Golden (148 cases, deterministic).
- Phase 1 done: ai-docs/plans/2026-09-27-modernization-and-v2-release.md, decisions D1-D16, exceptions E1-E10.

## In progress
- Waiting at the plan review stop for Mark's rulings.

## Decisions made this session
- Proposed only (ai-docs/decisions/2026-09-27-v2-promise-system-ffmpeg-named-exceptions.md): system ffmpeg through processes instead of MediaToolkit, netstandard2.0 plus net10.0, the console app as the .NET tool TrailerClipper.Tool, 2.0.0-beta.1 rehearsal.

## Dead ends hit
- The capture template's net10.0 cannot load 1.1.0 (net40 plus System.Web.Extensions): capture on net48.
- Python heredocs through Git Bash turned `\r\n` inside a C# string into real line breaks; use the editor tool for backslashes.

## Next single action
Get Mark's rulings on D5, D8, D13 and D14 (silence means the recommendations stand), then Phase 2 on branch v2: remove the D12 files and write the golden test first.
