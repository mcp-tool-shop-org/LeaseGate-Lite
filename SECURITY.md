# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | Yes       |

## Reporting a Vulnerability

Email: **64996768+mcp-tool-shop@users.noreply.github.com**

Alternatively, use [GitHub's private vulnerability reporting](https://github.com/mcp-tool-shop-org/LeaseGate-Lite/security/advisories/new).

Include:
- Description of the vulnerability
- Steps to reproduce
- Version affected
- Potential impact

### Response timeline

| Action | Target |
|--------|--------|
| Acknowledge report | 48 hours |
| Assess severity | 7 days |
| Release fix | 30 days |

## Scope

LeaseGate Lite is a **local-first** MAUI desktop app and daemon for throttling AI workloads.

- **Data touched:** Local daemon config, system metrics (CPU%, RAM%), throttling state, event logs
- **Data NOT touched:** No cloud sync. No telemetry. No analytics. No user data collection
- **Network:** Daemon listens on `localhost:5177` only — no external network access
- **No secrets handling** — does not read, store, or transmit credentials
- **No telemetry** is collected or sent

### Known Security Considerations

- The daemon listens on `localhost:5177` with **no authentication**. Any local process can call the API. This is by design for home-PC scope.
- Diagnostics export may contain file paths and configuration details. Review before sharing.
- Simulation endpoints are always enabled. Do not expose the daemon to a network.
