# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-02-27

### Added

- Initial stable release
- One-tab MAUI control panel with auditable checklist layout
- Local daemon on localhost:5177 with real Windows system metrics
- Windows system tray companion
- Core throttling (concurrency, cooldown, background cap)
- Adaptive tuning (soft/hard thresholds, recovery rate)
- Request shaping (output clamp, overflow behavior, retry policy)
- Rate limiting (requests/min, tokens/min, burst allowance)
- 3 presets: Quiet (laptop), Balanced, Performance (desktop)
- Diagnostics export and event tail
- 178 xUnit tests
- Ship Gate compliance (security, docs, identity)

## [0.1.0] - 2026-02-12

### Added

- Initial pre-release with daemon and MAUI app
- Basic throttling and adaptive controls
- Simulation endpoints for testing
