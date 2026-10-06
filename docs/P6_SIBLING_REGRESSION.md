# P6 complete existing Beep.Skia test-project regression

Date: 2026-10-05. [Master tracker](../plans/enhancements/MASTER_TRACKER.md) · [Release checklist](RELEASE_CHECKLIST.md).

The complete existing `Beep.Skia.Tests` project passes **504 tests, zero failed/skipped** on Windows, with Release dependencies built under warnings-as-errors. This closes the previously unrun full sibling test-project gate. It does not certify manual WinForms interaction, physical devices, other browser engines or independent acceptance.

## Scope and commands

The test project targets `net10.0-windows10.0.19041.0`, references 19 projects directly and restores a 21-project closure. Ordinary sibling targets remain in effect: the build includes core/model net10/net9, FlowChart net10, the other referenced families/loader net9 and Windows controls/tests net10 Windows. No global target-framework override or portable-only source substitution is used.

Run from the SimpleDraw repository:

```powershell
dotnet restore ../Beep.Skia/Beep.Skia.Tests/Beep.Skia.Tests.csproj --configfile NuGet.Config -p:GeneratePackageOnBuild=false -p:SkipMinVer=true -p:BeepSkiaLocalBuildCopies=false
dotnet test ../Beep.Skia/Beep.Skia.Tests/Beep.Skia.Tests.csproj -c Release --no-restore -p:GeneratePackageOnBuild=false -p:SkipMinVer=true -p:BeepSkiaLocalBuildCopies=false -p:TreatWarningsAsErrors=true --logger 'trx;LogFileName=sibling-tests.trx' --results-directory artifacts/p6-sibling-regression-verified/tests -v:minimal
node scripts/pin-inputs.mjs verify
```

This broader restore uses the ordinary global package cache and declared app feeds. It is separate from the app's isolated-cache portable reproduction. The two cached owner archives were checked against the existing exact archive pins; no claim of a fresh isolated-cache reproduction of every sibling target is made.

## Failures resolved without weakening product contracts

The initial complete run passed 502/504. `MindMapLayoutTests.Arrange_PlacesTopicsAroundCentralAtLevelSpacing` reused central output port 0 for three children. The manager accepts one edge per directional port; the additional connections were refused, leaving orphan nodes. The corrected fixture uses existing central outputs 0/1/2 and asserts every connection succeeds and exactly three edges exist. Determinism and subtopic fixtures also assert successful connections/counts. Existing radial spacing, non-overlap, parent spacing and deterministic-position assertions remain intact. Production layout and connection policy are unchanged.

`RenderPerformanceTests.RenderLargeDiagram_ReportsFrameCostAndAllocations` required connections to allocate more managed memory than nodes alone. Retained line resources can avoid that extra allocation. The corrected fixture asserts all 199 edges were created between 200 nodes and compares actual native raster pixel bytes against the unconnected workload. The existing limits remain **100 ms/frame** and **512 KB/frame**. The complete passing run reports nodes-only 5.37 ms / 17.27 KB and connected 7.89 ms / 17.31 KB per frame. These are desktop native smoke budgets, separate from the app's still-open 1,000 mixed-object browser p95 budget.

The five focused fixture checks passed before the complete 504-test rerun. Only two sibling test files changed. The portable 209-file source pin remains `b80560e6fcddb0ba973287babfe178312819b5e19de0e94b421a4c5356f46568`; neither app/runtime source nor the published candidate changed. Test files are outside that portable pin and their exact changes are retained separately.

## Retained evidence

- [Initial complete run](evidence/p6-sibling-regression-initial.trx), [focused run](evidence/p6-sibling-regression-focused.trx), [passing complete run](evidence/p6-sibling-regression-verified.trx).
- [Restore log](evidence/p6-sibling-regression-restore.log), [complete build/test log](evidence/p6-sibling-regression-verified.log), [test changes](evidence/p6-sibling-regression-tests.patch).
- [Counters, measured output, test-input hashes, source pin and evidence hashes](evidence/p6-sibling-regression-summary.json).

Mixed browser performance, physical-device/accessibility/manual desktop checks, hosted CI/public-host/install verification and independent owner/maintainer acceptance remain open. Green automated checks do not sign those gates.


## Phase-profile source rerun

The 2026-10-06 [phase-profile source](P6_SCENE_PHASE_PROFILE.md), pin `5c0edb87…`, was rebuilt against the same ordinary 21-project/19-reference graph after its native portable release. All 504 existing sibling tests pass again, zero failed/skipped, warnings-as-errors: [current TRX](evidence/p6-phase-profile-sibling-tests.trx), [build/test log](evidence/p6-phase-profile-sibling-verified.log), [restore](evidence/p6-phase-profile-sibling-restore.log). The previous test-fixture corrections remain unchanged. The earlier b80560e6 evidence above is preserved as historical scope; current renderer profiling has its own exact pixel, browser, native and offline evidence.

## Lazy ink-bounds source rerun

The later [ink-bounds foundation](P6_INK_BOUNDS.md), source 5a26224a…, was restored and rebuilt on the ordinary sibling graph. All 504 existing tests pass with warnings-as-errors, zero failed/skipped: [TRX](evidence/p6-lazy-ink-bounds-sibling.trx), [build/test log](evidence/p6-lazy-ink-bounds-sibling.log), [restore](evidence/p6-lazy-ink-bounds-sibling-restore.log). The original fixture corrections remain intact. The retained net8 host/core/model build also passes with zero warnings/errors. Independent/manual/device acceptance and mixed browser performance remain open.

## Automatic frame cache delivery — 2026-10-06

The [frame-cache release](P6_FRAME_CACHE.md) passes 241 canonical/native C# + 72 JS checks, full ordinary sibling regression (504 passed, no failed/skipped) and a zero-warning net8 host build. Source pin 4f280f93 covers 210 inputs. Repeated ordinary mixed desktop p95 30.6/30.4 ms passes the unchanged 50 ms gate; normal/server-stopped edit/undo/native bytes and scheduling remain exact. Candidate artifacts/p6-frame-cache-release, offline 99a7f9824c40819e, preview 5242. External device/accessibility, intended host/provider/cold-process, hosted CI/install and independent acceptance remain open. Earlier entries retain historical evidence.
