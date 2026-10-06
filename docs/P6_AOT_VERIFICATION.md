# P6 Release AOT performance investigation

Status: measured and rejected; production AOT flag/package reverted. [Master tracker](../plans/enhancements/MASTER_TRACKER.md) · [Performance protocol](P6_PERFORMANCE.md).

The experiment requested `RunAOTCompilation=true` for Release native publication. It used the same app, shared scene/history and native raster backend. The unchanged 50 ms editing budget was measured through the existing driver. The production flag and added package were removed after two failed measurements; the shared renderer remains unchanged.

[Microsoft's AOT guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-10.0) describes a CPU-performance/download-size tradeoff. This experiment did not establish a benefit worth adopting.

## Dependency repair and complete trial gate

The initial `artifacts/p6-aot-trial` stopped at the managed-only build: the SDK rejects AOT when `WasmBuildNative=false`. The trial condition was corrected to select only Release native builds. `artifacts/p6-aot` then passed 188 C# + 70 JS, but AOT compilation failed on the transitive `System.Private.Windows.Core.dll` input because its `System.Formats.Nrbf` assembly dependency was unavailable.

The trial declared [Microsoft's System.Formats.Nrbf 10.0.12 package](https://www.nuget.org/packages/System.Formats.Nrbf/10.0.12) explicitly. It has no net10 package dependencies and supplied the missing compiler input. No product file-format/import path was introduced. Its exact archive SHA256 is `be0554f6c2bb0b6ba2e50c219ae48bd4650101383e5675a003a22462f649b508`.

The trial union pinned 57 archives; all 1,125 extracted package payload files verified. [Trial package metadata/notices audit](evidence/p6-aot-package-license-audit.json) preserves the previous 56 package records and adds this exact Microsoft MIT package. Its notice bundle contained 46 groups, 21 unique original texts and 24 distributed files. Original notice bytes were unchanged. Native attribution now conservatively covers resolved managed runtime candidates and reviewed source, since AOT places managed code inside the native WASM payload. The retained refusal/coverage test prevents an unmapped managed candidate from passing native attribution.

The repair trial `artifacts/p6-aot-nrbf` compiled all 86 AOT inputs and their 85 bitcode units, including the generated instance assembly. The subsequent complete `artifacts/p6-aot-verified` gate passed **188 C# + 71 JS**, zero-warning Release compilation/native publish, 135 offline assets, 102 runtime source mappings and **405 exact ZIP-entry comparisons**. Offline version: `7e72b91b309ca7b5`; identity/Brotli/gzip inventory: 68,430,473 / 13,945,104 / 20,669,267 bytes. Its archive SHA256 is `782185eeec5f139a9612f794b5fa5eb8e15d9ade5ec5569bf3aff027b0e3794a`. It passes the compressed inventory gate but is an experimental artifact, not the adopted release.

[Asset report](evidence/p6-aot-asset-report.json), [provenance](evidence/p6-aot-provenance.json), [C# results](evidence/p6-aot-tests.trx), [JavaScript results](evidence/p6-aot-javascript-tests.log), [runtime notices](evidence/p6-aot-runtime-notices.json) and [archive hash](evidence/p6-aot-archive.sha256) retain exact trial scope. [Missing dependency log](evidence/p6-aot-missing-dependency.log) and [successful repair log](evidence/p6-aot-repaired-publish.log) preserve the investigation.

## Actual same-board editing measurements

The unchanged contrast-final baseline was served on fresh origin 5234; the verified AOT trial on 5235. Both imported the same validated 1,000 mixed-object fixture, selected Text 1, closed the object list and fitted the board at 1280×720 / DPR 1 in visible IAB Chromium 154. Compilation had finished before measuring. Each run performed 140 actual shared-history edits and retained 120 post-warmup samples. Both origins preserve the exact 50 ms budget and existing presentation proxy.

| Candidate/run | Median ms | p95 ms | Maximum ms | Result |
|---|---:|---:|---:|---|
| Non-AOT baseline | 48.6 | 50.5 | 68.0 | Fail |
| AOT first | 48.7 | 50.6 | 51.7 | Fail |
| AOT repeat | 48.7 | 50.6 | 60.5 | Fail |

Each run verified 140 revisions, 140 dispatches and 140 actual paints, one selected object, restored coordinates and 100 retained undo entries. [Baseline raw report](evidence/p6-aot-baseline-performance.json), [AOT first](evidence/p6-aot-1000-mixed.json), [AOT repeat](evidence/p6-aot-1000-mixed-repeat.json) and [independent evidence summary](evidence/p6-aot-browser-summary.json) retain all samples. Run `node tests/verify-aot-browser-evidence.mjs` to verify the captures, native byte comparison and exact rejected-candidate inventory.

The actual [baseline native download](evidence/p6-aot-baseline.simpledraw.json) and [AOT download after both runs](evidence/p6-aot-roundtrip.simpledraw.json) are byte-identical: 714,792 bytes, SHA256 `6f23948d6a7a4d386962f1c44a5781b24da4d1a1be790d93423e7190f7a59343`. [Measured board screenshot](evidence/p6-aot-measured-board.png) records the actual trial. AOT warning/error logs were empty. Correct native output does not establish a latency improvement.

## Current source and remaining work

AOT and its extra package are reverted; the original 56-archive / 1,106 extracted-file inputs and 45-group/24-file notice bundle are restored. The 209-file sibling source pin remains `b80560e6fcddb0ba973287babfe178312819b5e19de0e94b421a4c5356f46568`. Retained build-tool changes record evaluated AOT properties, allow a separately named package-audit capture and verify conservative native managed-code notice coverage. The rejection does not change rendering/history/native limits or the performance budget.

## Release cache regression and correction

The incremental non-AOT `artifacts/p6-after-aot` passed 188 C# + 71 JS, offline integrity and all 402 archive checks, but actual browser startup failed with a Mono interpreter assertion. Its reused linked assemblies had been stripped during the earlier AOT publish. [Captured error](evidence/p6-aot-cached-restore-logs.json), [startup UI](evidence/p6-aot-cached-restore-ui.txt) and [runtime delta](evidence/p6-after-aot-runtime-delta.json) preserve the failure. Passing managed tests and static integrity checks did not establish usable browser startup.

The canonical release script now moves the web project's intermediate directory into the new candidate's `managed-web-intermediates`, repeats locked Web restore and native toolchain verification, and publishes from fresh web intermediates. It preserves the previous cache for inspection and retains the existing single source/model/runtime path. The guard was exercised by deliberately putting the known failing cache back into the web project before running the complete release gate; [setup](evidence/p6-aot-cache-regression-setup.json) records that condition.

`artifacts/p6-publish-cache-guard` passes **188 C# + 71 JS**, zero-warning build/native publish, 56 archive/1,106 extracted-package verification, 134 offline assets, 101 runtime mappings and **402 exact ZIP checks**. Offline version is `b8c46ebd9ba44fe5`; identity/Brotli/gzip inventory is 29,913,096 / 8,144,713 / 10,761,219 bytes. [All-file comparison](evidence/p6-publish-cache-guard-runtime-equivalence.json) confirms every published file is byte-identical to contrast-final, with no added, missing or changed files. [C# results](evidence/p6-publish-cache-guard-tests.trx), [JS results](evidence/p6-publish-cache-guard-javascript-tests.log), [assets](evidence/p6-publish-cache-guard-asset-report.json), [provenance](evidence/p6-publish-cache-guard-provenance.json), [toolchain](evidence/p6-publish-cache-guard-toolchain.json), [publish](evidence/p6-publish-cache-guard-publish.log) and [archive hash](evidence/p6-publish-cache-guard-archive.sha256) retain the guarded build.

Actual fresh-origin browser startup, seven-tool import, selected-width edit/undo, exact native download and stopped-server reload are recorded in [guarded browser evidence](evidence/p6-publish-cache-guard-browser-summary.json), [full offline UI](evidence/p6-publish-cache-guard-offline-ui.txt), [screenshot](evidence/p6-publish-cache-guard-offline.png) and [empty warning/error logs](evidence/p6-publish-cache-guard-browser-logs.json). The baseline, edit/undo and offline downloads are byte-identical: 5,105 bytes, SHA256 `02743b965d9c18a3f15fdc59031c6b1af5d71434faf8b1bfb936d73f493a5ebd`. Archive SHA256: `7d3880aaa7af800ac141c93c89b5c1480806e0fd46e95071fda7f7f616f3906a`. Run `node tests/verify-publish-cache-evidence.mjs` to validate the captured failure/correction and native-byte regression. This closes the reproduced compilation-mode cache defect; it does not certify every possible incremental compiler issue.

Mixed editing performance remains over budget. Startup/provider/cold-process, physical-device/other-engine/install and independent acceptance remain separate gates. The measured failures do not identify a proven bottleneck; the renderer/pixel-transfer investigation continues.
