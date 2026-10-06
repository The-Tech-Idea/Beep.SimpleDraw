# M3_ARTIFACTS: Persistence, libraries and rendered artifacts

## Deliverables

Persist native version 7 data with library metadata in a bounded local workspace. Atomic replacements and a cross-process lease prevent torn/lost writes. Existing SketchLibrary validates entries and remaps component/group/port identities. Existing DrawingExport produces PNG/SVG; MCP returns inline image content as well as content-hashed native/image/library artifact paths. Browser exchange uses native files and does not silently alter an existing browser tab.

Active implementation: DrawingWorkspace.cs; SimpleDrawTools.cs; existing DrawingExport/SketchLibrary.

## Gate and evidence

Verify restart preserves the full native graph, library insertion duplicates internal connections with fresh identities, exports decode/render, resources return exact native data, and content-hashed artifacts distinguish export options. Preserve the user preview drawing.

Record actual evidence in docs/evidence/mcp-* and the MCP master tracker. Do not infer client/model acceptance from compilation or registration alone.
