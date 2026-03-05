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

## Design philosophy

LeaseGate-Lite intentionally excludes heavy governance features (approvals, signing, receipts). The goal is smoother AI calls with less stutter and fewer thermal spikes — nothing more.
