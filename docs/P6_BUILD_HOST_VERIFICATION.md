# P6 pinned build and static hosting verification
Later sibling regression closure: [the complete existing Windows test project](P6_SIBLING_REGRESSION.md) passes all 504 tests against its ordinary target graph, with warnings-as-errors. This separate global-cache test run leaves the portable source pin and browser-tested artifact unchanged; earlier reproduction results below remain historical.

Date: 2026-10-05. [Authoritative tasks](../plans/enhancements/P6_RELEASE.md). This is focused engineering evidence, not owner acceptance.

## Build inputs and execution
The fresh sibling reconstruction uses commit `19bb026ba403ff91bd1a2ce324000ddce07cd26d`, the reviewed patch and explicit added files. All 209 normalized input files match digest `09ef2eb8612abef6a0b5c466e19fce3a231fa55ccc9390cff84b1515a23e20d1`. SDK 10.0.401, Node 24.14.0, wasm manifest 10.0.112, runtime/compiler packs 10.0.12 and Emscripten 3.1.56/packs 10.0.12 are checked before and after restore. The actual native reference is SkiaSharp.NativeAssets.WebAssembly 4.150.1, Emscripten 3.1.56, single-threaded SIMD.

An isolated package directory downloaded the declared graph from nuget.org and the two bundled owner archives; initial restore took 1.73 minutes. No machine-global package cache or developer feed supplied this graph. Subsequent candidates reused that explicitly audited directory. Seven net10 locks pin 56 archives; archive, metadata and 1,106 extracted payload-file comparisons pass. Owned copies of an archive and extracted build property were deliberately changed; each was refused, then restored. See [cache refusal evidence](evidence/p6-cache-refusal.json). Input drift/new files, patch/added-file/archive tampering, corrupt offline payloads/encodings, unsafe paths, missing fonts, external entry references and hosting-report mismatch have automated refusal coverage.

`scripts/build.ps1` was executed from `artifacts/p6-verified/Beep.SimpleDraw` with output `artifacts/update-candidate`, base `/draw/`, and the fresh audited package directory from `artifacts/p6-clean/Beep.SimpleDraw/artifacts/fresh-packages`. The solution maps the same four sibling projects to Release. Their opt-in `BeepSkiaPortableTargetFramework=net10.0` selects only the intended target without globally overriding single-target app projects. Developer-local copy/packaging targets are disabled for this gate.

The final candidate passed locked restore, managed build, **162 C# tests (zero skipped/failed)**, **47 JavaScript tests**, native-linked trimmed publish, offline/SRI/encoding checks, provenance and **324 exact ZIP-entry comparisons**. Build/publish report zero warnings/errors. C# TRX counters guard against the test runner exiting successfully with zero discovered tests. Existing multi-target defaults remain intact; a new retained-net8 run and the complete sibling desktop/family suite were not performed in this increment.

## Candidate inventory
| Item | Verified value |
|---|---|
| Latest subpath candidate | `artifacts/p6-verified/Beep.SimpleDraw/artifacts/update-candidate/publish/wwwroot` |
| Subpath/offline content version | `/draw/` / `362ea4bc22cb4f26` |
| Identity folder bytes | 28,762,736 |
| Brotli inventory bytes | 7,975,472; below 25 MiB gate |
| Gzip inventory bytes | 10,514,315 |
| ZIP SHA256 | `5ee607d52653ab6688a81cbfe7693a5b7e14dde8a85ea945fae6dd0eef192c28` |
| Root variant exercised | `artifacts/p6-root-final/wwwroot`; `/`; version `9cedf01f0225878f` |

[Subpath asset report](evidence/p6-subpath-asset-report.json), [provenance](evidence/p6-subpath-provenance.json), and [root report](evidence/p6-root-asset-report.json) preserve input and asset hashes. The root variant was finalized from the preceding verified candidate; latest subpath adds PWA description/theme metadata and uses the same renderer/editor code. These inventories do not measure cold transfer, startup or editing latency. Input reproduction and archive integrity are verified; byte-identical ZIP output across machines is not claimed.

## Actual desktop browser and HTTP behavior
The verified Node reference host served `/draw/` at port 5212 and `/` at 5213. Automated real-HTTP tests cover WASM MIME, Brotli/gzip quality negotiation, HEAD, canonical subpath redirect, scoped SPA fallback, missing-asset 404, traversal/encoded separators, write-method refusal, cache headers and CSP. Root and subpath app startups completed with clean warning/error logs.

Subpath: the real connected template created three reviewed family objects and two edges; undo removed the whole batch and redo restored it. Native download [fixture](evidence/p6-subpath-browser-board.simpledraw.json) contains v6/three nodes/two lines; actual [PNG](evidence/p6-subpath-browser-export.png) is 678×138. Root: opening the seven-tool older native fixture migrated through the existing decoder; its actual [SVG](evidence/p6-root-browser-export.svg) is byte-identical to the verified P3 glyph-outline export.

Both servers were stopped and independent HTTP probes returned ECONNREFUSED before browser reload. Both boards restored from installed offline assets/storage. An offline rectangle edit in the subpath board reached saved status and survived another offline reload; that temporary shape was then deleted, preserving the original three-node/two-edge graph. Screenshots: [subpath offline](evidence/p6-subpath-offline.png), [root offline](evidence/p6-root-offline.png).

The same-origin subpath server restarted with the latest candidate. A text draft retained its exact value while a new worker waited; Update app stayed disabled ([draft screenshot](evidence/p6-update-paused.png)). Cancelling the draft enabled update. With another editor tab still open, activation refused and reported “Close other editor tabs before updating.” ([screenshot](evidence/p6-update-blocked.png)). Closing that tab permitted activation/controller reload; three nodes/two edges restored and the new DOM theme/description confirmed the changed entry. No new draft object was committed. Final warning/error logs were empty.

Browser verification exposed an initial CSP import-map failure: HTML parsing normalizes CRLF before inline-script hashing. The finalizer now normalizes that script text and canonicalizes the entry, with a Windows-line-ending regression test. The verified policy permits self-hosted resources, the required WASM compilation token and exact inline-script hashes; it does not grant general JavaScript `unsafe-eval`. Inline styles support the existing HTML geometry/text UI. A full running-browser request inventory has not yet been captured; asset checks/CSP and clean logs alone are not that evidence.

## Open release gates
Hosted GitHub CI is configured with pinned actions/read-only permissions but unrun; this workspace has no app Git remote. [User guide](USER_GUIDE.md), [deployment guide](DEPLOYMENT.md) and [release checklist](RELEASE_CHECKLIST.md) are implemented; independent review is open. The [56-archive audit](evidence/p6-package-license-audit.json) retains exact metadata and embedded notice hashes. Subsequent [notice implementation](P6_NOTICES_VERIFICATION.md) delivers full source/runtime/native texts, maps actual published runtime files and verifies offline access. Independent ownership/header review remains open. This document's earlier hosting candidate and artifact hashes remain historical evidence.

Startup/edit latency benchmarks, formal PWA installation, other browser engines/physical touch devices/screen readers/actual zoom and independent owner/maintainer acceptance remain open. See the P4 matrix and master tracker. No public deployment or device certification is claimed.
