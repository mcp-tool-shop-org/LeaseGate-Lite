# LeaseGate-Lite Usability Checklist (v0.1.0 Gate)

This checklist is a **pass/fail release gate** for `v0.1.0`.
Do not tag the release unless every item is marked PASS.

## 1) First-run under 30 seconds
- [ ] PASS / [ ] FAIL — Fresh install opens to first-run banner immediately.
- [ ] PASS / [ ] FAIL — User can pick Quiet/Balanced/Performance in one click.
- [ ] PASS / [ ] FAIL — Optional toggles (`Keep UI responsive`, `Start on login`) are visible and understandable.
- [ ] PASS / [ ] FAIL — `Complete setup` applies a safe configuration in <30 seconds.
- [ ] PASS / [ ] FAIL — Banner collapses into `Setup complete` chip and does not reappear on restart.

## 2) Controls are self-explanatory
- [ ] PASS / [ ] FAIL — Every control has a one-line explanation in UI.
- [ ] PASS / [ ] FAIL — Every major control has an impact hint/example.
- [ ] PASS / [ ] FAIL — Impact preview updates live before clicking Apply.
- [ ] PASS / [ ] FAIL — Recommended ranges are visually indicated where tuning is most critical.

## 3) Human-readable behavior
- [ ] PASS / [ ] FAIL — Clamp/queue reasons are shown in plain language (not enum-only text).
- [ ] PASS / [ ] FAIL — `What changed?` mini-feed shows last 3 decisions with timestamps.
- [ ] PASS / [ ] FAIL — Each mini-feed item has one-click navigation to related controls.

## 4) Safety and recovery UX
- [ ] PASS / [ ] FAIL — Every error message includes a recovery suggestion.
- [ ] PASS / [ ] FAIL — Destructive action (`Exit daemon`) requires confirmation.
- [ ] PASS / [ ] FAIL — Disabled states explain *why* (e.g., daemon not running).
- [ ] PASS / [ ] FAIL — If daemon restarts/unreachable, UI reconnects and stays responsive.

## 5) Notification quality
- [ ] PASS / [ ] FAIL — Tray notifications are opt-in by default.
- [ ] PASS / [ ] FAIL — Notifications are rate-limited (no spam under load).
- [ ] PASS / [ ] FAIL — Notification language is calm and actionable.

## 6) Demo scenarios
Run each script in `scripts/` and record outcome:
- [ ] PASS / [ ] FAIL — `demo-laptop-mode.ps1`
- [ ] PASS / [ ] FAIL — `demo-high-load-recovery.ps1`
- [ ] PASS / [ ] FAIL — `demo-app-profile-override.ps1`

## 7) Release capture assets
- [ ] PASS / [ ] FAIL — Screenshots captured per `RELEASE_CAPTURE_GUIDE.md`
- [ ] PASS / [ ] FAIL — Changelog includes usability highlights and known limitations.

---

## Sign-off
- Tester:
- Date:
- Build/commit:
- Decision: [ ] RELEASE BLOCKED  [ ] RELEASE READY
