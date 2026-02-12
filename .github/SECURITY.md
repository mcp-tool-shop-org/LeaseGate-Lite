# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | ✅        |
| < 0.1.0 | ❌        |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly:

1. **Do not** open a public issue
2. Email the maintainers or use [GitHub's private vulnerability reporting](https://github.com/mcp-tool-shop-org/LeaseGate-Lite/security/advisories/new)
3. Include steps to reproduce and potential impact

We will acknowledge within 48 hours and aim to release a fix within 7 days for critical issues.

## Known Security Considerations

- The daemon listens on `localhost:5177` with **no authentication**. Any local process can call the API. This is by design for home-PC scope, but be aware that local malware could reconfigure throttling.
- Diagnostics export may contain file paths and configuration details. Review before sharing.
- Simulation endpoints (`/simulate/flood`, `/simulate/pressure`) are always enabled. Do not expose the daemon to a network.
