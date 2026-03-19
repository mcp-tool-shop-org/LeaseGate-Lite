---
title: Daemon API
description: All REST endpoints on localhost:5177.
sidebar:
  order: 2
---

The LeaseGate-Lite daemon exposes a local REST API on `localhost:5177`. No external network access — all communication stays on the machine.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/status` | Live `StatusSnapshot` — CPU%, RAM%, queue depth, heat state |
| `GET` | `/config` | Current configuration |
| `GET` | `/config/defaults` | Default config values |
| `POST` | `/config` | Apply new configuration |
| `POST` | `/config/reset` | Reset to defaults |
| `POST` | `/service/start` | Start the daemon |
| `POST` | `/service/stop` | Stop the daemon |
| `POST` | `/service/restart` | Restart the daemon |
| `POST` | `/service/pause-background` | Pause/resume background work |
| `POST` | `/service/exit` | Graceful daemon shutdown |
| `GET` | `/autostart/status` | Autostart toggle status |
| `POST` | `/autostart` | Enable/disable autostart |
| `GET` | `/notifications` | Notification settings |
| `POST` | `/notifications` | Enable/disable notifications |
| `GET` | `/presets` | List all presets |
| `POST` | `/preset/preview` | Preview preset diff against current config |
| `POST` | `/preset/apply` | Apply a preset (Quiet/Balanced/Performance) |
| `GET` | `/profiles` | Per-app profile overrides |
| `POST` | `/profiles/apply` | Set per-app profile override |
| `POST` | `/diagnostics/export` | Export JSON diagnostic bundle |
| `GET` | `/diagnostics/preview` | Preview diagnostic export contents |
| `GET` | `/events/tail?n=200` | Event tail (last N events) |
| `GET` | `/events/stream` | Poll for new events since a given ID |
| `POST` | `/simulate/pressure` | Set pressure mode (requires `--enable-simulation`) |
| `POST` | `/simulate/flood` | Flood simulation (requires `--enable-simulation`) |

## Status snapshot

The `GET /status` endpoint returns a `StatusSnapshot` object with:

- **CPU%** — current processor utilization
- **RAM%** — available memory percentage
- **Queue depth** — number of pending AI calls (interactive + background)
- **Heat state** — one of `Calm`, `Warm`, or `Spicy`
- **Effective concurrency** — current throttled concurrency limit
- **Throttle reason** — why throttling is active (CPU pressure, memory pressure, cooldown, rate limit, manual clamp)

The daemon reads real Windows system metrics using `PerformanceCounter` (CPU) and `GlobalMemoryStatusEx` (RAM), then simulates queue pressure dynamics for the throttling engine.

## Configuration lifecycle

1. `GET /config` — read current settings
2. Modify values (concurrency, thresholds, rate limits, etc.)
3. `POST /config` — apply changes (takes effect immediately)
4. `POST /config/reset` — restore factory defaults at any time

## Security

The daemon listens on `localhost:5177` with no authentication by default. For environments where local process isolation matters, start the daemon with `--require-auth` to enable token-based authentication. The token is auto-generated at first run and stored in `%LOCALAPPDATA%\LeaseGateLite\daemon.token`. Clients must pass it via the `X-Auth-Token` header.

Simulation endpoints (`/simulate/pressure` and `/simulate/flood`) are disabled by default. Enable them with `--enable-simulation` or by running in Development mode.

## Diagnostics

`POST /diagnostics/export` generates a JSON diagnostic bundle containing the current configuration, recent events, system metrics history, and throttling state. Useful for troubleshooting thermal issues or tuning thresholds.

The export respects two toggles:
- **includePaths** — when false (default), local file paths are redacted to `[PATH]`
- **includeVerbose** — when true, exports up to 1000 events and status samples instead of the default 250/120

Event logs are rotated at 5 MB on disk with one generation kept.
