# Stage 5: Default Beep.Skia catalog and personal diagram reuse — SDD
Date: 2026-10-06. Status: default catalog, personal library, connectors and recovery workflows implemented and locally verified; independent acceptance remains open. See the master tracker and P5_DEFAULT_SKIA_CATALOG.md for the current delivery.
Estimate: 4–7 engineer-days. Entry dependencies: P2 selection/style and P3 safe document persistence.

## 0. Problem statement
Library entries contain an allowlisted DiagramDto subset and metadata, stored separately from the active board. Insertion assigns fresh stable IDs where supported and remaps connection points. Embed the reviewed sixteen Beep.Skia families through nineteen project references and fixed factories; validate every factory in the trimmed browser release. Extend the native importer registry with concrete factories; do not expose the framework's reflection loader to untrusted files. The Beep.Skia catalog is open by default; the personal library panel stays collapsed until used. Excalidraw interchange, if commissioned, is a separate strict converter with explicit unsupported-element report, never a silent fallback.

## 1. Goals and acceptance criteria
1. Store/reuse local sketch selections as library entries.
2. Insert all 249 reviewed standalone Beep shapes through a searchable family catalog without requiring a family choice before sketching.
3. Validate each new family with a fixed registered factory and loss-free serialization.

## 2. Design
Library entries contain an allowlisted DiagramDto subset and metadata, stored separately from the active board. Insertion assigns fresh stable IDs where supported and remaps connection points. Embed the reviewed sixteen Beep.Skia families through nineteen project references and fixed factories; validate every factory in the trimmed browser release. Extend the native importer registry with concrete factories; do not expose the framework's reflection loader to untrusted files. The Beep.Skia catalog is open by default; the personal library panel stays collapsed until used. Excalidraw interchange, if commissioned, is a separate strict converter with explicit unsupported-element report, never a silent fallback.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
Implemented: SketchLibrary.SaveSelection(manager, name), Add/Remove/Import/Parse/Serialize and DrawingSession.InsertDiagram(dto, location). Library v1 is bounded to 1 MiB, 100 entries, 100 components and 256 KiB per entry, with names limited to 80 characters. Each entry contains a strict native document normalized to origin, preserving component order, groups, geometry, text and style. Whole proposed-library validation precedes mutation; imported entries receive fresh metadata IDs. Insertion reuses native preparation and one DrawingManager batch with fresh component/group IDs, validates the resulting board before mutation, and preserves active drafts on refusal.

The existing documents.js storage factory serves independent drawing/library channels, scoped keys and writer locks, with the same compare-and-swap, recovery, quota and file-only policy. Library initialization is lazy when its collapsed panel opens. Removing an entry exposes one-level undo; duplicate visible names receive ordinal labels without changing persisted content. Library exports remain available in read-only/file-only mode.

Implemented P5B object registry: FlowchartDefinitions over existing ComponentDto, with exact concrete ProcessNode/DecisionNode/StartEndNode constructors. Native v6 supports these keys and reviewed static connectors; v1–v4 sketch migration remains explicit. Ordered port IDs are globally distinct and preserved on open, remapped on insertion/duplication. ComponentEditAction reuses SketchEditAction.State and captures immutable family history values; the same instances survive undo. Selection, pointer/keyboard movement, erasing, mixed grouping, order, resize, duplication, styles and label edits share DrawingSession and the one DrawingManager.

The Workflow templates disclosure starts collapsed. Family labels expose one bounded single-line input and explicit Apply command, preserving drafts when unrelated viewport/style state refreshes. Numeric resize enforces family minimum dimensions. Sketch roughness controls are disabled for family/mixed selection. Family PNG/SVG reuse RenderingHelper and required Kalam glyph runs; crop margins include ports.

ConnectorDefinitions validates the reviewed static LineDto profile and exact output/input references before scene changes. Native versions 1–5 reject connectors; v6 permits up to 4,000. Copy/library subsets retain internal edges, remap fresh ports and validate the combined graph before one batch. Exact-line history preserves paint order, style, availability and component relationships through remove/delete/rewire. ConnectionLine uses required Kalam glyph outlines and shared export bounds. The collapsed connector panel supports named endpoints/available port indices, three routes, labels, creation and undoable removal. No arbitrary reflection loading or workflow execution is used.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| New | src/Beep.SimpleDraw.Core/Library/SketchLibrary.cs | ~160 | medium |
| Modify | src/Beep.SimpleDraw.Core/SimpleDrawDocument.cs | ~100 | high |
| New | src/Beep.SimpleDraw.Web/Components/LibraryPanel.razor | ~120 | medium |
| Modify | src/Beep.SimpleDraw.Web/Beep.SimpleDraw.Web.csproj | ~15 | medium |

## 5. Error handling matrix
Unknown family/type is rejected with a report; broken connector references reject entry before insertion; unavailable assets do not become invented placeholders.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Selection-library-document roundtrip, fresh identity and connector remapping, family properties/history, browser trimming and incremental payload. PO checks that first-open UI remains simple.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P5_LIBRARIES.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Reflection/trimming and family package payload: explicit factory registration, release roundtrip and payload gates. Stored external SVG requires a sanitization design before insertion.

## 9. Out of scope
All 16 diagram families by default, automation execution, arbitrary plugin downloads, uncontrolled SVG assets.

## 10. Sub-task execution order
5.A.1. Define local library schema and store. Verify against the corresponding stage acceptance scenario before advancing.
5.A.2. Implement selection save/insert with identity remapping. Verify against the corresponding stage acceptance scenario before advancing.
5.B.1. Select minimal Beep families and test WASM payload. Verify against the corresponding stage acceptance scenario before advancing.
5.B.2. Register reviewed templates and collapsed palette. Verify against the corresponding stage acceptance scenario before advancing.
5.M.1. Verify family roundtrip and simple-first UX. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 5 row and review handoff. ENG performs implementation; QA records actual evidence; PO owns product/scope acceptance; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.
