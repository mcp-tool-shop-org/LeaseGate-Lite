# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [1.0.1] - 2026-03-19

### Fixed

- DaemonVersion now correctly reports `1.0.0` instead of `0.2.0-lite`
- Event log file no longer grows unbounded — rotates at 5 MB, keeps one generation
- Event writes are buffered (flush every 500ms or 50 entries) instead of synchronous per-event
- Event stream endpoint no longer holds the global lock during long-poll
- Tray app resolves control panel path for packaged layout (sibling `app/` folder)
- Tray polls notification settings every 30s instead of every 2s
- Regex redaction uses source-generated `[GeneratedRegex]` instead of re-creating per call

### Added

- `--enable-simulation` flag — simulation endpoints disabled by default in production
- `--require-auth` flag — optional token-based auth (token stored in `%LOCALAPPDATA%\LeaseGateLite\daemon.token`)

### Changed

- Contracts target simplified to `net10.0` only (dropped unused `net9.0`)

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
