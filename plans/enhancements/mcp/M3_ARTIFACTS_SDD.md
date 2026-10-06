# M3_ARTIFACTS software design

Persist native version 7 data with library metadata in a bounded local workspace. Atomic replacements and a cross-process lease prevent torn/lost writes. Existing SketchLibrary validates entries and remaps component/group/port identities. Existing DrawingExport produces PNG/SVG; MCP returns inline image content as well as content-hashed native/image/library artifact paths. Browser exchange uses native files and does not silently alter an existing browser tab.

## Implementation boundary

DrawingWorkspace.cs; SimpleDrawTools.cs; existing DrawingExport/SketchLibrary. Drawing state remains the existing native DTO; no client-specific renderer or parallel model. Input is untrusted data and never resolves arbitrary CLR types.

## Failure behavior

Return MCP tool errors with actionable domain messages. Preserve committed native data on rejected input. Revision conflict requires reading the current drawing. Runtime-only CLR fields are identified explicitly rather than advertised as portable edits.

## Verification

Verify restart preserves the full native graph, library insertion duplicates internal connections with fresh identities, exports decode/render, resources return exact native data, and content-hashed artifacts distinguish export options. Preserve the user preview drawing.
