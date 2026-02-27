# Scorecard

> Score a repo before remediation. Fill this out first, then use SHIP_GATE.md to fix.

**Repo:** LeaseGate-Lite
**Date:** 2026-02-27
**Type tags:** [desktop]

## Pre-Remediation Assessment

| Category | Score | Notes |
|----------|-------|-------|
| A. Security | 7/10 | SECURITY.md in .github/ (non-standard), good content but no root copy |
| B. Error Handling | 8/10 | MAUI app with user-friendly messages, structured daemon responses |
| C. Operator Docs | 7/10 | README comprehensive, no CHANGELOG, no SHIP_GATE |
| D. Shipping Hygiene | 6/10 | Package scripts exist, no CHANGELOG, version at 0.1.0 |
| E. Identity (soft) | 10/10 | Logo, translations, landing page, metadata all present |
| **Overall** | **38/50** | |

## Key Gaps

1. SECURITY.md only in .github/ — needs root copy
2. No CHANGELOG.md
3. No SHIP_GATE.md or SCORECARD.md
4. Version still at v0.1.0

## Remediation Priority

| Priority | Item | Estimated effort |
|----------|------|-----------------|
| 1 | Add root SECURITY.md with data scope | 5 min |
| 2 | Add CHANGELOG.md with 1.0.0 entry | 5 min |
| 3 | Fill SHIP_GATE.md, SCORECARD.md, update README | 15 min |

## Post-Remediation

| Category | Before | After |
|----------|--------|-------|
| A. Security | 7/10 | 10/10 |
| B. Error Handling | 8/10 | 10/10 |
| C. Operator Docs | 7/10 | 10/10 |
| D. Shipping Hygiene | 6/10 | 10/10 |
| E. Identity (soft) | 10/10 | 10/10 |
| **Overall** | 38/50 | 50/50 |
