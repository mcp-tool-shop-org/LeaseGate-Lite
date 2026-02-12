# Release Capture Guide (v0.1.0)

Use this guide to produce consistent screenshots and short clips for release notes.

## Recording setup
- Resolution: 1920x1080 preferred.
- Window size: large enough to show checklist + right control cards.
- Theme: capture one Light and one Dark pass if possible.
- Ensure daemon is running before capture.

## Required screenshots
1. **First-run setup banner**
   - Show goal choices and optional toggles.
2. **Setup complete state**
   - Show collapsed chip and normal control panel.
3. **Live status calm state**
   - Show large Active/Queued/Effective numbers with Calm heat badge.
4. **What changed mini-feed**
   - Show human-readable explanations and `Open` links.
5. **Profiles card**
   - Show recent app selection and per-app override controls.
6. **Diagnostics card**
   - Show technical details collapsed by default, then expanded.
7. **Sticky pending actions**
   - Show `Pending changes` pill with always-reachable Apply/Revert bar.

## Required short clips (10–20s each)
1. **Laptop mode setup**
   - Run first-run flow with Quiet + responsive toggle.
2. **High-load recovery**
   - Trigger Spicy pressure; show clamp explanations and recovery.
3. **Per-app override**
   - Apply app-specific preset/override and verify status/events update.

## Naming convention
- Screenshots: `v0.1.0-01-first-run.png`, `v0.1.0-02-setup-complete.png`, ...
- Clips: `v0.1.0-demo-01-laptop-mode.mp4`, ...

## Release note snippets
For each asset, include:
- What user problem it solves
- What changed from previous phase
- Any caveat (if applicable)
