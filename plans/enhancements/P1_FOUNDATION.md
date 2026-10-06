# Stage 1: Portable foundation and working Beep.Skia canvas — tasks
Date: 2026-10-05. Status: first vertical slice implemented and verified; independent product acceptance remains open.
[SDD](P1_FOUNDATION_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: Specification and ADR 001. Original estimate: 3–5 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 1.A.1 | Audit actual Beep.Skia contracts and select static architecture | Verified | ADR 001 and global SDD source audit |
| 1.A.2 | Repair existing Blazor canvas host and pointer lifecycle | Verified | Real canvas paint/input smoke; 5 pointer-bridge tests; net8/net10 compilation |
| 1.A.3 | Add reusable SketchShape and extend framework target support | Verified | Existing scene-loader roundtrip and shape geometry/render tests; net10 model support |
| 1.B.1 | Implement session with draft/drag/cancel and existing history | Verified | Gesture/history/cancel/viewport tests and browser selection/move |
| 1.B.2 | Implement validated native envelope and file roundtrip | Verified | Strict import tests plus actual browser download/reopen and invalid version rejection |
| 1.B.3 | Build accessible initial web shell and shape controls | Verified | Named HTML tools/object list; keyboard move/undo smoke; final screenshot |
| 1.M.1 | Run build and focused behavior/render/document tests | Verified | Release solution build: 0 warnings/errors; 16 C# tests + 5 JS tests pass |
| 1.M.2 | Publish static artifact and perform browser smoke | Verified | Trimmed/native-linked release publish and browser smoke; docs/VERIFICATION.md |
| 1.M.3 | Update evidence and documentation | Verified | Evidence and documents updated; independent acceptance remains a separate task |
| 1.M.4 | Independent owner / Beep.Skia maintainer acceptance | Open | RACI review roles; no signature fabricated |

## Verification scenarios
Release solution build and native publish succeeded. Browser: draw/style three shapes at different coordinates, move, undo/redo, zoom/reset, download/reopen native file, release into the header outside the canvas, delete, reject version 42 without losing three shapes, keep drawing through unsaved-work prompt, select from object list and move/undo via keyboard.
Real pointercancel/window-blur dispatch was verified by the bridge harness and core cancel tests; real touch/device cancellation remains in P4.
See [verification evidence](../../docs/VERIFICATION.md) and [browser screenshot](../../docs/evidence/p1-browser-smoke.png).

## Closure record
Implementation: complete for the P1 vertical slice. Automated: 16 C# tests and 5 JS bridge tests pass. Release build: 0 warnings/errors. Browser: desktop in-app browser smoke passed, no application warning/error logs. Independent review/owner acceptance: open.
PWA scaffolding exists, but installed/offline behavior is not certified. No browser autosave yet.

## Next action
P2 line/arrow/freehand/text tools and undoable selected-style editing. Future stage work is planned, not implicitly complete.

