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
| `POST` | `/config` | Apply new configuration |
| `POST` | `/config/reset` | Reset to defaults |
| `POST` | `/service/start` | Start the daemon |
| `POST` | `/service/stop` | Stop the daemon |
| `POST` | `/service/restart` | Restart the daemon |
| `POST` | `/diagnostics/export` | Export JSON diagnostic bundle |
| `GET` | `/events/tail?n=200` | Event tail (last N events) |

## Status snapshot

The `GET /status` endpoint returns a `StatusSnapshot` object with:

- **CPU%** — current processor utilization
- **RAM%** — current memory usage
- **Queue depth** — number of pending AI calls
- **Heat state** — one of `Calm`, `Warm`, or `Spicy`

The daemon reads real Windows system metrics using `PerformanceCounter` (CPU) and `GlobalMemoryStatusEx` (RAM), then simulates queue pressure dynamics for the throttling engine.

## Configuration lifecycle

1. `GET /config` — read current settings
2. Modify values (concurrency, thresholds, rate limits, etc.)
3. `POST /config` — apply changes (takes effect immediately)
4. `POST /config/reset` — restore factory defaults at any time

## Diagnostics

`POST /diagnostics/export` generates a JSON diagnostic bundle containing the current configuration, recent events, system metrics history, and throttling state. Useful for troubleshooting thermal issues or tuning thresholds.
