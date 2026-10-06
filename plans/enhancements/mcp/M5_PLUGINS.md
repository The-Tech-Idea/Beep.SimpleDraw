# M5: Installable Codex and Claude Code plugins

## Goal

Distribute SimpleDraw as a user-installable local marketplace archive containing native host manifests, the tested server and embedded Beep.Skia libraries. Install it in the local Codex and Claude Code clients and retire our standalone registrations after plugin connection verification.

## Stages

1. Verify official manifest, marketplace and plugin path rules against installed CLI commands.
2. Add host templates and a shared discovery/edit/export workflow skill. Keep the server implementation and native document contract shared.
3. Package compiled server dependencies, native assets, 31 tool definitions and 249 component schemas. Exclude machine-specific config and acceptance paths. Generate payload hashes and an archive digest; preserve old releases.
4. Validate manifests, install both plugins, inspect the actual cached payloads and exercise each resolved launcher over MCP. Verify extraction to an unrelated directory with spaces.
5. Remove only our redundant standalone registrations after plugin verification; publish local installation/removal instructions and scoped evidence.

## Gate

Both CLI plugin installs pass; Claude component inventory contains the skill and MCP server; each cached server initializes, discovers all 31 tools/249 components and renders a portable artifact. Every archived file matches its manifest hash. Interactive model sessions, additional operating systems, public marketplace submission and live browser synchronization are separate acceptance items.
