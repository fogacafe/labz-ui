# Compact status footer styling

The footer uses a compact 32px row, subtle top divider, left-aligned status,
and right-aligned environment, tenant and user identity.

Environment badges have recognizable accents:

| Environment | Accent |
| --- | --- |
| PROD / PRODUCTION / PRODUÇÃO | Muted red |
| UAT / HML / STAGING / HOMOLOG | Amber |
| DEV / DEVELOPMENT | Blue |
| Other / missing | Neutral gray |

Accent matching is case-insensitive and presentation-only. The host is still
responsible for the authoritative environment and user/tenant context.

Use `LabzStatusBar.GetEnvironmentColor(name)` for a consistent indicator
elsewhere in the application. Context click events remain unchanged.
