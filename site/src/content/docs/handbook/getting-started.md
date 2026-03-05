---
title: Getting Started
description: Install, run the daemon, and launch the control panel.
sidebar:
  order: 1
---

## Projects

LeaseGate-Lite is a multi-project .NET solution:

| Project | Description |
|---------|-------------|
| `src/LeaseGateLite.Contracts` | Shared DTOs and enums (net9.0 + net10.0) |
| `src/LeaseGateLite.Daemon` | Local API daemon on `localhost:5177` with real Windows system metrics |
| `src/LeaseGateLite.App` | One-tab MAUI control panel (Windows/Android/iOS/macCatalyst) |
| `src/LeaseGateLite.Tray` | Windows system tray companion |
| `tests/LeaseGateLite.Tests` | 178 xUnit tests (config validation, simulation, diagnostics) |

## Run the daemon

```powershell
dotnet run --project src/LeaseGateLite.Daemon
```

The daemon starts on `localhost:5177` and immediately begins collecting real system metrics (CPU via PerformanceCounter, RAM via GlobalMemoryStatusEx).

## Launch the control panel

```powershell
dotnet build src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
dotnet run --project src/LeaseGateLite.App -f net10.0-windows10.0.19041.0
```

The MAUI app connects to the daemon automatically and displays live status.

## One-click packaging (Windows)

Create a release artifact (portable zip + SHA256 checksum):

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-v0.1.0.ps1
```

Install locally from the packaged artifact:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install-local.ps1 -EnableAutostart
```

After installation:
- The daemon runs immediately and can start on login
- The control panel launches and connects automatically
- Balanced is the default preset; laptop-like hardware gets a Quiet recommendation in first-run setup (never forced)

## Run the tests

```powershell
dotnet test tests/LeaseGateLite.Tests
```

All 178 tests use a `FakeSystemMetrics` provider via dependency injection for deterministic, hardware-independent verification.
