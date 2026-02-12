# Release Gate — LeaseGate-Lite v0.1.0

This gate must pass before tagging `v0.1.0`.

## 1) Functional gate (must pass)
- [ ] Endpoints reachable
- [ ] Presets apply (`Quiet`, `Balanced`, `Performance`)
- [ ] Autostart toggles (`enabled`/`disabled` roundtrip)
- [ ] Diagnostics export succeeds
- [ ] Event tailing/stream endpoint returns valid payload

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-release-gate.ps1
```

Expected artifact:
- `RELEASE_GATE_REPORT.md`

## 2) Usability gate (must pass)
Use [USABILITY_CHECKLIST.md](USABILITY_CHECKLIST.md) and mark all items PASS.

Minimum required:
- first-run under 30 seconds
- controls self-explanatory
- plain-language throttle explanations
- no destructive action without confirmation
- no spammy notifications

## 3) Audit harness gate (must pass)
In app:
- Open `Audit Harness` card
- Run `Run Audit Harness`
- Ensure all rows are ✅

## 4) Packaging gate (must pass)
- [ ] `scripts/package-v0.1.0.ps1` creates zip + `.sha256`
- [ ] `scripts/install-local.ps1` installs and launches app/daemon/tray
- [ ] Balanced defaults active on first run

## 5) Story gate (must pass)
Release note must clearly state:
- what it does
- what it does not do
- recommended settings (laptop/desktop)
- quality-of-life value: smoother AI calls, less stutter, fewer thermal spikes

---

If any gate item fails, do not tag release.
