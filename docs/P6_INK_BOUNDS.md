# P6 ink bounds and full-canvas contributor foundation

Date: 2026-10-06. Task [6.B.1](../plans/enhancements/P6_RELEASE.md), [stage SDD](../plans/enhancements/P6_RELEASE_DESIGN.md), [master tracker](../plans/enhancements/MASTER_TRACKER.md).

Status: the shared ink-bounds contract, automated/native release and focused desktop normal/offline checks are verified. Full-canvas contributor repainting passes a native prototype. Automatic damage repainting is **not enabled in the editor**. Current ordinary mixed p95 remains over 50 ms; the full plan remains active.

## Adopted shared contract

`SketchShape.TryGetInkBounds` reports conservative world-space drawing bounds for a leaf. Existing cached sketch paths supply geometry, including roughness and arrowheads; round strokes expand those bounds by half the stroke width. Text bounds come from the actual glyph outline bounds and their original positions. Empty/invisible/inactive/disposal-state leaves return empty ink. Children make the footprint unknown and return false rather than pretending the parent box contains them. The caller transforms the bounds and adds at least two device pixels for antialias coverage.

The existing typeface helper retains the original per-glyph Save/Translate/DrawPath/Restore commands. An overload optionally collects outline bounds while issuing those same commands. Text bounds are computed only when requested; normal text recording does not read each glyph's bounds. A first bounds query can rerecord the existing picture to collect its footprint, then subsequent unchanged queries reuse it. Existing text/geometry invalidation and component-owned disposal remain authoritative. This API does not track scene damage, connections, static/world annotations or host overlays.

Twenty-one raster cases cross all seven sketch kinds with scales 0.41, 0.875 and 1.75. Each exercises eight mutation steps, fractional pan/positions, 20-pixel strokes, roughness, undersized declared bounds, multiline/fractional text size, alpha, opacity and reversed paths. Every painted alpha pixel must be inside transformed ink plus the device guard. Querying bounds after ordinary drawing, and querying them again, must preserve exact raw raster bytes. Two further cases reject unknown child footprints and check empty/invisible leaves. Exact byte equality uses `Span.SequenceEqual`; no channel tolerance is introduced.

The complete canonical suite passes **214 C# tests, zero failed/skipped**, after these strengthened assertions: [results](evidence/p6-lazy-ink-bounds-canonical.trx), [build/test log](evidence/p6-lazy-ink-bounds-canonical.log), [test source](evidence/p6-lazy-ink-bounds-tests.cs). These tests add 23 to the preceding 191-test suite, preserving its independent original-glyph pixel checks.

## Full canvas, subset contributors, regional byte copy

The [earlier raster trials](P6_RASTER_TRIALS.md) rejected changing the canvas clip. The next prototype instead clears a full-size scratch bitmap, retains the original full canvas clip and transform, draws contributors through `DrawingManager.Draw`, then copies only fixed device rectangle (15,15)–(350,320) into the retained bitmap.

The first explicit-contributor trial passes all 12 cases: [source](evidence/p6-full-canvas-contributors.cs), [TRX](evidence/p6-full-canvas-contributors.trx). A second trial uses `TryGetInkBounds`, transforms each leaf's footprint and adds the two-pixel guard to omit noncontributors. It also passes all 12. Rerunning that trial against the final lazy-bounds source passes **12 cases / 96 edit frames**, zero changed channels: [source](evidence/p6-lazy-ink-contributors.cs), [TRX](evidence/p6-lazy-ink-contributors.trx), [log](evidence/p6-lazy-ink-contributors.log).

Those cases cross the same three zooms, grid on/off and overlapping ellipse opacity 0.7/1.0. Eight steps cover movement, fractional/large text size, alpha, layout, opacity, empty text and restoration. At least 80 visible text objects outside the copied rectangle are omitted from scratch rendering; their retained right-hand pixels must remain unchanged. Other text/ellipse content overlaps the region and its edges. Canvas matrix, clip and save stack remain unchanged.

This is a fixed-damage sketch-leaf experiment. Temporary visibility changes select contributors only inside the test and are restored in `finally`; production does not mutate visibility to filter rendering. The prototype is archived outside compiled tests. It does not prove automatic damage detection, manager replacement, connected families, reorder/add/remove/undo damage, viewport/DPI/resize invalidation or host-overlay coverage. No new renderer or scene is adopted.

## Current native release

`artifacts/p6-lazy-ink-bounds` passes the full pinned release gate: **214 C# + 72 JavaScript**, zero build/publish warnings/errors under warnings-as-errors, 56 archive/1,106 extracted-package checks, fresh Web intermediates, native WASM linking, 134 offline assets and all 402 ZIP comparisons.

The source pin covers 209 files: `5a26224ac9a5b78412e781486e919290fea54da87606f3585c0282c802969b6b`. Offline version: `f6a3c3533a8708b2`. Identity/Brotli/gzip inventory: **29,919,195 / 8,146,854 / 10,763,876 bytes**; Brotli is 1,044 bytes above the phase-profile release and remains below 25 MiB. ZIP SHA256: `bfaf33cc26b33d3ed33ab74b4586dfc3f27409d7981ada61502d2f304df37b88`.

[Release TRX](evidence/p6-lazy-ink-bounds-release.trx), [JS](evidence/p6-lazy-ink-bounds-javascript-tests.log), [build](evidence/p6-lazy-ink-bounds-build.log), [publish](evidence/p6-lazy-ink-bounds-publish.log), [assets](evidence/p6-lazy-ink-bounds-asset-report.json), [provenance](evidence/p6-lazy-ink-bounds-provenance.json), [notices](evidence/p6-lazy-ink-bounds-runtime-notices.json), [archive hash](evidence/p6-lazy-ink-bounds-archive.sha256). Runtime notice mapping remains 101 payloads, 45 retained groups/21 original texts/24 files.

The final ordinary sibling graph was restored and rebuilt separately: **504 passed, zero failed/skipped**, warnings-as-errors. [Sibling TRX](evidence/p6-lazy-ink-bounds-sibling.trx), [build/test log](evidence/p6-lazy-ink-bounds-sibling.log), [restore](evidence/p6-lazy-ink-bounds-sibling-restore.log). The retained [net8 host/core/model build](evidence/p6-lazy-ink-bounds-net8-host.log) passes with zero warnings/errors. These are automated scopes, not independent device or owner acceptance.

## Actual published browser scope

Fresh origin [normal preview 5241](http://127.0.0.1:5241/draw/) starts the real canvas, imports the seven-tool drawing and fits it. Selected Ellipse 3 width changes from 2 to 4, then Undo restores 2: [changed state](evidence/p6-lazy-ink-bounds-normal-width4-ui.txt), [undo state](evidence/p6-lazy-ink-bounds-normal-ui.txt). The actual native download matches the original 5,105-byte drawing exactly.

Stopping the owned preview process and reloading restores seven objects from the device: [full offline state](evidence/p6-lazy-ink-bounds-offline-ui.txt). An offline selected-width edit/undo/save also passes: [width 4](evidence/p6-lazy-ink-bounds-offline-width4-ui.txt), [restored width 2](evidence/p6-lazy-ink-bounds-offline-undo-ui.txt). Actual [normal](evidence/p6-lazy-ink-bounds-normal.simpledraw.json) and [offline](evidence/p6-lazy-ink-bounds-offline.simpledraw.json) downloads share SHA256 `02743b965d9c18a3f15fdc59031c6b1af5d71434faf8b1bfb936d73f493a5ebd`. The normal page has zero diagnostics panels and warning/error logs are empty: [DOM scope](evidence/p6-lazy-ink-bounds-normal-dom.json), [logs](evidence/p6-lazy-ink-bounds-final-browser-logs.json). The preview process was restarted and its normal tab retained.

## Ordinary performance remains open

Both final runs use the unchanged actual 1,000 mixed-object fixture, one ungrouped selected Text 1, fitted scene, closed object list, visible Chromium 154 / 1280×720 / DPR 1, no profiling and no concurrent build/test process. Each completes 140 shared-history edits/dispatches/paints, restores coordinates and records 120 post-warmup samples. No workload or budget is reduced.

| Final run | Median ms | p95 ms | Maximum ms | 50 ms gate |
|---|---:|---:|---:|---|
| 1 | 57.9 | 59.9 | 78.2 | Fail |
| 2 | 51.3 | 59.8 | 60.8 | Fail |

[First raw report](evidence/p6-lazy-ink-bounds-ordinary-mixed.json), [repeated raw report](evidence/p6-lazy-ink-bounds-ordinary-mixed-repeat.json). The actual [mixed native download](evidence/p6-lazy-ink-bounds-mixed.simpledraw.json) preserves the baseline **714,792 bytes**, SHA256 `6f23948d6a7a4d386962f1c44a5781b24da4d1a1be790d93423e7190f7a59343`.

An intermediate eager-bounds release, source `0fa1b384…`, completed the same automated/normal/offline checks and ordinary p95 59.0/58.7 ms: [raw reports](evidence/p6-ink-bounds-ordinary-mixed-repeat.json), [provenance](evidence/p6-ink-bounds-provenance.json). Code review removed unconditional footprint calculation before retaining the final lazy design. Neither these limited runs nor the historical phase-profile/control observations establish causal improvement or regression. The foundation does not claim a performance gain; the normal editor still repaints its full scene.

The [capture checker](../tests/verify-ink-bounds-evidence.mjs) validates counters, 96 zero-difference frames, raw sample percentiles, history/paint counts, exact native bytes and release hashes/scope, producing [the summary](evidence/p6-lazy-ink-bounds-summary.json). It checks retained evidence rather than simulating execution.

## Next implementation gate

Implement automatic damage tracking and contributor filtering within the existing scene/renderer/host. Preserve the full canvas clip during scratch rasterization, then copy only proven damaged pixels. Track old and current ink, additions/removals, ordering/history, selection/drafts/eraser/marquee/keyboard overlays and connected/unknown component effects. Run the existing update phase before interpreting footprints. Require a full repaint after manager, viewport, size/DPI, background or otherwise unknown effects change. Keep native document limits and the ordinary 50 ms driver unchanged. Require exact full-frame comparisons and actual browser improvement before adopting the repaint optimization. External browser/device/install/host and independent acceptance remain open.

The subsequent [automatic frame cache](P6_FRAME_CACHE.md) now implements and verifies this gate: 27 exact frame tests, 241 canonical checks, a native release, sibling/net8 checks, actual normal/offline/native workflows and repeated mixed p95 30.6/30.4 ms. This document retains the earlier foundation snapshot.
