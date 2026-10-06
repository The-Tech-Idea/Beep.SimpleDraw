# M4_CLIENTS: Claude and Codex delivery

## Deliverables

Publish one framework-dependent .NET 10 server with compiled Beep.Skia libraries and package locks. Generate concrete Claude Desktop and Codex configuration snippets. Register the same workspace in installed Claude Code and Codex using their CLIs. Keep native/tool/component manifests and scoped acceptance records with the release.

Active implementation: scripts/publish-mcp.ps1; docs/mcp/README.md; generated client snippets.

## Gate and evidence

Require locked restore, warning-free publish, MCP/core tests, stdio and HTTP protocol checks, native artifact interoperability, and CLI registration/connection evidence. Distinguish these from a model-driven client session, physical device checks or remote hosting.

Record actual evidence in docs/evidence/mcp-* and the MCP master tracker. Do not infer client/model acceptance from compilation or registration alone.
