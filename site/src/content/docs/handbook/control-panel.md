---
title: Control Panel
description: One-tab layout, audit checklist, and presets.
sidebar:
  order: 3
---

The LeaseGate-Lite control panel is a single-tab MAUI desktop app. Everything is visible on one screen — no hidden menus, no buried settings.

## Layout

- **Header strip** — status dot, mode picker, endpoint, quick actions (Start, Stop, Apply, Export Diag)
- **Left column** — auditable checklist with jump-to-card links
- **Right column** — ordered control cards matching checklist sections

Each card shows: current value, short meaning, controls, effect preview, and a coverage footer.

## Audit checklist

The checklist provides a structured walkthrough of every control surface:

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

## Presets

Three built-in profiles to match your hardware:

| Preset | Best for | Behavior |
|--------|----------|----------|
| **Quiet** | Laptops | Conservative limits, aggressive thermal protection, lower concurrency |
| **Balanced** | Typical desktops | Sensible limits for moderate AI workloads (default) |
| **Performance** | Desktops with headroom | Higher concurrency, relaxed thresholds, maximum throughput |

Laptop-like hardware gets a Quiet recommendation during first-run setup, but it is never forced — you always choose.

## Per-app profiles

The control panel lets you set per-application overrides. When the daemon sees requests with an `X-Client-AppId` header, it tracks the client and lets you assign a preset or custom limits (concurrency, background cap, token clamps, rate limits) for that specific app. This is useful when you run multiple AI tools simultaneously and want to prioritize one over others.

## Heat states

The status dot in the header strip reflects the current thermal state:

| State | Meaning |
|-------|---------|
| **Calm** | System pressure is low, running normally |
| **Warm** | Pressure has crossed the soft threshold, concurrency is being reduced |
| **Spicy** | Pressure has crossed the hard threshold, aggressive throttling is active |

## Design philosophy

LeaseGate-Lite intentionally excludes heavy governance features (approvals, signing, receipts). The goal is smoother AI calls with less stutter and fewer thermal spikes — nothing more.
