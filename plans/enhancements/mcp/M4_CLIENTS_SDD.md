# M4_CLIENTS software design

Publish one framework-dependent .NET 10 server with compiled Beep.Skia libraries and package locks. Generate concrete Claude Desktop and Codex configuration snippets. Register the same workspace in installed Claude Code and Codex using their CLIs. Keep native/tool/component manifests and scoped acceptance records with the release.

## Implementation boundary

scripts/publish-mcp.ps1; docs/mcp/README.md; generated client snippets. Drawing state remains the existing native DTO; no client-specific renderer or parallel model. Input is untrusted data and never resolves arbitrary CLR types.

## Failure behavior

Return MCP tool errors with actionable domain messages. Preserve committed native data on rejected input. Revision conflict requires reading the current drawing. Runtime-only CLR fields are identified explicitly rather than advertised as portable edits.

## Verification

Require locked restore, warning-free publish, MCP/core tests, stdio and HTTP protocol checks, native artifact interoperability, and CLI registration/connection evidence. Distinguish these from a model-driven client session, physical device checks or remote hosting.
