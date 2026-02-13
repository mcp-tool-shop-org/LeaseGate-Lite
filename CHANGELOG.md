# Changelog

All notable changes to LeaseGate-Lite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-02-12

### Fixed
- MAUI app no longer crashes when daemon is not running
- Added centralized error handling for all daemon API calls
- All button clicks now gracefully handle connection failures with user-friendly error messages
- Fixed platform compatibility issue with WinUI exception handler (iOS/Android builds now succeed)

### Changed
- Improved error messages to clearly indicate when daemon is offline
- Wrapped all HTTP operations in `SafeDaemonCallAsync` for consistent error handling

## [0.1.0] - 2026-02-12

### Added
- Initial release of LeaseGate-Lite
- Windows daemon for MCP server throttling
- MAUI desktop app for configuration and monitoring
- Real-time system metrics monitoring (CPU, RAM)
- Adaptive concurrency limiting based on system pressure
- Per-app profile overrides
- First-run wizard
- Live event stream
- Diagnostics export
