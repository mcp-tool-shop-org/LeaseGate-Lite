<p align="center">
  <a href="README.ja.md">日本語</a> | <a href="README.zh.md">中文</a> | <a href="README.es.md">Español</a> | <a href="README.fr.md">Français</a> | <a href="README.hi.md">हिन्दी</a> | <a href="README.it.md">Italiano</a> | <a href="README.pt-BR.md">Português (BR)</a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/mcp-tool-shop-org/brand/main/logos/LeaseGate-Lite/readme.jpg" width="500">
</p>

<p align="center">
  <a href="https://github.com/mcp-tool-shop-org/LeaseGate-Lite/actions/workflows/ci.yml"><img src="https://github.com/mcp-tool-shop-org/LeaseGate-Lite/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10.0-512BD4" alt=".NET 10"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/mcp-tool-shop-org/LeaseGate-Lite" alt="License: MIT"></a>
  <a href="https://mcp-tool-shop-org.github.io/LeaseGate-Lite/"><img src="https://img.shields.io/badge/Landing_Page-live-blue" alt="Landing Page"></a>
</p>

A one-tab MAUI control surface and local daemon for throttling AI workloads on Windows — smoother calls, less stutter, fewer thermal spikes.

Keeps LeaseGate's operational feel (explicit control, bounded execution, deterministic reasons, observable status) but trims to home-PC scope.

## Projects

| Project | Description |
|---------|-------------|
| `src/LeaseGateLite.Contracts` | Shared DTOs and enums (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Local API daemon on `localhost:5177` with real Windows system metrics |
| `src/LeaseGateLite.App` | One-tab MAUI control panel (Windows/Android/iOS/macCatalyst) |
| `src/LeaseGateLite.Tray` | Windows system tray companion |
| `tests/LeaseGateLite.Tests` | 178 xUnit tests (config validation, simulation, diagnostics) |

## Run

1) Start daemon:

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

2) Start MAUI app (Windows):

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

## Daemon endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/status` | Live `StatusSnapshot` (CPU%, RAM%, queue depth, heat state) |
| `GET` | `/config` | Current config |
| `POST` | `/config` | Apply config |
| `POST` | `/config/reset` | Reset defaults |
| `POST` | `/service/start` | Start daemon |
| `POST` | `/service/stop` | Stop daemon |
| `POST` | `/service/restart` | Restart daemon |
| `POST` | `/diagnostics/export` | Export JSON diagnostic bundle |
| `GET` | `/events/tail?n=200` | Event tail |

## One-tab layout

Single tab/page: **Control**

- **Header strip**: status dot, mode picker, endpoint, quick actions (Start, Stop, Apply, Export Diag)
- **Left column**: auditable checklist (jump to card)
- **Right column**: ordered control cards matching checklist sections

Each card includes: current value, short meaning, controls, effect preview, and a coverage footer.

## Audit checklist

| Section | Controls |
|---------|----------|
| **A) Service** | Connect, Start/Stop/Restart, version + uptime, config location, reset |
| **B) Live status** | Heat state (Calm/Warm/Spicy), active calls, queue depth, CPU%, RAM% |
| **C) Core throttling** | Max concurrency, interactive reserve, background cap, cooldown |
| **D) Adaptive tuning** | Soft/hard thresholds, recovery rate, smoothing |
| **E) Request shaping** | Max output/prompt clamp, overflow behavior, retry policy |
| **F) Rate limiting** | Requests/min, tokens/min, burst allowance |
| **G) Presets** | Quiet (laptop), Balanced, Performance (desktop) |
| **H) Diagnostics** | Export diagnostics, event tail, copy status summary |

## Notes

- Lite intentionally excludes heavy governance features (approvals, signing, receipts).
- The daemon reads real Windows system metrics (CPU via PerformanceCounter, RAM via GlobalMemoryStatusEx) and simulates queue pressure dynamics.
- Tests use a `FakeSystemMetrics` provider via dependency injection for deterministic, hardware-independent verification.

---

<p align="center">
  Built by <a href="https://mcp-tool-shop.github.io/">MCP Tool Shop</a>
</p>
