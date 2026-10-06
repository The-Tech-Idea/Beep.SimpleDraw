# Stage 4: Touch, keyboard and accessible simple workflows — SDD
Date: 2026-10-05. Status: implemented; desktop engineering verified, external/device acceptance open. See master tracker and [test matrix](../../docs/P4_TEST_MATRIX.md).
The implementation audit is [P4_ACCESSIBILITY_AUDIT.md](../../docs/P4_ACCESSIBILITY_AUDIT.md). Current authorization includes this stage; no additional scope approval is needed.
Estimate: 4–6 engineer-days. Entry dependencies: P2 and P3 functional tools/documents.

## 0. Problem statement
Keep standard HTML buttons and inputs. Add a named object list with selected state and live announcements after commits. Centralize shortcut handling outside text inputs; show a shortcut dialog. Touch gesture state separates single-pointer drawing from two-pointer viewport movement and cancels preview before promotion. Provide reduced motion, high contrast, accessible color names and tooltips. RTL shell uses logical CSS; canvas coordinates remain unchanged.

## 1. Goals and acceptance criteria
1. Operate basic editing entirely by keyboard with visible focus.
2. Use pen/touch with capture, palm/secondary-pointer policy and pinch zoom.
3. Keep UI usable at 320px and 200% zoom; verify target browsers.

## 2. Design
Keep standard HTML buttons and inputs. Add a named object list with selected state and live announcements after commits. Centralize shortcut handling outside text inputs; show a shortcut dialog. Touch gesture state separates single-pointer drawing from two-pointer viewport movement and cancels preview before promotion. Provide reduced motion, high contrast, accessible color names and tooltips. RTL shell uses logical CSS; canvas coordinates remain unchanged.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Implemented contracts below extend the audited existing methods in the global SDD.

## 3. API surface
Extend host pointer contract with pointer ID/type and bounded pressure samples when needed. The app.js shortcut boundary and DrawingSession viewport methods share the existing state. No server API.
Implement viewport-size/pinch messages on the host's opt-in gesture surface. Keyboard Enter/arrow gestures call the same PointerDown/Move/Up operations; numeric resizing shares existing resize snapshots. Central modal boundaries contain focus and suppress editor shortcuts while native controls are active. Logical layout and bilingual resources affect the shell only; they do not mirror canvas geometry or silently change document glyph support.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| Modify | ../Beep.Skia/Beep.Skia.Blazor.Controls/wwwroot/skiaHost.js | ~100 | high |
| Modify | src/Beep.SimpleDraw.Core/DrawingSession.cs | ~100 | high |
| Modify | src/Beep.SimpleDraw.Web/Pages/Home.razor | ~180 | medium |
| Modify | src/Beep.SimpleDraw.Web/wwwroot/css/app.css | ~160 | medium |

## 5. Error handling matrix
Lost focus cancels gesture; unsupported pressure uses normal stroke explicitly. Shortcuts never intercept native typing. Toolbar overflow remains reachable via labeled controls.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Keyboard-only add/select/move/delete/save. Screen-reader names/selection/status. Actual Android touch and iOS Safari; resize/orientation changes during gesture; high-DPI and browser zoom. Document tested versions and remaining gaps.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P4_USABILITY.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Canvas has no DOM semantic tree: object list provides an alternative but requires screen-reader review. Browser touch behavior varies: real device gates remain separate from emulation.

## 9. Out of scope
Claims of formal WCAG certification and automatic translation without reviewed resources.

## 10. Sub-task execution order
4.A.1. Implement central shortcuts and help dialog. Verify against the corresponding stage acceptance scenario before advancing.
4.A.2. Implement keyboard object list and selection announcements. Verify against the corresponding stage acceptance scenario before advancing.
4.B.1. Implement touch/pen gesture policy and pinch zoom. Verify against the corresponding stage acceptance scenario before advancing.
4.B.2. Refine responsive inspector, focus, contrast and RTL resources. Verify against the corresponding stage acceptance scenario before advancing.
4.M.1. Complete browser/device/accessibility matrix. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 4 row and review handoff. ENG performs implementation; QA records actual evidence; PO owns product/scope acceptance; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.

## Rendered contrast correction
Later actual P6-runtime inspection identified low-contrast numeric/select borders, selected-width state, canvas keyboard focus and small object-count text. Five existing CSS rules change in place; the canvas uses the host's existing -4 px inward offset. [Rendered contrast evidence](../../docs/P4_ACCESSIBILITY_AUDIT.md) records unrounded ratio checks, preserved exact native bytes after style/undo/reopen, 320px targets and the 188 C# / 70 JS native release. No scene/history/coordinate/host contract changes. Independent screen-reader/device/actual zoom acceptance remains open.


### Docked workspace and scroll navigation follow-through

The user selected a docked UI: tools/style on the left, canvas in the middle, Beep.Skia catalog/properties on the right. Desktop rails occupy grid tracks and do not cover the drawing; narrow screens stack the rails around the canvas. Text editing anchors within the center canvas, retaining existing inert/focus/modal boundaries. Ordinary wheel input pans; Shift+vertical wheel pans horizontally; Ctrl/Cmd+wheel zooms around the pointer. The shared host carries finite CSS-pixel horizontal/vertical deltas and normalizes line/page units; DrawingSession owns viewport changes without changing native geometry/history. Existing host default-interaction behavior retains its existing zoom policy. The four remaining shell labels use existing resources. Meaningful viewport/bridge checks and the published responsive/scroll/modal verification are recorded in docs/DEFAULT_SKIA_SHELL_AND_BOUNDS.md. Shared WellLogs brushes are clipped to their supplied rectangle and restore caller canvas state, addressing the actual gallery ink overflow; all eleven brush containment tests pass after eight failed before correction. These local corrections do not imply independent editorial/device acceptance. Verification and adoption are in progress until the final release record below.
