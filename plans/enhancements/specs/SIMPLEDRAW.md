# Specification: Beep.SimpleDraw
Date: 2026-10-05. Status: implementation authorized by the user.
Owner: The Tech Idea. Implementation authorization: "then start implementation" and permission to update Beep.Skia.

## Problem
Provide an easy, portable browser whiteboard for sketches, diagrams, and explanations, with the owner's Beep.Skia as the default embedded drawing library.

## Product outcomes
1. Open a blank board and draw without signing in, installing a server, or selecting a diagram family.
2. Draw, select, move, style, undo, and export from desktop and touch browsers.
3. Keep editable documents on the user's device, with explicit save failures and recoverable files.
4. Publish one static web folder that runs on Windows, macOS, Linux, Android, and iOS browsers; install/offline operation after caching over HTTPS.
5. Reuse Beep.Skia's active component, scene, selection, history, serialization, and rendering contracts.

## Scope and priorities
P0: standalone Blazor WebAssembly; working Skia browser host; rectangle/ellipse/diamond; selection/move; undo/redo; pan/zoom; native JSON round trip.
P1: line/arrow/freehand/text/eraser; style inspector; grouping/multiselection; local autosave; PNG/SVG; production PWA; responsive and keyboard editing.
P2: the default embedded Beep.Skia catalog (249 standalone shapes across sixteen families), templates, and an optional personal local library. The catalog is open by default with family/search controls; personal saved entries remain in their separate collapsed panel.
Later: explicitly optional sharing/collaboration service. No account or service needed for local editing.

## Reference and product boundary
The [Excalidraw repository](https://github.com/excalidraw/excalidraw), reviewed 2026-10-05, lists a canvas whiteboard, sketch style, drawing tools, history, pan/zoom, image support, export, and local-first/offline operation. These inform workflows, not source copying or a pixel-identical clone.
SimpleDraw native files use Beep DiagramDto in a versioned application envelope. No claim of .excalidraw compatibility. Interoperability requires a separate importer with a documented loss report.

## Constraints and targets
- Existing .NET 10 SDK; SkiaSharp pinned to Beep.Skia's 4.150.1 for coherent native/managed assets.
- No runtime dependency on a sibling source folder after publish.
- No database, Docker, authentication, telemetry, external font CDN, or backend in the MVP.
- Target devices: last two stable Chrome/Edge/Firefox desktop versions and Safari on macOS/iOS; record real versions at verification.
- Proposed budgets: cold compressed download <=25 MiB, first usable board <=5s on desktop broadband, p95 edit-to-paint <=50ms on a 1,000-shape board. Targets, not measured claims.
- Core native import: <=2 MiB, <=2,000 components, finite geometry, bounded properties, allowlisted types only.
- Per-phase effort is a planning range in engineer-days, not a delivery promise.

## Success scenarios
Create three shapes -> move one -> undo/redo -> save file -> reopen with identical geometry/styles.
Draw with a pointer leaving the canvas -> release outside -> no stuck gesture.
Reload offline after production installation -> saved board reappears -> native export remains usable.
Invalid/future-version document -> useful error -> current board remains unchanged.
Use only keyboard -> add/select/move/delete and reach every toolbar action.

## Decisions and approval
The user has authorized planning and an implementation start. Defaults adopted: local-first standalone WASM, direct Beep.Skia integration, native Beep JSON, optional later collaboration. Product acceptance and future service operation remain the owner's role; do not mark stakeholder sign-off from build success.
