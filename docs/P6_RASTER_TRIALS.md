# P6 native raster feasibility trials

Date: 2026-10-06. Task [6.B.1](../plans/enhancements/P6_RELEASE.md), [SDD](../plans/enhancements/P6_RELEASE_DESIGN.md), [tracker](../plans/enhancements/MASTER_TRACKER.md).

Status: both executable prototypes rejected by exact pixel checks. Neither was adopted. The published phase-profile candidate and production source remain unchanged. The ordinary 1,000-object 50 ms p95 gate remains open.

## Alpha-only glyph masks

The [phase profile](P6_SCENE_PHASE_PROFILE.md) identified 143 text draw attempts as the largest measured CPU group, median 21.4 ms in both actual browser runs. A native prototype rasterized each original glyph path into a device-space Alpha8 image, then painted that mask with the original color and nearest sampling. It retained fractional translation, viewport clipping, bundled Kalam, linear metrics and original glyph positions.

All six cases compiled and executed but failed byte equality against independent original glyph-path commands. Maximum channel difference was 2 in every case:

| Zoom | Opaque coral changed channels | Translucent slate changed channels |
|---|---:|---:|
| 0.41 | 383 | 619 |
| 0.875 | 1,001 | 1,412 |
| 1.75 | 2,052 | 2,970 |

[Prototype source](evidence/p6-text-mask-rejected.cs), [actual failures](evidence/p6-text-mask-rejected.log), [six-case TRX](evidence/p6-text-mask-rejected.trx). Source SHA256: `3e61c1f53dc1d97d2d7a71e728a1f079065ae2bd4330f6731894c0e771f8bd78`.

This full-viewport-per-glyph experiment tested feasibility; it never became a bounded production cache. These results reject this particular composition approach rather than proving that every glyph cache is impossible.

## Retained pixels with a smaller canvas clip

The exact package's [SKCanvasView source](https://raw.githubusercontent.com/mono/SkiaSharp/c3e4f4c20e1f23ab74d31a8838a5bd6dc55365f2/source/SkiaSharp.Views/SkiaSharp.Views.Blazor/SKCanvasView.razor.cs) retains its CPU pixel allocation until size changes and creates a surface over that allocation for each paint. This suggested testing clipped repainting before introducing damage tracking into the host.

The native prototype recreated each surface over a retained 500×400 bitmap, clipped to the deliberately fixed device rectangle (15,15)–(350,320), cleared inside that clip, then called the existing `DrawingManager.Draw`. Expected output cleared and repainted the whole frame. Both used fractional pan (12.375,7.125), the same manager and original commands. A changed multiline text object overlapped a translucent ellipse and 80 unchanged text objects. The eight intended edit steps covered movement, font size, alpha stroke, text layout, opacity, empty text and restoration. No production damage detector was implemented.

Twelve cases crossed three zooms, grid on/off and ellipse opacity 0.7/1.0. **Two passed, ten failed.** Both passing cases were zoom 0.41 with grid disabled, one for each ellipse opacity; they completed all eight steps. Each failing case stopped at its first exact assertion, step 0, so later edits are unverified for those cases.

| Zoom | Grid | Ellipse opacity | First-step changed channels | Maximum channel difference |
|---|---|---:|---:|---:|
| 0.41 | Off | 0.7 / 1.0 | 0 / 0 | 0 / 0 |
| 0.41 | On | 0.7 / 1.0 | 2,832 / 2,832 | 7 / 7 |
| 0.875 | Off | 0.7 / 1.0 | 272 / 272 | 15 / 15 |
| 0.875 | On | 0.7 / 1.0 | 1,502 / 1,502 | 15 / 15 |
| 1.75 | Off | 0.7 / 1.0 | 587 / 782 | 18 / 25 |
| 1.75 | On | 0.7 / 1.0 | 2,822 / 3,023 | 18 / 25 |

[Final controlled source](evidence/p6-retained-clip-rejected.cs), [actual output](evidence/p6-retained-clip-rejected.log), [twelve-case TRX](evidence/p6-retained-clip-rejected.trx). Source SHA256: `be17b33f96b219e683ef077231e6b73afd12cb8bb4a693984d64386c261b4ca4`.

Disabling ellipse opacity isolation did not eliminate mismatches. This rules out attributing all failures to that layer. Current grid extents depend on `DeviceClipBounds`, making a changed clip an additional behavior change. The evidence records differing pixel bounds; it does not establish the internal Skia cause of every nongrid mismatch. No pixel tolerance was introduced.

## Canonical restoration and next gate

Both temporary test sources were moved outside the compiled test project into their artifact directories. Rebuilding and running the canonical portable suite passes **191 tests, zero failed/skipped**, with warnings treated as errors: [TRX](evidence/p6-raster-trials-restored.trx), [build/test log](evidence/p6-raster-trials-restored.log). The reviewed 209-file sibling source pin still verifies as `5c0edb87a753355d42eb85bdb09580355bb8a09dec3e8756a7c6cde7455fd076`.

The [retained evidence checker](../tests/verify-raster-trial-evidence.mjs) validates actual counters, failure samples and source hashes and produces [the machine-readable summary](evidence/p6-raster-trials-summary.json). It does not rerun native tests or simulate browser execution. The earlier published 72 JavaScript/504 sibling/browser results remain historical evidence for the unchanged candidate; no new publish or browser performance run was warranted for rejected prototypes.

Next hypothesis: preserve the full canvas clip and original glyph commands while reducing replay to contributors to an explicitly tracked changed region, then copy only that region into retained pixels. This requires proven ink bounds, dependencies, overlays and full-repaint invalidation for viewport/DPI/size/background and unknown component effects. It has not been implemented or measured. First require exact full-frame comparisons, then run the unchanged ordinary browser driver. The full P1–P6 goal remains active; external acceptance gates remain open.

## Subsequent verified foundation

The later [ink-bounds foundation](P6_INK_BOUNDS.md) proves the full-canvas subset-contributor/regional-byte-copy hypothesis over 96 exact native edit frames and adopts a conservative shared footprint API with 23 raster checks. Its current source differs from the historical rejected-trial snapshot above. The editor still uses full repainting; automatic damage tracking and the 50 ms performance gate remain open. The rejected mask/clip prototypes remain unadopted.
