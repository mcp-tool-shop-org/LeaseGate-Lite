---
title: Configuration
description: Throttling, rate limiting, and adaptive tuning options.
sidebar:
  order: 4
---

All configuration is stored locally at `%LOCALAPPDATA%\LeaseGateLite\leasegatelite.config.json`. No cloud sync, no telemetry. Changes take effect immediately via `POST /config` with no daemon restart required.

## Core throttling

These settings control how many AI calls run concurrently and how the daemon manages load:

| Setting | Default | Description |
|---------|---------|-------------|
| **MaxConcurrency** | 8 | Upper limit on simultaneous AI calls |
| **InteractiveReserve** | 2 | Slots reserved for user-initiated requests |
| **BackgroundCap** | 6 | Maximum background/batch calls allowed |
| **CooldownBehavior** | `Mild` | Delay between calls: `Off`, `Mild`, or `Aggressive` |

## Adaptive tuning

The daemon automatically adjusts behavior based on system pressure:

| Setting | Default | Description |
|---------|---------|-------------|
| **SoftThresholdPercent** | 70 | CPU/RAM percentage where concurrency begins reducing gradually |
| **HardThresholdPercent** | 90 | CPU/RAM percentage where aggressive throttling kicks in |
| **RecoveryRatePercent** | 20 | How quickly concurrency ramps back up after pressure drops |
| **SmoothingPercent** | 40 | Dampening factor to prevent oscillation between states |

When the smoothed pressure crosses the soft threshold, the daemon reduces effective concurrency proportionally. If pressure hits the hard threshold, the daemon clamps to minimum concurrency and enters `Spicy` heat state.

## Request shaping

Control how individual requests are constrained:

| Setting | Default | Description |
|---------|---------|-------------|
| **MaxOutputTokensClamp** | 1024 | Maximum token output per request |
| **MaxPromptTokensClamp** | 4096 | Maximum prompt size allowed |
| **OverflowBehavior** | `TrimOldest` | What happens when the queue is full: `TrimOldest`, `Deny`, or `QueueOnly` |
| **MaxRetries** | 2 | Automatic retries for transient failures |
| **RetryBackoffMs** | 500 | Milliseconds between retries |

## Rate limiting

Global rate controls applied across all AI calls:

| Setting | Default | Description |
|---------|---------|-------------|
| **RequestsPerMinute** | 120 | Maximum number of requests per minute |
| **TokensPerMinute** | 120,000 | Maximum total tokens per minute |
| **BurstAllowance** | 12 | Short-term burst capacity above the rate limit |

## Per-app profiles

The daemon tracks connected clients via `X-Client-AppId`, `X-Process-Name`, and `X-Client-Signature` headers. You can set per-app overrides that take precedence over global settings:

- `MaxConcurrency`, `BackgroundCap`, `MaxOutputTokensClamp`, `MaxPromptTokensClamp`, `RequestsPerMinute`, `TokensPerMinute`

Use `GET /profiles` to see recently connected apps, then `POST /profiles/apply` to set an override for a specific client.

## Applying changes

Configuration changes take effect immediately via `POST /config`. No daemon restart required. Use `POST /config/reset` (with `apply=true`) to restore factory defaults at any time.

Use `POST /preset/preview` before applying a preset to see exactly what will change.
