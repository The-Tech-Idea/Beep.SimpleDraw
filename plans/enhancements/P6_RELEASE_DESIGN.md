# Stage 6: Portable release, performance and maintainability — SDD
Date: 2026-10-05. Status: implementation in progress; see master tracker for verified progress.
Estimate: 3–5 engineer-days. Entry dependencies: P1–P4 closure; P5 optional for first release.

## 0. Problem statement
Pin .NET SDK and NuGet dependencies; choose released Beep.Skia packages or CI checkouts at exact commits. Use portable build scripts and a static-host example, with no database/mandatory server. Test Release trimming/native link and all active component factories. Measure cold compressed bytes, first usable canvas and p95 frame delay for 100/1,000/5,000 components on named devices. Optimize shared render scheduling/culling only if measured bottlenecks justify it. Define cache/base path/wasm MIME settings, CSP, install/update procedure, dependency notices and rollback to previous static artifact.

## 1. Goals and acceptance criteria
1. Reproduce a release from declared, pinned source/package artifacts without a machine feed, account or undeclared global cache. Two licensed owner archives are bundled explicitly; their versions are not claimed to be public releases.
2. Serve a static publish folder at root and a subpath; install and reload offline.
3. Measure payload/startup/edit performance and record browser results.
4. Publish documentation, dependency notices and an honest readiness checklist.

## 2. Design
The adopted [input decision](../../docs/decisions/002-pinned-build-inputs.md) and [build procedure](../../docs/BUILD_REPRODUCIBILITY.md) define exact source/toolchain/package inputs, verified cache hits, a non-overwriting build gate and static packaging. CI configuration is locally reviewed/tested separately from hosted execution.

Native publication always starts with fresh Web intermediates: after managed tests, preserve the canonical Web `obj` under the candidate, repeat locked Web restore and verify native toolchain selection. Earlier AOT stripping can otherwise leave linked managed assemblies that pass static gates but crash an interpreter startup. [Replayed-cache evidence](../../docs/P6_AOT_VERIFICATION.md) validates the correction through all-file runtime equivalence and actual browser startup, native roundtrip and offline reload. Package/source caches remain independently audited; this rule does not introduce another renderer or model.

Native documents remain bounded to 2 MiB/2,000 components. Benchmark 100/1,000 scenes for the product budget and the 2,000 native boundary. The original 5,000-component case is shared-renderer stress evidence plus explicit native-import refusal, not a claim that the app accepts oversized native files. Do not silently raise the document limit for a benchmark.
Pin .NET SDK and NuGet dependencies; choose released Beep.Skia packages or CI checkouts at exact commits. Use portable build scripts and a static-host example, with no database/mandatory server. Test Release trimming/native link and all active component factories. Measure cold compressed bytes, first usable canvas and p95 frame delay for 100/1,000/5,000 components on named devices. Optimize shared render scheduling/culling only if measured bottlenecks justify it. Define cache/base path/wasm MIME settings, CSP, install/update procedure, dependency notices and rollback to previous static artifact.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
No application HTTP API. Static hosting contract: index fallback, application/wasm, HTTPS, base href, versioned files; the deployment wrapper is configurable infrastructure.

Local diagnostics are implemented behind the explicit `?diagnostics=1` query. Home.BenchmarkState and BenchmarkEdit are JS-invokable only in this opt-in mode; the latter requires a running benchmark, one actual active selected component and a ready editor, then invokes the normal Shortcut path. The diagnostic module waits for the actual canvas paint callback, reports raw samples in the DOM and validates revision/geometry/bounded history. No alternate editing or rendering route is used. [Performance protocol and evidence](../../docs/P6_PERFORMANCE.md) retain both failed and passing measurements and identify unmeasured broadband/device gates.

The explicit `&profile=1` probe also captures one `SceneRenderProfile` from the existing renderer. Disjoint CPU scopes cover preparation/updates, grid, component draw attempts grouped by sketch kind or component type, connections, selection/world adorners and static content. Total includes unassigned transitions and instrumentation overhead; counts identify attempts rather than visible pixels. The host installs its callback only for the synchronous draw and restores any previous callback in `finally`. Ordinary draws allocate no report and read no profiling clocks. JavaScript requires finite nonnegative scopes, coherent totals and exact board considered counts (draw attempts plus known skips), retains 120 post-warmup raw frames, and leaves the ordinary 50 ms acceptance driver unchanged. Exact native raster, matrix/clip/save-stack and history regression tests cover the probe. Historical [phase evidence](../../docs/P6_SCENE_PHASE_PROFILE.md) identifies text as the largest measured component group across two 140-edit runs; those earlier ordinary/control p95 measurements exceeded the budget. The subsequent automatic cache/session-overlay release passes ordinary mixed p95 30.5/30.2 ms. Attribution does not isolate glyph recording versus replay/raster cost or establish a causal speed change.

RendererStress opens a disposable RendererStressScene only in the opt-in diagnostics UI. It generates 5,000 rough rectangles or seven mixed sketch tools and uses the existing DrawingManager, MoveSelectedComponents history and SkiaHostComponent. Both cases perform 140 moves, retain 120 presentation samples and verify actual paints, restored coordinates and history capacity. The harness does not enter native documents or autosave and does not receive the 1,000-object performance budget. Home compares the product's serialized drawing and revision on close. The existing modal focus boundary applies; close/cancellation disposes temporary native resources and pending driver waits. Actual historical desktop rectangle/mixed/repeated runs and close/Escape/cancellation pass. Later ordinary mixed editor measurements pass the unchanged 50 ms budget; the separate stress results retain their original scope.

Measured optimization retains native SketchShape paths/paints/absolute glyph recordings under complete invalidation keys and the existing disposal path. Empty children skip sorting; nonempty priority order is preserved. ObjectList avoids quadratic name scans and retains displayed row/reference snapshots, without owning document state. Exact cached-versus-original glyph pixels and actual same-ID file replacement/export checks are recorded separately from performance.

Additional `profile=1` requires `diagnostics=1` and enables the existing shared host's optional ScenePaintMeasured callback. Stopwatch records CPU clear plus DrawingManager.Draw, before overlays; the ordinary host reads no clock when the callback is unset. The diagnostic driver separately records command invocation completion and the original presentation proxy, retaining all raw samples and refusing missing/nonfinite CPU evidence. These durations can overlap and must not be added or treated as GPU timestamps. This instrumentation identifies bottlenecks before changing rendering or scheduling; it does not alter the 50 ms acceptance budget.

SkiaHostComponent combines pending invalidations before dispatching to the existing SKCanvasView. Invalidation during painting records one follow-up and yields past the synchronous vendor callback before dispatching it. Disposal suppresses that continuation. Opt-in lifetime counters exclude vendor resize/DPI requests; normal URLs enable neither counters nor profiling. A source-owned scheduling check sends three initial and three in-paint requests, waits for two actual paints, requires exactly two dispatches/paints and unchanged revision/history/counts/selected coordinates. The browser confirms that check on blank and 1,000-object scenes, plus exactly 140 dispatches/paints for 140 ordinary edits. This is scheduling evidence. Historical measurements for that scheduling increment recorded mixed p95 50.7 ms, above the 50 ms budget. The later production session-overlay release records ordinary mixed p95 30.5/30.2 ms, passing the unchanged budget; see [current performance evidence](../../docs/P6_SESSION_OVERLAYS.md).

## 4. Files to change
The optional local `serve-profiled.mjs` harness calls the same verified static host with ResponseLink delivery. A single monotonic reservation clock shares the encoded-body bandwidth limit across concurrent responses; idle time cannot accumulate burst credit. Each response waits its declared delay before headers/body; HEAD sends no body. Disconnects abort pending waits and release streams. Ordinary hosting retains its direct stream and all existing scope/MIME/compression/CSP/cache policy. The CLI requires an absent JSONL evidence file and records exact artifact/profile metadata and completed/aborted body transfers. This is a controlled local download/response-delay model, excluding DNS/TLS/header/upload/packet-loss/provider effects; it is not browser or physical-network throttling. Fresh origins are required for cold browser observations.

| Action | File | Estimated lines | Risk |
|---|---|---|---|
| New | scripts/build.ps1 | ~50 | low |
| New | docs/DEPLOYMENT.md | ~130 | low |
| New | .github/workflows/verify.yml | ~100 | medium |
| New | docs/RELEASE_CHECKLIST.md | ~100 | low |

## 5. Error handling matrix
Reproducibility failure blocks release evidence. Subpath missing assets blocks release. Update regressions roll back static artifact while retaining document schema support.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Fresh checkout restore/build/test/publish; artifact inventory and compressed bytes; offline browser matrix; CSP and no-external-request inspection; 1,000-shape performance evidence; root/subpath hosting and export/download checks.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P6_RELEASE.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
SDK/native workload drift: pin versions and build-machine setup. Broad Beep core regression is separate from app tests and must be recorded, not inferred from focused green checks.

## 9. Out of scope
Hosting charges, public deployment or a fabricated acceptance signature.

## 10. Sub-task execution order
6.A.1. Pin build inputs and define sibling/package distribution. Verify against the corresponding stage acceptance scenario before advancing.
6.A.2. Add CI restore/test/release publish and artifact checks. Verify against the corresponding stage acceptance scenario before advancing.
6.B.1. Benchmark native payload, startup and 1,000-shape editing. Verify against the corresponding stage acceptance scenario before advancing.
6.B.2. Verify root/subpath/offline/update deployment. Verify against the corresponding stage acceptance scenario before advancing.
6.B.3. Complete notices, operations and user guide. Verify against the corresponding stage acceptance scenario before advancing.

Notices implementation retains full exact vendor texts in one generated public bundle, with a lock tied to package/toolchain/font inputs. Build verifies originals and installed SDK notices; publish verifies runtime source mappings and all notice offline hashes. Unknown payload attribution fails packaging. Independent owner/header/guide review remains separate from engineering distribution evidence. See [notices verification](../../docs/P6_NOTICES_VERIFICATION.md).
6.M.1. Record all release evidence and owner acceptance. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 6 row and review handoff. ENG performs implementation; QA records actual evidence; ENG owns implementation delivery; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
Task 6.A.3 verifies the complete existing sibling Windows test project against ordinary core/family/controls targets, separately from the portable app's isolated-cache native release. [Recorded execution](../../docs/P6_SIBLING_REGRESSION.md) passes all 504 tests with warnings-as-errors. Actual graph/pixel checks replace stale fixture assumptions while preserving port policy and rendering budgets. This automated gate does not imply manual desktop/device certification or independent acceptance.

All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.

The [ink-bounds foundation](../../docs/P6_INK_BOUNDS.md) implements conservative leaf footprints in existing SketchShape/typeface code and passes 23 raster checks. Its historical native release passed 214 C# + 72 JS and mixed p95 59.9/59.8 ms. The subsequent [automatic frame cache](../../docs/P6_FRAME_CACHE.md) is implemented in the existing renderer/host with full-canvas scratch rendering, old/new ink damage, retained-pixel copying, known host overlays and conservative full invalidation. It adds 27 exact frame tests; the canonical suite passes 241. Profile groups count actual attempts and known skips separately. Browser release/performance and sibling reruns are tracked separately; the budget remains 50 ms.

The subsequent [session overlay coverage](../../docs/P6_SESSION_OVERLAYS.md) moves Home's existing overlay painting/footprints into DrawingSession and delegates the page callbacks to them. Forty-eight additional tests exercise actual pointer/keyboard/selection/eraser/marquee/endpoint/resize states, recording 528 exact frames and footprint containment at zoom .41/.875/1.75 and incoming device scale 1.25/2. The new native release passes 289 C# + 72 JS with zero managed build/publish warnings/errors. Actual normal/offline/responsive/native/scheduling gates pass; repeated ordinary mixed p95 is 30.5/30.2 ms against the unchanged 50 ms budget. Beep.Skia inputs are unchanged at source 4f280f93, so its 504-test ordinary regression/net8 evidence retains that exact scope.

The [2026-10-06 raster trials](../../docs/P6_RASTER_TRIALS.md) rejected per-glyph Alpha8 composition and retained-pixel repainting with a reduced canvas clip: exact pixels differed, including grid-disabled and nonisolated-overlap controls. Both prototypes remain archived outside production/test compilation. The subsequent automatic cache preserves the original raster clip and commands; its release gates remain evidence-driven.
