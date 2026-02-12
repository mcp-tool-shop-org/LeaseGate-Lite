# LeaseGate-Lite v0.1.0

## What it does
- Smooths local AI workload bursts so your PC stays responsive during heavy usage.
- Applies practical throughput controls (concurrency, adaptive throttling, rate limits) with sensible defaults.
- Gives you clear, auditable visibility through live status, event trail, diagnostics export, and in-app audit harness.

## What it does NOT do
- It does **not** upload prompts or call external telemetry services by default.
- It does **not** act as cloud orchestration or multi-host fleet management.
- It does **not** replace your model runtime; it only governs local request pressure and pacing.

## Recommended settings
- **Laptop / thermals-first:** `Quiet` preset, keep UI responsive ON, start on login ON.
- **Desktop / balanced daily use:** `Balanced` preset (default), adjust only if needed.
- **High-throughput desktop:** `Performance` preset with monitoring of heat/clamp signals.

## Why this exists
LeaseGate-Lite is a quality-of-life utility for home PCs: smoother AI calls, less stutter, and fewer thermal spikes without constant tuning.
