# Development and operation
Date: 2026-10-05.

## Prerequisites
.NET SDK 10.0.401, Node 24.14.0 and the pinned wasm-tools inputs. See [reproducible builds](BUILD_REPRODUCIBILITY.md). The build script verifies an existing sibling ../Beep.Skia without overwriting it, or reconstructs the pinned source at an absent target.
SkiaSharp 4.150.1 is pinned by Beep.Skia. Native WebAssembly packs must match the SDK; no native runtime installation is needed on users' devices.
Seven net10 locks and 56 archive pins use the declared NuGet.Config feeds: nuget.org and two bundled owner package archives with recorded provenance. Developer machine feeds/global-cache contents are not the release contract.

## Commands
```powershell
dotnet workload install wasm-tools --skip-manifest-update
./scripts/build.ps1 -Output artifacts/candidate
node scripts/serve-static.mjs artifacts/candidate/publish/wwwroot artifacts/candidate/asset-report.json 5210
```
Choose an absent output directory. Add `-BasePath /draw/` for a subpath build and `-PackagesPath` for an explicit isolated package cache. The script runs the complete locked restore/build/test/native publish/integrity/provenance/archive gate. The selected sibling projects accept the scoped portable net10 property, and local packaging/copy targets are disabled. Do not globally override TargetFramework/TargetFrameworks across this solution. See [deployment](DEPLOYMENT.md) for cache/MIME/CSP and update operation.

Debug hot reload is outside the pinned Release graph. Its development-server package previously encountered network/DNS timeouts; no fresh Debug verification is claimed. Release excludes that development-only package. Use the verified static candidate for offline/native/CSP testing.

## Current interaction contract
Rectangle (R), ellipse (O), diamond (D), line (L), arrow (A), pen (P): drag to draw. Eraser (E): sweep over shapes; one undo restores a whole sweep. Select (V): click/drag existing shapes; Shift-click on the canvas or object list adds/removes selection. Dragging an already-selected shape moves the selection. Hand (H) or middle mouse: pan. Scroll: zoom at pointer.
Delete removes selected objects. Undo/redo uses Ctrl+Z / Ctrl+Shift+Z; Command on macOS. Arrow keys move selected objects, Shift for ten pixels. Escape cancels the current gesture.
In Select mode, style controls apply to the selected shapes and can be undone; mixed styles show no chosen swatch. In drawing modes they set the next shape's defaults. Pen strokes are bounded to 4096 samples; the editor displays a notice at the limit.
Objects list offers explicit selection alongside the canvas. Save starts a native JSON file download; Open validates files before replacing the board. New/Open asks to preserve unsaved edits.
Committed changes autosave after a 500 ms debounce. The status reports saved/pending/error; failed writes keep edits pending and offer retry. Download native JSON for a portable copy because browser storage may be cleared or evicted. Each origin/base path has one exclusive local writer. Other tabs may close/retry or edit a file-only snapshot; they cannot overwrite the writer. Unreadable stored bytes pause autosave and remain downloadable until explicit recovery replacement.
Text (T): click and type; Ctrl+Enter commits, Escape cancels, double-click edits. Bounds/endpoint handles resize. Ctrl+D duplicates, Ctrl+G/Ctrl+Shift+G groups/ungroups. Grouped select/move/erase applies to all members. Each commit is undoable.
Export image offers PNG/SVG, scale and paper/transparent background. Excessive-size exports are rejected before allocation. Shared glyph-path text makes SVG independent of installed fonts; editable text stays in native JSON.
Offline use requires a successful online release load/cache installation. Updates wait; the update button requires no pending local changes/gesture/text. Close other editor tabs before activation.

Keyboard drawing: a tool shortcut focuses the canvas. Enter starts/finishes a rectangle, ellipse, diamond, line, arrow or pen stroke; arrows move the cursor by 5 CSS pixels, Shift by 20. T then Enter opens the HTML text editor. With Select active, arrows move selection by 1 drawing unit, Shift by 10. Native inputs/buttons retain their own arrows/Enter. Numeric Width/Height in Style shares the resize action; undo restores both geometry and displayed values. Fit uses the current canvas size.

Two touches on the app canvas cancel the current single-pointer preview, then zoom/pan around the moving centroid. Lift both before drawing again. Active pen input ignores incidental touch; pressure is bounded metadata, and stroke width remains constant. This opt-in host behavior leaves default Beep.Skia consumers' secondary-pointer policy unchanged. Viewport resize cancels gestures but keeps an open text draft.

Text/help/replacement/storage dialogs contain focus, make background controls inert and restore focus on closing. Escape cancels text/help/replacement; a ready storage dialog exits to a file-only copy while preserving local bytes. At 320px tools wrap and Style collapses; inspector content scrolls. The language button changes shell labels/direction only; document content and glyph validation remain unchanged. Some diagnostics remain English pending editorial review.

## Native file
.simpledraw.json, format beep.simpledraw, envelope version 6, Beep DiagramDto schema 3.
SketchShape and three exact reviewed Flowchart families (ProcessNode, DecisionNode, StartEndNode) are accepted. Each type has ten exact property keys. Paths use invariant ordered normalized point pairs; component/port/group IDs are stable and distinct. Maximum 2 MiB and 2,000 components. V1–V4 sketch files and V5 reviewed object files migrate explicitly; all earlier envelopes still reject connectors. V6 preserves reviewed static connectors by output/input port GUID. Future versions/unregistered types/properties and unsupported connector metadata are rejected before scene mutation. The embedded/self-hosted Kalam font has explicit glyph and text limits; font notice is in THIRD-PARTY-NOTICES.md. The workflow palette is collapsed by default; family editing uses the same selection/history/export path.
No .excalidraw import compatibility is claimed.

## Verification boundaries
Core renderer tests execute real Skia native paint on the development OS. Pointer module tests use a deterministic event harness; they do not substitute for a real browser.
The master tracker records actual desktop server-stopped offline reload/edit/restore, updates, recovery and quota checks. Cross-browser, real touch-device, screen-reader, performance and formal PWA installation remain later gates.

## Local performance diagnostics
Run `node scripts/performance-fixtures.mjs artifacts/my-new-performance-fixtures` with an absent output directory. Open a verified Release preview with `?diagnostics=1`, import a 100/1,000-object rectangle or mixed fixture, select one ungrouped object, close Objects and fit the drawing. The diagnostic button executes 140 ordinary undoable keyboard edits, measures 120 after warmup and renders raw local results. Keep the tab foreground and avoid simultaneous builds. Benchmark commands change document revisions/history even though alternating movement restores final geometry. The panel is absent on normal URLs and sends no data. [Method, results and boundaries](P6_PERFORMANCE.md) explain the 100-step history capacity, 2,000-object native boundary and separate 5,000-object refusal/stress scope.

Complete P2 snapshot: `python -m http.server 5192 --bind 127.0.0.1 --directory artifacts/p2-final/wwwroot`. Final P3 source snapshot is artifacts/p3-portable-glyphs/wwwroot on port 5196. Separate origins isolate test storage; same-origin update behavior has its own verification evidence.

Connector profile: exact ConnectionLine, three routes, 2 px stroke, bundled 14 px label outlines (128 single-line characters), 4–32 arrow size, one line per directional port, no execution/schema/animation/status/ERD metadata. Capture verifies live rendering properties rather than silently losing fields absent from LineDto. Connected templates/library insertion/duplication use one manager and one atomic history batch.
