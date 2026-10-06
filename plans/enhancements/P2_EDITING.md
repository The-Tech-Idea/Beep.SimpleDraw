# Stage 2: Complete everyday sketch editing — tasks
[SDD](P2_EDITING_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: P1 usable vertical slice. Estimate: 5–8 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 2.A.1 | Add line/arrow and geometry hit tests | Automated + desktop browser verified | Reversed/horizontal/vertical arrows, distance hit tests, real paint, native roundtrip |
| 2.A.2 | Add freehand sampling and eraser transactions | Automated + desktop browser verified | Ordered coalesced samples; single eraser undo; cancel and paint-order restoration |
| 2.A.3 | Add text overlay with bundled font metrics | Automated + desktop browser verified | Kalam embedded/self-hosted; multiline create/edit/cancel, font resize, glyph validation, actual WASM file native roundtrip |
| 2.B.1 | Add style inspector and undoable edits | Automated + desktop browser verified | Mixed selection, atomic validation, one action per style command; browser undo restored original colors |
| 2.B.2 | Add multiselect, resize, duplication and reorder | Automated + desktop browser verified | Bounds and arrow endpoint resize/cancel; one duplicate undo with fresh IDs; reorder/history preserves paint order |
| 2.B.3 | Add explicit grouping and deterministic sketch styling | Automated + desktop browser verified | Group selection/move/erase; duplicate group remapping; persisted deterministic seed and outline cache |
| 2.M.1 | Run editing regressions and browser workflow acceptance | Engineering verified; independent acceptance open | 50 C# tests, 7 bridge tests; published all-tool create/style/undo, grouping/copy/reorder, actual v4 file and screenshot |

## Verification scenarios
Test reversed arrow direction, freehand segments, distance hit tests, multiline text roundtrip, style undo, resize cancel, duplicate IDs and redo invalidation. Browser create all tools, style and undo each once.

## Closure record
Implementation: all P2 editing tasks implemented. Automated evidence: 50 C# tests and 7 Node bridge tests, retained net8 host build and native-linked publish passed on 2026-10-05. Twelve sibling serialization/typeface tests also passed; this is scoped regression evidence. Browser evidence: all seven tools, multiline edit/cancel, selected styles with undo for each kind, resize and endpoint undo, group duplication as one action, reorder, group erase/undo, actual v4 export imported by native tests. See [verification](../../docs/VERIFICATION.md). Independent review/owner acceptance remains pending.

## Next action
Proceed to P3 reliable local documents and exports. P4 handles full responsive, touch, focus and accessibility review.
