# Stage 6: Portable release, performance and maintainability — tasks
[SDD](P6_RELEASE_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: P1–P4 closure; P5 optional for first release. Estimate: 3–5 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 6.A.1 | Pin build inputs and define sibling/package distribution | Automated verified | [Build contract](../../docs/BUILD_REPRODUCIBILITY.md): 210 sibling inputs, seven locks, 56 exact archives; fresh checkout/restore and cache refusal checks |
| 6.A.2 | Add CI restore/test/release publish and artifact checks | Local gate verified; hosted CI open | Current [offline-notices release](../../docs/P6_OFFLINE_NOTICES.md): 289 C# + 77 JS, native link, zero warnings/errors and all 405 ZIP entries; actual startup/normal/offline/roundtrip checked. Publish-cache correction retained. Hosted workflow run open |
| 6.A.3 | Run complete existing Beep.Skia test-project regression | Automated verified | [Sibling regression](../../docs/P6_SIBLING_REGRESSION.md): original fixture corrections retained; prior [frame-cache run, unchanged sibling inputs](../../docs/P6_FRAME_CACHE.md) passes 504, zero failed/skipped on ordinary multi-target graph with warnings-as-errors, source pin 4f280f93…; net8 host build passes |
| 6.B.1 | Benchmark native payload, startup and 1,000-shape editing | Mixed/rectangle desktop gate verified; intended startup/matrix open | Current [production session overlays](../../docs/P6_SESSION_OVERLAYS.md): 48 additional cases / 528 exact frames and ordinary mixed p95 30.5/30.2 ms pass the unchanged 50 ms limit; actual native bytes and scheduling remain exact. Rectangle p95 39.4/35.7 ms, native 2,000 boundary and separate 5,000 renderer stress previously verified. Controlled startup 2.96–3.13 s; physical-provider/public-host/cold-process and external devices open |
| 6.B.2 | Verify root/subpath/offline/update deployment | Desktop browser verified; matrix open | [Build/hosting evidence](../../docs/P6_BUILD_HOST_VERIFICATION.md): CSP, exports, stopped-server reload/edit, draft/tab update guards and activation; formal install/other engines/devices open |
| 6.B.3 | Complete notices, operations and user guide | Engineering distribution verified; independent review open | [Notice evidence](../../docs/P6_NOTICES_VERIFICATION.md): 24 delivered files/21 original texts, all 101 runtime files mapped, actual stopped-server page/text access; guide and ownership/header reviews unsigned; [directly controlled IAB online/offline exact downloads pass](../../docs/P6_OFFLINE_NOTICES.md); separate popup completion unverified |
| 6.M.1 | Record all release evidence and owner acceptance | In progress | Local evidence recorded; independent review and owner acceptance unsigned |

## Verification scenarios
Fresh checkout restore/build/test/publish; artifact inventory and compressed bytes; offline browser matrix; CSP and no-external-request inspection; 1,000-shape performance evidence; root/subpath hosting and export/download checks.

## Closure record
Build/hosting implementation and focused automated/desktop evidence are recorded in [P6 verification](../../docs/P6_BUILD_HOST_VERIFICATION.md). Notices are distributed and engineering verified. Performance, independent notices/header/guide review, external browser/device/install gates and owner acceptance remain open. The full-plan goal is blocked after three resumed audits found unchanged external verification and independent acceptance prerequisites; see the current master tracker.

## Next action
The [production session overlay release](../../docs/P6_SESSION_OVERLAYS.md) verifies 528 exact frames, actual responsive pointer/resize/modal/viewport behavior and repeated ordinary mixed desktop p95 below 50 ms. Continue intended-host/provider/cold-process, independent ownership/header/guide review, popup download completion, physical-device/screen-reader/high-DPI/zoom, hosted CI/public-host/install and independent acceptance gates. Keep task IDs stable.

## Consolidated local delivery - 2026-10-06

Current candidate artifacts/simpledraw-v1-final includes all current P1-P6 engineering features and shared editing updates. Its gate passes 585 C# / 79 JS with native publish and exact offline/archive verification. The preview update preserves ten user objects. See docs/FINAL_DELIVERY.md (repository root) for current evidence and limits. The historical blocked goal paragraphs above do not describe the current active goal; external acceptance remains open. Existing P6 task IDs and independent role signatures are retained.
