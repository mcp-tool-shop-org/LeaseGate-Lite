# LeaseGate-Lite

LeaseGate-Lite is a one-tab MAUI control surface plus a lightweight local daemon.

It keeps LeaseGate’s operational feel (explicit control, bounded execution, deterministic reasons, observable status) but trims to home-PC scope.

## Projects

- `src/LeaseGateLite.Contracts` — shared DTOs and enums
- `src/LeaseGateLite.Daemon` — minimal local API daemon (`http://localhost:5177`)
- `src/LeaseGateLite.App` — one-tab MAUI app (`Control`)
- `LeaseGateLite.slnx` — solution

## Run

1) Start daemon:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Start MAUI app (Windows example):

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

## One-click packaging and install (Windows)

Create release artifact (portable zip + SHA256 checksum):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Install locally from packaged artifact:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

Post-install behavior:
- Daemon runs immediately and can be configured to start on login.
- Control panel launches and connects automatically.
- Balanced is the default preset; laptop-like hardware gets a Quiet recommendation in first-run setup (never forced).

## Daemon endpoints (minimal)

- `GET /status` — live `StatusSnapshot`
- `GET /config` — current config
- `POST /config` — apply config
- `POST /config/reset` — reset defaults
- `POST /service/start`
- `POST /service/stop`
- `POST /service/restart`
- `POST /diagnostics/export` — exports JSON bundle and returns path/bytes
- `GET /events/tail?n=200` — event tail

## One-tab layout

Single tab/page: `Control`

- Header strip: status dot, mode picker, endpoint, quick actions (`Start`, `Stop`, `Apply`, `Export Diag`)
- Left column: auditable checklist (jump to card)
- Right column: ordered control cards matching checklist sections

Each card includes:

- current value
- short meaning
- controls
- effect preview
- footer: `✅ Covered by: ...`

## Audit checklist (phase 1)

A) Service control
- Connect/reconnect
- Start/Stop/Restart
- Version + uptime
- Open config file location
- Reset defaults

B) Live status
- Calm/Warm/Spicy
- Active calls
- Queue depth
- Effective concurrency
- CPU% / Available RAM%
- Last throttle reason

C) Core throttling controls
- Max concurrency
- Interactive reserve
- Background cap
- Cool-down behavior

D) Adaptive throttle tuning
- Soft threshold
- Hard threshold
- Recovery rate
- Smoothing

E) Request shaping
- Max output clamp
- Max prompt/context clamp
- Overflow behavior
- Retry policy

F) Rate limiting
- Requests/min
- Tokens/min
- Burst allowance

G) Presets
- Quiet (Laptop)
- Balanced
- Performance (Desktop)
- Custom preset save/load

H) Diagnostics + audit-lite
- Export diagnostics
- Event tail (read-only)
- Copy status summary

## Notes

- Lite intentionally excludes heavy governance features (approvals/signing/receipts).
- The daemon currently simulates pressure and queue dynamics to validate UI behavior end-to-end.
