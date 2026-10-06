# Stage 2: Complete everyday sketch editing — SDD
Date: 2026-10-05. Status: engineering verified; independent acceptance open. See master tracker.
Estimate: 5–8 engineer-days. Entry dependencies: P1 usable vertical slice.

## 0. Problem statement
Extend SketchShape and its explicit persisted properties; line/freehand geometry is shape-local and hit tested by distance. Arrowheads derive from endpoint tangents; endpoint direction survives negative drags. Text uses an HTML editor overlay and Skia font metrics, with multiline bounds. Add DrawingAction subtypes for style/resize/group operations in existing history; grouping requires explicit persisted membership, not nested duplicated scenes. Roughness uses seeded geometry generated once per shape so it is stable across paints and exports.

## 1. Goals and acceptance criteria
1. Add line, arrow, pen, text and eraser with one undo entry per gesture.
2. Edit stroke/fill/width and text; changes roundtrip and undo correctly.
3. Select several objects, resize, duplicate and reorder without losing identity.

## 2. Design
Extend SketchShape and its explicit persisted properties; line/freehand geometry is shape-local and hit tested by distance. Arrowheads derive from endpoint tangents; endpoint direction survives negative drags. Text uses an HTML editor overlay and Skia font metrics, with multiline bounds. Add DrawingAction subtypes for style/resize/group operations in existing history; grouping requires explicit persisted membership, not nested duplicated scenes. Roughness uses seeded geometry generated once per shape so it is stable across paints and exports.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
SketchShape Kind/style/path/text/group/roughness/seed properties and DrawingSession.SetStyle, DuplicateSelection, GroupSelection, ReorderSelection and text editing are implemented. Resize previews capture SketchEditAction.State and commit one existing history action. HistoryManager.BeginBatch makes duplication atomic; order actions preserve identities and undo paint order. No HTTP API.

Implemented contracts: SketchShape now supports Line/Arrow/Freehand, ordered normalized PathData, distance hit tests and whole-segment eraser sweep intersection. DrawingSession.SetStyle edits the active selection in Select mode through shared SketchStyleAction; other modes set new-shape defaults. Shift selection and drag move all selected shapes with one MoveComponentsAction. Eraser collects a preview and commits existing DeleteSelectedComponents once. Delete undo restores original component paint order.

Native envelope v4 uses Beep DiagramDto schema 3 and the same fixed SketchShape factory. PathData is invariant ordered normalized x,y pairs. Limits: 4096 points/120,000 characters per path, 2 MiB/file, 2000 shapes. Line/arrow need two distinct points; freehand at least two; closed/Text need an empty path. Coalesced input travels in ordered batches of 256. Text has 4096 characters/64 lines/512 characters per line and an 8–120 px font; Kalam is embedded and self-hosted, with unsupported glyphs rejected. Every current component has a unique stable ID; nonempty group IDs require at least two members and cannot equal a component ID. Duplication remaps both identities. Roughness 0–2 and seed persist; cached xorshift outlines stay stable across paint/export. V1–V3 have explicit migration rules in the global SDD.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| Modify | ../Beep.Skia/Beep.Skia/Components/SketchShape.cs | ~200 | high |
| Modify | src/Beep.SimpleDraw.Core/DrawingSession.cs | ~200 | high |
| New | src/Beep.SimpleDraw.Core/Actions/SketchEditAction.cs | ~100 | medium |
| Modify | src/Beep.SimpleDraw.Web/Pages/Home.razor | ~150 | medium |

## 5. Error handling matrix
Degenerate stroke commits nothing; missing font yields an explicit bundled-font choice; cancelled text edit changes no scene; invalid style is rejected before history mutation.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Test reversed arrow direction, freehand segments, distance hit tests, multiline text roundtrip, style undo, resize cancel, duplicate IDs and redo invalidation. Browser create all tools, style and undo each once.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P2_EDITING.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Freehand input coalescing can discard geometry: preserve sampled points in event batches. Group semantics can break connectors: define delete/move behavior before group UI. Avoid per-paint randomness.

## 9. Out of scope
Advanced Beep diagram families, external images and remote collaboration.

## 10. Sub-task execution order
2.A.1. Add line/arrow and geometry hit tests. Verify against the corresponding stage acceptance scenario before advancing.
2.A.2. Add freehand sampling and eraser transactions. Verify against the corresponding stage acceptance scenario before advancing.
2.A.3. Add text overlay with bundled font metrics. Verify against the corresponding stage acceptance scenario before advancing.
2.B.1. Add style inspector and undoable edits. Verify against the corresponding stage acceptance scenario before advancing.
2.B.2. Add multiselect, resize, duplication and reorder. Verify against the corresponding stage acceptance scenario before advancing.
2.B.3. Add explicit grouping and deterministic sketch styling. Verify against the corresponding stage acceptance scenario before advancing.
2.M.1. Run editing regressions and browser workflow acceptance. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 2 row and review handoff. ENG performs implementation; QA records actual evidence; PO owns product/scope acceptance; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.
