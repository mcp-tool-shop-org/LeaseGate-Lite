import type { SiteConfig } from '@mcptoolshop/site-theme';

export const config: SiteConfig = {
  title: 'LeaseGate-Lite',
  description: 'A one-tab MAUI control surface and local daemon for throttling AI workloads on Windows — smoother calls, less stutter, fewer thermal spikes.',
  logoBadge: 'LG',
  brandName: 'LeaseGate-Lite',
  repoUrl: 'https://github.com/mcp-tool-shop-org/LeaseGate-Lite',
  footerText: 'MIT Licensed — built by <a href="https://github.com/mcp-tool-shop-org" style="color:var(--color-muted);text-decoration:underline">mcp-tool-shop-org</a>',

  hero: {
    badge: 'Windows desktop',
    headline: 'Throttle AI workloads,',
    headlineAccent: 'keep your PC smooth.',
    description: 'A one-tab MAUI control surface and local daemon that manages concurrency, rate limits, and thermal pressure for local AI — fewer stutters, fewer thermal spikes.',
    primaryCta: { href: '#usage', label: 'Get started' },
    secondaryCta: { href: 'handbook/', label: 'Read the Handbook' },
    previews: [
      { label: 'Start daemon', code: 'dotnet run --project src/LeaseGateLite.Daemon' },
      { label: 'Status', code: 'GET /status → { cpu: 42%, heat: "Calm", queue: 2 }' },
      { label: 'Throttle', code: 'POST /config → { maxConcurrency: 3, cooldown: 5s }' },
    ],
  },

  sections: [
    {
      kind: 'features',
      id: 'features',
      title: 'Features',
      subtitle: 'Everything you need to keep AI workloads under control.',
      features: [
        { title: 'Live system metrics', desc: 'Real CPU%, RAM%, queue depth, and heat state (Calm/Warm/Spicy) via Windows PerformanceCounter and GlobalMemoryStatusEx.' },
        { title: 'Adaptive throttling', desc: 'Soft and hard thresholds with recovery rate and smoothing — automatically backs off when your system gets hot.' },
        { title: 'One-tab control panel', desc: 'MAUI desktop app with an auditable checklist layout — every setting visible, every action explicit.' },
      ],
    },
    {
      kind: 'code-cards',
      id: 'usage',
      title: 'Usage',
      cards: [
        {
          title: 'Start the daemon',
          code: '# Start the local API daemon\ndotnet run --project src/LeaseGateLite.Daemon\n\n# Daemon listens on localhost:5177',
        },
        {
          title: 'Launch the control panel',
          code: '# Build and run the MAUI app (Windows)\ndotnet build src/LeaseGateLite.App \\\n  -f net10.0-windows10.0.19041.0\ndotnet run --project src/LeaseGateLite.App \\\n  -f net10.0-windows10.0.19041.0',
        },
      ],
    },
    {
      kind: 'data-table',
      id: 'endpoints',
      title: 'Daemon Endpoints',
      subtitle: 'Local REST API on localhost:5177.',
      columns: ['Method', 'Path', 'Description'],
      rows: [
        ['GET', '/status', 'Live snapshot: CPU%, RAM%, queue depth, heat state'],
        ['GET', '/config', 'Current configuration'],
        ['POST', '/config', 'Apply new configuration'],
        ['POST', '/config/reset', 'Reset to defaults'],
        ['POST', '/service/start', 'Start the daemon'],
        ['POST', '/service/stop', 'Stop the daemon'],
        ['POST', '/service/restart', 'Restart the daemon'],
        ['POST', '/diagnostics/export', 'Export JSON diagnostic bundle'],
        ['GET', '/events/tail?n=200', 'Event tail'],
      ],
    },
    {
      kind: 'features',
      id: 'presets',
      title: 'Presets',
      subtitle: 'Three built-in profiles to match your hardware.',
      features: [
        { title: 'Quiet', desc: 'For laptops — conservative limits, aggressive thermal protection, lower concurrency.' },
        { title: 'Balanced', desc: 'The default — sensible limits for typical desktop use with moderate AI workloads.' },
        { title: 'Performance', desc: 'For desktops with headroom — higher concurrency, relaxed thresholds, maximum throughput.' },
      ],
    },
  ],
};
