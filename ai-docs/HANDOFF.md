# Handoff

<!-- Keep under 50 lines. Replace, never append. Written at the end of a work session so the next one starts without re-deriving state. -->

## Current state
- TrailerClipper 2.0.0-beta.1 (library) and TrailerClipper.Tool 2.0.0-beta.1 (tclipper) are on branch v2, pull request #1, CI green on Linux, macOS and Windows (run 36335032052 at 7639fad). 193 tests per framework (net10.0, net48): golden replay of the 1.1.0 capture with exceptions E1-E11, unit tests with a fake engine, locator, review and public API tests.
- Phases 0-3 done 2026-09-27; plan ruled (see the plan's "Rulings"); the Phase 3 review's 12 findings are fixed (ai-docs/notes/2026-09-27-phase-3-review-findings.md) and summarised on the PR.
- Already applied before the merge: ruleset "master" (24078385, required check ci), tag ruleset "Tags only by admins" (24078386), secret scanning and push protection, private vulnerability reporting, workflow permissions read, description, homepage, topics, delete-branch-on-merge; environment nuget (reviewer m4bwav, tags v*, secret NUGET_USER). Mark's nuget.org Trusted Publishing policy covers TrailerClipper* from release.yml with environment nuget.

## In progress
- Waiting for Mark's review and merge of PR #1 (Phase 4). No cleanup list: no issues, bot pull requests, webhooks or stale branches exist; after the merge only the v2 branch goes (delete-branch-on-merge is on).

## Decisions made this session
- FFMpegCore runs ffmpeg (Mark); ffmpeg is found without configuration and installed by tclipper through winget, brew, apt or dnf; the library never downloads. See ai-docs/decisions/2026-09-27-v2-promise-system-ffmpeg-named-exceptions.md.
- NUnit 4.6.1, not 5.0.0 (published 2026-09-27, inside the cooldown). CI installs ffmpeg 9.0 from BtbN/FFmpeg-Builds on Linux (apt has 6.1).

## Dead ends hit
- The capture template's net10.0 cannot load 1.1.0: capture on net48.
- Write, Edit and heredocs decode backslash-u escapes: write such text with python -c and chr(92).

## Next single action
After Mark merges PR #1: read the merge SHA (gh pr view 1 --json mergeCommit,mergedAt), wait for ci green on master, then tag v2.0.0-beta.1 on it and push the tag; release.yml stops at the nuget environment for Mark's approval (Review deployments), then run verify-published with 2.0.0-beta.1.
