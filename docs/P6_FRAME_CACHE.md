# P6 automatic scene frame cache

## Decision and scope

The portable host uses `SceneFrameCache` in the existing Beep.Skia renderer. The document, components, history, exports and draw commands retain their existing authority. The cache owns two disposable full-size raster buffers, component visual keys, previous ink footprints and paint-order references. At 1280 × 720 with four-byte pixels, its two buffers require 7,372,800 bytes, excluding the target surface and component resources. Size changes replace both buffers; host disposal releases them.

The renderer updates every component through its original update path. It draws contributors into a full-size scratch canvas with the original device matrix and full clip, then copies damaged pixels into the retained frame. It writes the complete retained frame to the target, including when that target is a new allocation. Reduced raster clips were rejected by earlier exact-pixel trials and are not used here.

## Damage and invalidation

For sketch leaves, visual keys include kind, position, dimensions, stroke/fill/width, opacity, roughness/seed, path, text/font size, renderability and static status. Changed leaves contribute both old and new actual ink footprints. Device bounds include a two-pixel raster guard. Unchanged leaves retain their known footprints. A contributor intersects the combined damage region; paint order is preserved. No component visibility is changed to achieve omission.

Membership or paint-order changes require full repaint. Manager replacement, viewport zoom/pan, device matrix, target size/format, background, grid visibility/spacing/color and reset also require full repaint. Unknown effects use the ordinary renderer and invalidate the retained frame: connected lines, non-sketch families, children, static content, manager overlays/selection handles/gestures, unknown overlay bounds, unsupported clipping/matrices or non-readable raster targets. Exceptions in an overlay or its footprint callback invalidate the cache before propagating.

Home supplies CSS-space footprints for draft ink, eraser previews, marquee, selection handles and keyboard cursor. Old and current overlay bounds are always damaged, including when their contents change inside an unchanged rectangle. Degenerate marquee/selection bounds are inflated before testing emptiness, so a horizontal or vertical handle remains covered. Unknown draft ink requires full repaint.

## Measurements

Optional scene profiling records actual draw attempts and skipped known noncontributors separately. The sum must equal the number of scene components. The existing CPU scene measurement still includes clear and scene preparation/drawing; overlay and retained-frame copying follow that scope. Presentation timing continues to measure executed shared-history edits through actual paints and animation-frame presentation. Neither CPU duration nor skipped counts substitute for the unchanged 50 ms ordinary mixed p95 gate.

## Verification so far

`SceneFrameCacheTests` adds 27 executable tests. Twenty-one combinations cover all seven sketch kinds at zoom .41/.875/1.75 with incoming DPI 1.25, fractional positions, translucent overlapping ellipses and ten visual mutations per combination. Every resulting frame must match the ordinary renderer byte for byte. Separate tests cover membership/order/undo/redo/native replacement, viewport/grid/background/size/device matrix invalidation, unchanged-footprint overlay color changes/movement/removal, unknown content/clips, failed callbacks and actual contributor/skip profiling.

The canonical C# suite passes 241 tests with warnings treated as errors, including extended manager-replacement, partial grid repaint and connected-family controls. The reviewed source pin covers 210 inputs, SHA-256 `4f280f9375360e373833f1a97ccb98cf5159f484b767444dba20610c200d7993`. The fresh native portable release passes 241 C# + 72 JS checks, zero build warnings/errors, 56 archive/1,106 extracted-payload audits, 134 offline assets and all 402 ZIP payload comparisons. The original sibling test graph passes 504; the net8 host build has zero warnings/errors. Physical devices, screen readers, independent acceptance, intended public host/provider/cold-process startup and hosted CI remain open.

## Native release and browser evidence

Candidate: `artifacts/p6-frame-cache-release`; [normal preview](http://127.0.0.1:5242/draw/) while its loopback server runs. Offline version `99a7f9824c40819e`; inventory identity 29,935,561 bytes, Brotli 8,152,461 bytes, gzip 10,770,663 bytes. ZIP SHA-256 `70ca8c9dd142a252f55db070e9c77af0b9bbf7f9b346b1a531234c8a4716e776`. Brotli grew 5,607 bytes versus the prior lazy-ink foundation, remaining under the 25 MiB inventory limit.

Actual normal and server-stopped reload/edit/undo/native downloads pass. Seven-tool files remain exactly 5,105 bytes, SHA-256 `02743b965d9c18a3f15fdc59031c6b1af5d71434faf8b1bfb936d73f493a5ebd`. The mixed 1,000-object file remains exactly 714,792 bytes, SHA-256 `6f23948d6a7a4d386962f1c44a5781b24da4d1a1be790d93423e7190f7a59343`. Normal mode has no diagnostic panel; browser warning/error capture is empty. Keyboard horizontal draft/cursor creation, shape commit (8 objects), undo (7) and restored selection were inspected in the browser without residual draft ink.

| Ordinary mixed run | Median ms | p95 ms | Maximum ms | 50 ms p95 |
|---|---:|---:|---:|---|
| 1 | 29.3 | 30.6 | 48.1 | Pass |
| 2 | 29.3 | 30.4 | 30.9 | Pass |

Both executed 140 edits, 140 dispatches and 140 completed paints, with 20 warmup/120 post-warmup samples, profiling off, bounded history at 100 and restored coordinates. The desktop viewport was 1280×720, DPR 1, visible Chromium 154 on this Windows host. The [raw repeated report](evidence/p6-frame-cache-ordinary-mixed-repeat.json) retains every sample. These runs close the ordinary mixed desktop gate; they do not certify other devices or isolate a controlled causal speedup from historical candidates.

The scheduling check preserves geometry/revision/history and records exactly two dispatches/paints for six combined requests. The optional [CPU probe](evidence/p6-frame-cache-profile.json) records 32 actual contributors and 968 known skips in each of 120 frames; CPU clear/scene median is 6.5 ms and p95 8 ms, with overlay/copy outside that scope. Its presentation p95 is 30.8 ms; ordinary acceptance uses the unprofiled runs above.

The [capture checker](../tests/verify-frame-cache-evidence.mjs) validates retained TRX counters, release metadata, exact native bytes, UI states, profiling, scheduling and raw timing percentiles, producing [the summary](evidence/p6-frame-cache-summary.json). It checks evidence; it does not simulate tests or browser execution.

## Required release gate

The pinned native artifact, canonical C#/JS checks, ordinary sibling test graph/net8 host, normal/offline workflow and repeated ordinary mixed benchmark are verified for this desktop host. Continue the named external engine/device/high-DPI/accessibility matrix, intended hosting/provider/cold-process startup, hosted CI, ownership/header/guide review and independent acceptance. Native limits and the ordinary 50 ms protocol remain unchanged.

## Production session overlay follow-up — 2026-10-06

The subsequent [production session overlay release](P6_SESSION_OVERLAYS.md) moves the existing Home overlay callbacks into DrawingSession and verifies 48 additional cases / 528 exact frames, fresh 289 C# + 72 JS native release, actual responsive/normal/offline/native/scheduling checks and ordinary mixed p95 30.5/30.2 ms. The Beep.Skia source pin remains unchanged; this document and its earlier raw captures retain their historical 241-test / port-5242 scope.
