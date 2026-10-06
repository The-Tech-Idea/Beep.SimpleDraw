# P6 session overlay verification

## Coverage gap and implementation

The automatic cache had exact tests for generic overlay callbacks, while the real app's draft/selection/eraser/marquee/cursor painting and footprints were private page methods. Those existing methods now live in the existing `DrawingSession`: `DrawOverlay(SKCanvas)` and `GetOverlayInkBounds()`. Home delegates its existing callbacks to them and retains its paint-measurement/scheduling notifications. Paint commands, colors, camera coordinates, history and document authority remain the same.

The session reports CSS-space bounds before the incoming device transform. Drafts use actual sketch ink; selection and degenerate marquee rectangles are inflated before emptiness testing. Known old/current bounds drive the existing frame cache. Unknown draft content still requests ordinary full repaint. This change makes the production overlay code directly executable in native raster tests, without a copied test renderer or another scene model.

## Exact native verification

`DrawingOverlayTests` adds 48 cases: six pointer tools × three zooms × two device transforms, plus six selection/marquee/eraser/endpoint cases and six keyboard/resize cases. The executed test output records **528 exact frames**. Every cached frame must match the ordinary renderer byte for byte. Every actual overlay alpha pixel must also lie inside the reported footprint transformed into device coordinates with the renderer's two-pixel guard. Rendering must preserve all component references/geometry/visibility and undo counts.

Zooms: .41/.875/1.75. Device transforms: 1.25/2. Fractional coordinates, translucent overlapping components, thick rough drafts, same-footprint style changes, horizontal/degenerate drafts and marquees, multiselection, line/arrow endpoint handles, erase previews, cancel/commit/undo/redo, keyboard cursor movement and viewport resize are covered. Fresh target allocations are used for every frame. CSS sizes include 640×480 and 320×360. These are native renderer/device-transform checks, not physical monitor/browser-zoom certification.

The fresh native release gate passes **289 C# + 72 JS**, with warnings treated as errors: [TRX](evidence/p6-session-overlays-release.trx), [build](evidence/p6-session-overlays-build.log), [publish](evidence/p6-session-overlays-publish.log). The Beep.Skia source remains the previously verified `4f280f93…` pin (210 inputs); the prior ordinary 504-test sibling and net8 host evidence applies to those unchanged source inputs. Those sibling checks were not rerun for this app-only change. Retained [test source](evidence/p6-session-overlays-tests.cs) and [session source](evidence/p6-session-overlays-session.cs) identify the executed implementation.

## Responsive audit before moving the callbacks

On the browser-verified `artifacts/p6-frame-cache-release` candidate (port 5242), all eleven controls remain 44×44 at 320×720 with document width 320. Numeric ellipse resizing 200×90 → 240×108 and undo restore the original inputs. A fitted-scene drag of 10×5 CSS pixels produced world delta 23.89706×11.94852, agreeing with the fit zoom .418461538 within .0001; every other document field stayed exact. Restoring the viewport and saving produced the original 5,105-byte baseline exactly.

At 640×360 the help dialog is 370×260, client height 258/scroll height 500, without document overflow; Escape returns focus to the help button. Viewport resize cancels the pending session gesture in native tests. The embedded browser exposes only viewport width/height and has no device-scale/browser-zoom control; Ctrl+plus did not change DPR or canvas dimensions. Available browser surfaces are Chromium IAB and the app panel. Actual high-DPI, 200% browser zoom, other engines, physical touch and screen readers remain open.

## Release gate

Candidate: `artifacts/p6-session-overlays-release`; [current preview](http://127.0.0.1:5243/draw/) while its loopback server runs. Native publish has zero managed build/publish warnings/errors. Package checks cover 56 archives/1,106 extracted files, 134 offline assets and 402 exact ZIP entries. Offline version `00ef50ab8e91f107`; inventory identity 29,935,561 bytes, Brotli 8,152,076 bytes, gzip 10,770,530 bytes. ZIP SHA-256 `5705ebef8f165e0e3ad5236322458623d0ee2bdc69eab9119688c74fce78ed7b`.

Actual normal and stopped-server reload restore seven objects. Selected stroke width 2 → 4 → Undo 2 and native downloads match the original 5,105 bytes exactly. Responsive resize 200×90 → 240×108 → Undo 200×90 passes. All eleven tools stay 44×44 without 320px overflow; the 10×5 CSS drag reproduces the exact expected world delta within .0001 and changes no other document fields. This tolerance applies only to serialized float coordinate arithmetic; raster comparisons remain byte-exact. At 640×720, resizing with an active draft clears its preview without residual ink. At 640×360, help scrolls and Escape returns focus. [Narrow layout](evidence/p6-session-overlays-320-layout.json), [drag drawing](evidence/p6-session-overlays-320-moved.simpledraw.json), [cleared draft](evidence/p6-session-overlays-640-draft-cancel.png), [short-height help](evidence/p6-session-overlays-640-help.png), [stopped listener](evidence/p6-session-overlays-server-stopped.json), [offline drawing](evidence/p6-session-overlays-offline.simpledraw.json) and [final preview](evidence/p6-session-overlays-final-preview.png) retain the actual observations. Console warning/error capture is empty.

The unchanged ordinary 1,000-object mixed benchmark passes twice with profiling off:

| Run | Median ms | p95 ms | Max ms | 50 ms p95 |
|---|---:|---:|---:|---|
| 1 | 29.5 | 30.5 | 40.0 | Pass |
| 2 | 29.3 | 30.2 | 30.5 | Pass |

Each run completes 140 edits/dispatches/paints, 20 warmup and 120 measured samples, restores selected geometry and retains the bounded 100 undo entries. The scheduling check completes two dispatches/paints for six requests without changing geometry/history. The mixed native download matches its original 714,792 bytes exactly. These observations do not establish a causal speed change from moving the callbacks. [Raw runs](evidence/p6-session-overlays-ordinary-mixed-repeat.json), [scheduling](evidence/p6-session-overlays-scheduling.json), [native drawing](evidence/p6-session-overlays-mixed.simpledraw.json), [provenance](evidence/p6-session-overlays-provenance.json) and [asset inventory](evidence/p6-session-overlays-asset-report.json) are retained. The [observation checker](../tests/verify-session-overlays-evidence.mjs) produces the [verified summary](evidence/p6-session-overlays-summary.json); it validates captures rather than executing browser interactions.

The full P1–P6 goal remains active. Physical touch, screen readers, real high-DPI/200% browser zoom, other engines, intended host/provider/cold-process startup, hosted CI, installation, ownership/header/guide review and independent acceptance remain open. Native device transforms and embedded Chromium viewport tests do not close those gates. Source pin, native 2 MiB/2,000-object limits and 50 ms benchmark protocol remain unchanged.
