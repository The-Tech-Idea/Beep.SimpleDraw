# Default catalog shell and rendering follow-through

## Interface labels

P4's Arabic shell still exposed the device badge and Delete/Undo/Redo tooltips in English. These four Home labels now use the existing UiText resource path. Keyboard chords retain their invariant technical spelling; drawing content and serialized geometry are unchanged. No new localization subsystem or duplicate resources are introduced.

Verify the actual published English and Arabic badge/title values, preserve unfinished edits where applicable, and leave the user's prior language selected after checking. Record the release gate and visible runtime evidence below. This is a focused translation correction, not independent Arabic editorial acceptance.

## Well-log pattern bounds

The all-family gallery's WellLogs screenshot shows lithology diagonal hatching outside its column. WellLogBrushLibrary.DrawPattern draws diagonal lines from bounds.Left minus bounds.Height through the right edge, without a matching clip. DrawLithologyTrack passes its interval rectangle directly. This is a shared renderer defect: pattern strokes can cover adjacent tracks and extend outside export bounds.

The correction must clip the existing brush implementation to the supplied rectangle, preserve any caller clip/transform and restore canvas state after every pattern, including the solid early-return path. It must retain all brush colors, pattern definitions, document/port identity and layout. Verify actual transparent pixels outside the rectangle for every built-in brush, patterned ink inside, and caller drawing outside after the brush returns. Then repeat the published WellLogs fixture and native roundtrip.

## Ownership and scope

ENG implements and records local tests/build/browser evidence; SK is accountable for shared rendering contracts, PO for product scope, QA consulted on independent editorial/device checks, and OPS informed of the updated portable package. These actions are within the user's existing authorization to update Beep.Skia. Independent device/accessibility/host acceptance remains open.


## Docked layout and canvas scrolling

The user chose the docked workspace on 2026-10-06. Existing controls are reorganized into a left tool/style rail, center canvas, right catalog/property/export rail, and bottom history/viewport/status bar. Grid tracks keep the desktop rails outside the drawing viewport; narrower screens stack the same controls around a real center drawing area. The text editor lives inside the canvas layer so its existing world-to-screen anchor has the correct local origin. Only the canvas input wrapper is inert during text editing; the editor retains the existing modal focus boundaries.

The shared host now carries DeltaX alongside the existing vertical Delta. Wheel line units use 16 CSS pixels; page units use the measured host width/height. Ctrl and Cmd map to the existing control modifier. Default SimpleDraw wheel input pans both axes, Shift maps vertical wheel motion to horizontal pan, and modified wheel input performs anchored zoom. DrawingSession owns this policy, preserving the existing scene/history/native contracts; the Beep host's standalone default interaction retains its own existing wheel behavior. Finite/zero input guards avoid unintended zoom or corrupt transforms.

Six viewport cases verify vertical/horizontal/combined/Shift panning, unchanged native state and pointer-anchored modified zoom. Two new bridge cases cover horizontal trackpad/Cmd and line/page unit normalization. All eleven brush cases pass after eight failed before correction; the pixel test also verifies caller clip/transform/save-count restoration and subsequent drawing outside the brush rectangle. The full gate runs 585 C# and 79 JS checks. Published verification/adoption is recorded below when complete; the intermediate docked build was stopped by its offline-integrity guard while source was still changing and was never adopted.


The clipping fix uses Save/ClipRect/finally Restore in the existing DrawPattern method, including its solid-pattern early return. No brush definition, geometry schema or native property changes are introduced. Source lock now fingerprints 498 inputs as `e9f5faabe9dac9aee4d8fa38def10d5fddd98a74b3b0bf47e2ef2cef78618c9f`. Final release source is frozen during publication; earlier immutable artifacts are retained.


## Final docked release verification - 2026-10-06

Adopted candidate: `artifacts/default-skia-docked-workspace-final-v2`, served at http://127.0.0.1:5243/draw/. Full build: 585 C# / 79 JS checks; ordinary sibling regression: 504 passed. Source pin verification confirms 498 inputs with SHA e9f5faabe9dac9aee4d8fa38def10d5fddd98a74b3b0bf47e2ef2cef78618c9f. Offline version 98e58bfb9658a184, 151 offline assets, 117 runtime payloads, 56 pinned package archives, and all 453 ZIP entries verified. Compressed inventory 8,457,056 bytes; ZIP SHA256 8672b725a2511616c83d2dc5d4f7783c1535eaf80d2eb4f2bda1ca21975b142e.

Browser evidence: final 785px tool rail client/scroll widths both 188px; ordinary canvas wheel moves the shape out of view while zoom remains 100%, then Undo/Redo retains the single insertion history. At 320px page widths are 320/320, the SimpleDraw brand is visible, and the text editor stays inside the canvas with a 12px bottom margin. A draft survives resizing to 1280px, and Cancel restores focus to the canvas. Arabic device badge and Delete/Undo/Redo titles are translated. The final default WellLogCanvas renders lithology hatching inside its column. Captured warning/error logs are empty. See `docs/evidence/docked-final-responsive-modal.json`, `docked-final-scroll-before.png`, `docked-final-scroll-after.png`, `docked-final-welllogs.png`, and the final release log. The preceding 320px all-family audit covers 249 catalog entries per language across all sixteen families; the final CSS fix adjusts the tool rail, brand visibility and dialog height only.

These are local engineering and in-app-browser checks. Physical device, independent accessibility/editorial, other browser engines and production-host acceptance remain open. This release's native-file download completion was not established in the browser; prior C7 byte-exact native roundtrip evidence remains historical. No new mixed-1000-object performance measurement is claimed for this layout. Intermediate artifacts are retained as historical candidates; only final-v2 is adopted.

Original preview adoption is confirmed: Update app activated final-v2, restored the existing ten objects, and Fit drawing displayed them in the center canvas. Captured warning/error logs are empty. `docs/evidence/docked-final-user-preview.png` records the user-facing result; temporary test tabs and servers were closed, viewport override reset, and the 5243 preview remains running.
