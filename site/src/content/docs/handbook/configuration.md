---
title: Configuration
description: Throttling, rate limiting, and adaptive tuning options.
sidebar:
  order: 4
---

## Core throttling

These settings control how many AI calls run concurrently and how the daemon manages load:

- **Max concurrency** — upper limit on simultaneous AI calls
- **Interactive reserve** — slots reserved for user-initiated requests
- **Background cap** — maximum background/batch calls allowed
- **Cooldown** — delay between finishing one call and starting the next

## Adaptive tuning

The daemon automatically adjusts behavior based on system pressure:

- **Soft threshold** — when CPU or RAM exceeds this percentage, the daemon begins reducing concurrency gradually
- **Hard threshold** — when CPU or RAM exceeds this percentage, the daemon aggressively throttles to prevent thermal events
- **Recovery rate** — how quickly concurrency ramps back up after pressure drops
- **Smoothing** — dampening factor to prevent oscillation between throttled and unthrottled states

## Request shaping

Control how individual requests are constrained:

- **Max output** — maximum token output per request
- **Prompt clamp** — maximum prompt size allowed
- **Overflow behavior** — what happens when limits are exceeded (queue, reject, or truncate)
- **Retry policy** — automatic retry behavior for transient failures

## Rate limiting

Global rate controls applied across all AI calls:

- **Requests/min** — maximum number of requests per minute
- **Tokens/min** — maximum total tokens per minute
- **Burst allowance** — short-term burst capacity above the rate limit

## Applying changes

Configuration changes take effect immediately via `POST /config`. No daemon restart required. Use `POST /config/reset` to restore factory defaults at any time.

All settings are persisted locally. No cloud sync, no telemetry.
