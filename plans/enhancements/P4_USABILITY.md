# Stage 4: Touch, keyboard and accessible simple workflows — tasks
[SDD](P4_USABILITY_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: P2 and P3 functional tools/documents. Estimate: 4–6 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 4.A.1 | Implement central shortcuts and help dialog | Automated + desktop browser verified | Enter/arrow creation; input/composition exclusions; text/help/replace Tab wrap, Escape/return focus |
| 4.A.2 | Implement keyboard object list and selection announcements | Automated + desktop browser verified | Object-list Tab/Enter selection; pressed names; live counts; keyboard move/delete/undo/save |
| 4.B.1 | Implement touch/pen gesture policy and pinch zoom | Automated verified; hardware gate open | Ordered touch promotion/cancel/centroid/scale/lift/palm/observer checks; physical Android/iOS not run |
| 4.B.2 | Refine responsive inspector, focus, contrast and RTL resources | Implemented; desktop workflows and targeted rendered contrast verified | 320px/640px reflow; 44px mobile tools/swatches; numeric resize/undo; Arabic shell; later actual focus/input/select/width/count contrast meets targeted ratios; editorial/zoom/device gates open |
| 4.M.1 | Complete browser/device/accessibility matrix | Partial | [Matrix](../../docs/P4_TEST_MATRIX.md) records actual scope and every open external/device gate |

## Verification scenarios
Keyboard-only add/select/move/delete/save. Screen-reader names/selection/status. Actual Android touch and iOS Safari; resize/orientation changes during gesture; high-DPI and browser zoom. Document tested versions and remaining gaps.

## Closure record
Implementation: delivered. Automated: 67 C# and 24 JavaScript checks; Release WASM publish and retained net8 host build passed. Actual desktop keyboard/modal/resize/narrow/RTL evidence is in the [matrix](../../docs/P4_TEST_MATRIX.md). Independent review/owner acceptance and physical-device/screen-reader/actual zoom matrix remain open.

## Next action
Continue P5 local libraries and P6 release engineering. Keep 4.M.1 and the matrix external gates open until actual evidence is supplied; no fabricated acceptance.


### Docked workspace and scroll navigation follow-through

The user selected a docked UI: tools/style on the left, canvas in the middle, Beep.Skia catalog/properties on the right. Desktop rails occupy grid tracks and do not cover the drawing; narrow screens stack the rails around the canvas. Text editing anchors within the center canvas, retaining existing inert/focus/modal boundaries. Ordinary wheel input pans; Shift+vertical wheel pans horizontally; Ctrl/Cmd+wheel zooms around the pointer. The shared host carries finite CSS-pixel horizontal/vertical deltas and normalizes line/page units; DrawingSession owns viewport changes without changing native geometry/history. Existing host default-interaction behavior retains its existing zoom policy. The four remaining shell labels use existing resources. Meaningful viewport/bridge checks and the published responsive/scroll/modal verification are recorded in docs/DEFAULT_SKIA_SHELL_AND_BOUNDS.md. Shared WellLogs brushes are clipped to their supplied rectangle and restore caller canvas state, addressing the actual gallery ink overflow; all eleven brush containment tests pass after eight failed before correction. These local corrections do not imply independent editorial/device acceptance. Verification and adoption are in progress until the final release record below.
