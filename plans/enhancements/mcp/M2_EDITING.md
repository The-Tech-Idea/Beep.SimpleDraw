# M2_EDITING: Shared editing and revision control

## Deliverables

Use DrawingSession and existing DiagramDto/ComponentEditAction for components, sketches, paths, text, styles, geometry, grouping, ordering, selection, connectors and drawing undo/redo. Programmatic text targets an exact identity rather than pointer hit-testing. Grouped IDs are selected once to avoid toggling. Native/property/geometry validation happens before persistence. Every mutation requires expectedRevision; stale calls reject explicitly.

Active implementation: SimpleDrawTools.cs; DrawingWorkspace.cs; existing DrawingSession.

## Gate and evidence

Exercise real create/edit/connect/group/history calls through JSON-RPC. Verify exact text target under overlapping text, grouped selections, invalid-property/motion rollback, finite values, and two workspace instances rejecting stale revisions. History is local to a process and resets after external changes/restart.

Record actual evidence in docs/evidence/mcp-* and the MCP master tracker. Do not infer client/model acceptance from compilation or registration alone.
