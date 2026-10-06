# Stage 1: Portable foundation and working Beep.Skia canvas — SDD
Date: 2026-10-05. Status: implemented; automated and desktop browser verified. Independent owner review remains open; see master tracker.
Estimate: 3–5 engineer-days. Entry dependencies: Specification and ADR 001.

## 0. Problem statement
Repair SkiaHostComponent with SKCanvasView markup, CSS-pixel pointer capture and callbacks. Add SketchShape under existing Components. DrawingSession owns draft/drag state and invokes DrawingManager history; SimpleDrawDocument validates native DTOs. Web UI provides a blank canvas, HTML tools, native save/open and object selection. Reuse existing MoveComponentsAction; never introduce a snapshot undo stack.

## 1. Goals and acceptance criteria
1. Build and publish a standalone WASM app using the active Beep.Skia core.
2. Draw rectangle, ellipse and diamond; select/move, delete, undo/redo and pan/zoom.
3. Roundtrip an editable native document with all implemented styles.
4. Verify real canvas paint and pointer release/cancel behavior.

## 2. Design
Repair SkiaHostComponent with SKCanvasView markup, CSS-pixel pointer capture and callbacks. Add SketchShape under existing Components. DrawingSession owns draft/drag state and invokes DrawingManager history; SimpleDrawDocument validates native DTOs. Web UI provides a blank canvas, HTML tools, native save/open and object selection. Reuse existing MoveComponentsAction; never introduce a snapshot undo stack.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
Planned C# contracts: DrawingSession.PointerDown(SKPoint), PointerMove(SKPoint), PointerUp(SKPoint), CancelGesture(); SimpleDrawDocument.Serialize(DrawingManager), Parse(string), Apply(DrawingManager, DiagramDto). Host callbacks carry action/position/modifiers; paint uses Action<SKPaintSurfaceEventArgs>. No HTTP API.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| Modify | ../Beep.Skia/Beep.Skia.Blazor.Controls/SkiaHostComponent.cs | ~180 | high |
| New | ../Beep.Skia/Beep.Skia.Blazor.Controls/wwwroot/skiaHost.js | ~100 | high |
| New | ../Beep.Skia/Beep.Skia/Components/SketchShape.cs | ~100 | medium |
| New | src/Beep.SimpleDraw.Core/{DrawingSession,SimpleDrawDocument}.cs | ~350 | medium |
| New | src/Beep.SimpleDraw.Web/Pages/Home.razor | ~200 | medium |

## 5. Error handling matrix
Native asset failure shows the application error. Invalid uploaded DTO leaves the board intact. Cancelled gesture rolls back draft/move. No arbitrary component activation.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
dotnet build Beep.SimpleDraw.slnx; dotnet test tests/Beep.SimpleDraw.Tests; dotnet publish src/Beep.SimpleDraw.Web -c Release. Browser: draw three shapes at different coordinates, move, undo/redo, export/reopen, release outside and cancel.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P1_FOUNDATION.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Native linker/workload, reflected properties and heavy dependencies: install build workload, use typed sketch properties, test release and record payload. Browser host contracts: preserve manager/descriptor public surface and compile existing net8 target.

## 9. Out of scope
Full editing tools, storage reliability, release-quality offline installation and stakeholder acceptance belong to later phases.

## 10. Sub-task execution order
1.A.1. Audit actual Beep.Skia contracts and select static architecture. Verify against the corresponding stage acceptance scenario before advancing.
1.A.2. Repair existing Blazor canvas host and pointer lifecycle. Verify against the corresponding stage acceptance scenario before advancing.
1.A.3. Add reusable SketchShape and extend framework target support. Verify against the corresponding stage acceptance scenario before advancing.
1.B.1. Implement session with draft/drag/cancel and existing history. Verify against the corresponding stage acceptance scenario before advancing.
1.B.2. Implement validated native envelope and file roundtrip. Verify against the corresponding stage acceptance scenario before advancing.
1.B.3. Build accessible initial web shell and shape controls. Verify against the corresponding stage acceptance scenario before advancing.
1.M.1. Run build and focused behavior/render/document tests. Verify against the corresponding stage acceptance scenario before advancing.
1.M.2. Publish static artifact and perform browser smoke. Verify against the corresponding stage acceptance scenario before advancing.
1.M.3. Update evidence and seek independent product acceptance. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 1 row and review handoff. ENG performs implementation; QA records actual evidence; ENG owns implementation delivery; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.
