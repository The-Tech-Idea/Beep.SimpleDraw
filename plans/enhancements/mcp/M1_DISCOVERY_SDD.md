# M1_DISCOVERY software design

One pinned official SDK host exposes stdio and optional loopback Streamable HTTP. Fixed factories remain the authority for all 249 standalone components and 16 families. Discovery paginates exact type keys; descriptions expose native scalar/JSON fields, defaults, enum choices, help, default ports and runtime-only property metadata. Resources provide the guide, component schemas and native drawings.

## Implementation boundary

CatalogDescription.cs; SimpleDrawResources.cs; Program.cs. Drawing state remains the existing native DTO; no client-specific renderer or parallel model. Input is untrusted data and never resolves arbitrary CLR types.

## Failure behavior

Return MCP tool errors with actionable domain messages. Preserve committed native data on rejected input. Revision conflict requires reading the current drawing. Runtime-only CLR fields are identified explicitly rather than advertised as portable edits.

## Verification

Every compiled catalog entry is described through MCP, and every persisted key appears in its descriptor. Wire tools/list must expose real input schemas. stdout is JSON-RPC only; HTTP binds loopback and rejects foreign Host/Origin.
