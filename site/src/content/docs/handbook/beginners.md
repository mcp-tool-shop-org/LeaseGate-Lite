---
title: Beginners Guide
description: First-time setup walkthrough for new users.
sidebar:
  order: 99
---

New to LeaseGate-Lite? This guide walks you through everything from installation to your first configuration change.

## What is LeaseGate-Lite?

LeaseGate-Lite is a local daemon and control panel that throttles AI workloads on your Windows PC. If you run AI tools (coding assistants, chat clients, batch processing) and notice your machine stuttering, fans spinning up, or calls timing out during heavy use, LeaseGate-Lite sits between your apps and the system to keep things smooth.

It works by monitoring your CPU and RAM in real time, then automatically adjusting how many AI calls can run simultaneously. When your system gets hot, it backs off. When pressure drops, it ramps back up. You stay in control through a one-tab desktop panel with explicit settings and observable status.

## Prerequisites

Before you start, make sure you have:

1. **Windows 10 or 11** — The daemon uses Windows-specific APIs for system metrics
2. **.NET 10 SDK** — Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download). Verify with `dotnet --version`
3. **A terminal** — PowerShell, Windows Terminal, or Command Prompt all work

## Installation

Clone the repository and build from source:

```powershell
git clone https://github.com/mcp-tool-shop-org/LeaseGate-Lite.git
cd LeaseGate-Lite
dotnet build
```

Alternatively, use the one-click packaging script to create a portable install:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1 -Version 1.0.1
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

This creates a portable zip, installs it, and optionally registers the daemon to start on login.

## First run

Start the daemon first, then the control panel:

```powershell
# Terminal 1: Start the daemon
dotnet run --project src/LeaseGateLite.Daemon

# Terminal 2: Start the control panel
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

The daemon starts on `localhost:5177` and immediately begins reading real system metrics. The control panel connects to the daemon automatically and shows live status.

On first run, the daemon creates its runtime directory at `%LOCALAPPDATA%\LeaseGateLite\` with a default configuration file. If your hardware looks like a laptop, the control panel recommends the Quiet preset during setup -- but it never forces the choice.

## Key concepts

Understanding these concepts helps you get the most from LeaseGate-Lite:

**Heat states** track your system's thermal pressure:
- **Calm** — Everything is fine. Full concurrency available.
- **Warm** — CPU or RAM crossed the soft threshold (default 70%). Concurrency is being gradually reduced.
- **Spicy** — Hard threshold crossed (default 90%). Aggressive throttling is active to prevent thermal events.

**Presets** are pre-configured profiles tuned for different hardware:
- **Quiet** — Conservative limits for laptops (max 4 concurrent calls, aggressive cooldown)
- **Balanced** — Sensible defaults for typical desktops (max 8 concurrent calls)
- **Performance** — Higher throughput for desktops with headroom (max 14 concurrent calls)

**Throttle reasons** tell you why the daemon is limiting concurrency:
- `CpuPressure` — CPU utilization is high
- `MemoryPressure` — Available RAM is low
- `Cooldown` — Waiting between calls to let the system breathe
- `RateLimit` — Requests or tokens per minute exceeded
- `ManualClamp` — You manually reduced concurrency

**Overflow behaviors** control what happens when the request queue is full:
- `TrimOldest` — Drop the oldest queued request to make room (default)
- `Deny` — Reject new requests immediately
- `QueueOnly` — Queue without trimming (may grow until `MaxQueuedItems` is hit)

## Common tasks

### Check daemon status from the command line

```powershell
curl http://localhost:5177/status
```

This returns a JSON snapshot with CPU%, RAM%, heat state, queue depth, active calls, and effective concurrency.

### Switch to a different preset

```powershell
# Preview what will change
curl -X POST http://localhost:5177/preset/preview -H "Content-Type: application/json" -d "{\"name\":\"Quiet\"}"

# Apply the preset
curl -X POST http://localhost:5177/preset/apply -H "Content-Type: application/json" -d "{\"name\":\"Quiet\"}"
```

### Enable authentication

If you want to prevent other local processes from controlling the daemon:

```powershell
dotnet run --project src/LeaseGateLite.Daemon -- --require-auth
```

The daemon generates a token file at `%LOCALAPPDATA%\LeaseGateLite\daemon.token`. All requests must include the `X-Auth-Token` header with this token.

### Export diagnostics

```powershell
curl -X POST "http://localhost:5177/diagnostics/export?includePaths=false&includeVerbose=false"
```

This generates a JSON diagnostic bundle with configuration, recent events, system metrics history, and throttling state. Set `includeVerbose=true` for a larger export (up to 1000 events and status samples).

## Troubleshooting

**Daemon won't start — "daemon already running"**
Only one instance can run at a time. Check if another instance is running, or kill it and try again. The daemon uses a named mutex to enforce this.

**Control panel shows "Disconnected"**
Make sure the daemon is running on `localhost:5177`. Check your terminal for daemon error output. If you changed the port or the daemon crashed, restart it.

**System still stuttering despite throttling**
Check the heat state -- if it shows Calm, the daemon isn't detecting pressure. Lower the `SoftThresholdPercent` (e.g., from 70 to 60) to make the daemon react earlier. Also check that `MaxConcurrency` isn't set too high for your hardware.

**"Unknown flag" error on startup**
The daemon only accepts these flags: `--run`, `--install-autostart`, `--uninstall-autostart`, `--status`, `--enable-simulation`, `--require-auth`. Check for typos.

**Configuration changes not taking effect**
Changes via `POST /config` take effect immediately. If using the control panel, make sure you clicked Apply. Check `GET /config` to verify the current state.

**Want to start fresh**
Reset to factory defaults:
```powershell
curl -X POST "http://localhost:5177/config/reset?apply=true"
```
