# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- TrailerClipper 2.0.0 and TrailerClipper.Tool 2.0.0 released on nuget.org 2026-09-27 (release run 36336887025, tag v2.0.0 on 4a41b49, approved by Mark), GitHub Release v2.0.0 with both packages, verified from nuget.org by verify-published 36338281647 on Linux, macOS and Windows. 2.0.0-beta.1 went first (run 36335667408) and was verified from nuget.org on Linux, macOS and Windows (verify-published 36336494800 after a rerun).
- Master: PR #1 (the rewrite, ab3a984), #3 (release 2.0.0), #4 (verify waits for the registration index), #2 (Dependabot SDK 10.0.401). Rulesets 24078385 (master) and 24078386 (tags), scanning, private reporting, workflow permissions read, environment nuget.

## Standing work
- Mark: deprecate 1.x on nuget.org (UI only, package TrailerClipper, versions 1.0.0 to 1.1.0): reason Legacy, alternate package TrailerClipper 2.0.0, message "1.x bundles a 2015 ffmpeg and runs only on Windows .NET Framework; use 2.x".
- Merge Dependabot pull requests when ci is green (weekly; three-day cooldown). NUnit 5 can come in through them after it has aged.
- Next major: when .NET 10 leaves support (2028-11-14) or netstandard2.0 stops being useful; set EnablePackageValidation with PackageValidationBaselineVersion 2.0.0 in the library csproj at the next minor.
- If CI's ffmpeg changes major (BtbN latest n9.0), re-check the golden MP3 answers (E9).

## Decisions made this session
- See ai-docs/decisions/2026-09-27-v2-promise-system-ffmpeg-named-exceptions.md and the plan's "Rulings".

## Dead ends hit
- The capture template's net10.0 cannot load 1.1.0: capture on net48.
- Write, Edit and heredocs decode backslash-u escapes: write such text with python -c and chr(92).
- verify-published once checked only the flat container; dotnet tool install reads the registration index, which lagged. Fixed in #4.

## Next single action
Nothing for this package until Dependabot or the deprecation; the next package in the inventory is IsImageUrlDotNet (F#).
