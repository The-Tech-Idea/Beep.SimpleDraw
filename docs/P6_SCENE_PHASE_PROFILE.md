# P6 shared scene CPU phase profile

Date: 2026-10-06. Task [6.B.1](../plans/enhancements/P6_RELEASE.md), [SDD](../plans/enhancements/P6_RELEASE_DESIGN.md), [master tracker](../plans/enhancements/MASTER_TRACKER.md).

Status: native publication, automated regression and focused desktop browser phase/normal/offline checks verified. This is diagnostic implementation, not a claimed performance improvement.

## Measurement contract

The existing `RenderingHelper.DrawAll` path optionally emits one `SceneRenderProfile` through `FrameMeasured`. It measures preparation/updates, grid, each dynamic component draw attempt grouped by sketch kind or component type, connections, selection/world adorners and static content. The disjoint scopes do not include all intervening transitions; total CPU time includes those transitions and the probe's own overhead. Attempt counts do not establish visibility, clipping work or GPU cost. Clear, host overlays, pixel transfer and presentation are outside these phase scopes.

`SkiaHostComponent.ScenePhasesMeasured` installs the callback for its synchronous existing manager draw and restores the previous callback in `finally`. `Home` activates it only with `?diagnostics=1&profile=1`. Ordinary drawing allocates no profile and reads no profiling clocks. No renderer, scene, history or export path is replaced. Native document limits and the ordinary 50 ms acceptance driver remain unchanged.

The diagnostic module requires finite nonnegative durations, coherent scope totals and exact dynamic draw-attempt counts matching the measured board. It retains 120 post-warmup raw phase frames alongside actual command/scene/presentation samples and validates 140 shared-history edits with restored coordinates and bounded history. Profiled presentation includes instrumentation overhead; only ordinary unprofiled runs can support the acceptance budget.

## Automated evidence

Three new native regression cases compare profiled and ordinary PNG bytes at 0.41, 0.875 and 1.75 viewport scales, with all seven sketch kinds, text, translucency, rough paths, grid, world and static overlays. Matrix, clip, save stack, history and independent per-frame reports remain intact. JavaScript adds refusal checks for missing/invalid phase data, impossible durations and mismatched attempt counts; ordinary mode retains no profile payload.

The full portable gate passes **191 C# + 72 JavaScript** checks: [C# results](evidence/p6-phase-profile-tests.trx), [JS results](evidence/p6-phase-profile-javascript-tests.log). Reviewed source pin is `5c0edb87a753355d42eb85bdb09580355bb8a09dec3e8756a7c6cde7455fd076` for 209 sibling files. Package/toolchain locks and full original notice bytes are unchanged.

## Published and broader regression gates

`artifacts/p6-phase-profile` passes the complete pinned/native release gate with zero build/publish warnings/errors, 56 archive/1,106 extracted-package checks, 134 offline assets, 101 runtime mappings and all 402 exact ZIP entries. Offline version: `cdf98b860103a5e6`; identity/Brotli/gzip inventory: 29,918,043 / 8,145,810 / 10,763,262 bytes. The Brotli inventory increases by 1,097 bytes over the cache-guard release and remains below 25 MiB. Archive SHA256: `57e857c5f8c558e51f84f43bfd6303eed7d23e6666367b55c6739332ddbef476`.

[Assets](evidence/p6-phase-profile-asset-report.json), [provenance](evidence/p6-phase-profile-provenance.json), [native publish](evidence/p6-phase-profile-publish.log), [runtime notices](evidence/p6-phase-profile-runtime-notices.json) and [archive hash](evidence/p6-phase-profile-archive.sha256) retain exact release scope. The ordinary complete sibling project was restored and rebuilt on its normal Windows/multi-target graph after publication: **504 passed, zero failed/skipped**, warnings-as-errors. [Sibling TRX](evidence/p6-phase-profile-sibling-tests.trx), [build/test log](evidence/p6-phase-profile-sibling-verified.log), [restore](evidence/p6-phase-profile-sibling-restore.log). Neither build was running during timing.

The retained Blazor host/core/model `net8.0` target also [builds with zero warnings/errors](evidence/p6-phase-profile-net8-host.log) under warnings-as-errors, using the ordinary project target selection. Its build ran after the browser timings and does not certify additional devices or engines.

## Actual same-board CPU attribution

Desktop IAB Chromium 154, visible 1280×720 / DPR 1. Fresh origin 5238 imported the same validated 1,000-object mixed fixture used by the prior AOT experiment. Text 1 was selected, the object list closed and the board fitted. Each instrumented run completed 140 actual existing-history edits, retained 120 post-warmup frames, restored coordinates and verified 140 dispatches/paints and 100 undo entries. All seven group counts match the actual document: 143 attempts per kind except 142 freehand attempts. These are component draw attempts, not individual glyph counts.

| Instrumented group | Median ms, run 1 / 2 | p95 ms, run 1 / 2 |
|---|---:|---:|
| Text (143 attempts) | 21.4 / 21.4 | 23.0 / 27.2 |
| Ellipse (143) | 2.8 / 2.8 | 3.7 / 3.5 |
| Rectangle (143) | 1.9 / 2.1 | 2.6 / 2.9 |
| Diamond (143) | 1.7 / 1.7 | 2.3 / 2.4 |
| Line (143) | 0.4 / 0.4 | 0.8 / 0.8 |
| Arrow (143) | 0.5 / 0.5 | 0.7 / 0.9 |
| Freehand (142) | 0.5 / 0.5 | 0.8 / 0.9 |

Full CPU scene median/p95: 32.1/35.2 ms and 32.3/41.2 ms. Instrumented presentation p95: 50.4 / 59.0 ms. Group percentiles describe separate distributions and must not be added. Text is the largest measured component group in both runs, making text replay/raster work the next investigation target. This probe does not separate glyph-recording from picture playback/raster cost or establish an effective optimization.

After full navigation to ordinary diagnostics on 5238, the restored identical board had no phase payload: p95 **68.5 / 50.1 ms**, medians 49.5 / 48.7 ms. A separately served unchanged cache-guard control on fresh origin 5239 measured p95 **50.1 / 50.2 ms**, medians 48.7 / 48.8 ms. Every run exceeds the 50 ms budget. The first ordinary tail is retained; these limited observations do not establish causal improvement or regression from the probe. The ordinary acceptance method/budget were unchanged, and no compiler/tests overlapped timing.

[First profile](evidence/p6-phase-profile-1000-mixed.json), [profile repeat](evidence/p6-phase-profile-1000-mixed-repeat.json), [ordinary repeat](evidence/p6-phase-profile-ordinary-1000-mixed-repeat.json), [unchanged control](evidence/p6-phase-profile-control-1000-mixed-repeat.json) and [independent summary](evidence/p6-phase-profile-browser-summary.json) retain all samples. Run `node tests/verify-scene-phase-evidence.mjs` to validate scope/counts/timings, executed edits, counters and actual native outputs.

## Normal editor and offline regression

Actual thousand-object downloads after profiled and ordinary runs are byte-identical to the prior baseline: 714,792 bytes, SHA256 `6f23948d6a7a4d386962f1c44a5781b24da4d1a1be790d93423e7190f7a59343`. Normal navigation without diagnostic parameters reopens the canonical seven-tool file, fits it, edits selected width 2→4→2 through undo, and downloads an exact native copy. After the owned 5238 server was stopped, reload restores all seven objects and native download still succeeds; both seven-tool downloads match the original 5,105-byte file, SHA256 `02743b965d9c18a3f15fdc59031c6b1af5d71434faf8b1bfb936d73f493a5ebd`. Normal editing has zero diagnostic panels. [Full offline UI](evidence/p6-phase-profile-offline-ui.txt), [offline screenshot](evidence/p6-phase-profile-offline.png), [empty browser warning/error logs](evidence/p6-phase-profile-browser-logs.json), [ordinary thousand-object download](evidence/p6-phase-profile-ordinary-roundtrip.simpledraw.json) and [offline seven-tool download](evidence/p6-phase-profile-offline.simpledraw.json) retain the workflow.

Current normal preview is 5238 while its restarted server runs. Mixed p95, physical-device/public-host/cold-process and independent acceptance remain open. The full P1–P6 goal remains active.
