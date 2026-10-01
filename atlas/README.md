# LeaseGate-Lite: how it works

Mapped at 2026-10-01 from commit 2461665 by Atlas 1.24.0.

## What this is

9 parts, mostly C# (28 files), PowerShell (8), CSS (2), TypeScript (2), Astro (1) and JavaScript (1). Work enters through 2 doors; CI and Deploy site to GitHub Pages each reach 1 part, and CI is followed because a pull request goes through it. It deploys a site to GitHub Pages.

## What changed since the last map

This is the first map.

## What comes in

1. **CI.** On a pull request to main; on a push to main touching 8 paths; or by hand. Runs tests/LeaseGateLite.Tests/LeaseGateLite.Tests.csproj.
2. **Deploy site to GitHub Pages.** On a push to main touching 2 paths; or by hand. Runs site/astro.config.mjs and site/src/.

## What happens through CI

1. The workflow runs tests/LeaseGateLite.Tests/LeaseGateLite.Tests.csproj in tests.

## Who reads the results

CI writes nothing this map can see.

## The other doors

**Deploy site to GitHub Pages** runs site/astro.config.mjs and site/src/, and deploys the site.

## What breaks what

No part is imported by another part, and no part sits on the path of two doors.

LeaseGateLite.App, LeaseGateLite.Contracts, LeaseGateLite.Daemon, LeaseGateLite.Tray and scripts hold only C# and PowerShell files, which this map does not read, so what uses them cannot be seen.

## What tends to change together

No two source files changed together often enough to name.

Window: 180 days; a pair counts from 3 shared commits, since the window holds fewer than 30 qualifying commits.

## What no test touches

Every code part this map reads is imported by at least one test.

LeaseGateLite.App, LeaseGateLite.Contracts, LeaseGateLite.Daemon, LeaseGateLite.Tray and scripts hold only C# and PowerShell files, which this map does not read, so whether a test touches them cannot be seen.

## Written but never read

No place this map can see is written, so none goes unread.

## Helpers that look duplicated

No two parts export a helper that looks alike.

## Generated, never hand-edited

Nothing in this repository writes to a tracked place this map can see.

## Hand-authored

People write .github/, the repository root and site/. Nothing in this repository writes to them.

## Where to start

CI runs no code this map can follow, so there is no path of files to read in order.

## What this map cannot see

- Statistics confidence is low: fewer than 30 qualifying commits in the window, and fewer than 20 source files reach 10 revisions.

Regenerate with `npx --yes @dogfood-lab/atlas map`.
