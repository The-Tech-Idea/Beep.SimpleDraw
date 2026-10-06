# M2_EDITING software design

Use DrawingSession and existing DiagramDto/ComponentEditAction for components, sketches, paths, text, styles, geometry, grouping, ordering, selection, connectors and drawing undo/redo. Programmatic text targets an exact identity rather than pointer hit-testing. Grouped IDs are selected once to avoid toggling. Native/property/geometry validation happens before persistence. Every mutation requires expectedRevision; stale calls reject explicitly.

## Implementation boundary

SimpleDrawTools.cs; DrawingWorkspace.cs; existing DrawingSession. Drawing state remains the existing native DTO; no client-specific renderer or parallel model. Input is untrusted data and never resolves arbitrary CLR types.

## Failure behavior

Return MCP tool errors with actionable domain messages. Preserve committed native data on rejected input. Revision conflict requires reading the current drawing. Runtime-only CLR fields are identified explicitly rather than advertised as portable edits.

## Verification

Exercise real create/edit/connect/group/history calls through JSON-RPC. Verify exact text target under overlapping text, grouped selections, invalid-property/motion rollback, finite values, and two workspace instances rejecting stale revisions. History is local to a process and resets after external changes/restart.
