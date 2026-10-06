# P6 notices implementation and verification

Date: 2026-10-05. Task [6.B.3](../plans/enhancements/P6_RELEASE.md). Independent rights/header and guide acceptance remain open.

## Inputs and distribution

`scripts/collect-notices.py` captures full original files from exact audited archives and pinned SDK packs, plus the sibling MIT file and bundled font license. Capture refuses an existing destination, verifies archive/document hashes and records each origin. Supplemental BeepDM owner MIT and SkiaSharp.Svg 1.60.0 MIT texts were captured at exact commits. Ordinary builds use the retained inputs without network notice downloads or Python.

The manifest has 45 groups, 21 unique original texts and 504,290 original bytes. It binds archive, Web and toolchain locks and the font. Mono 10.0.12 supplies full license and vendor notices including ICU/Unicode. Emscripten 3.1.56 supplies its license, AUTHORS and compiler-rt/libunwind/libcxx/libcxxabi/musl notices. SkiaSharp's full native vendor collection is retained without trimming text. Original source/license bytes remain unchanged.

`prepare-notices.mjs verify <dotnet-root>` compares the public bundle against originals and installed SDK sources before restore. `release-notices.mjs` checks all 24 published notice files, offline SRI and exact font/license bytes. Fingerprinted runtime payloads map to resolved NuGet runtime items, exact SDK assembly inputs or reviewed source projects. CoreLib is checked against the SDK native-directory assembly and RuntimeList entry. Unknown assemblies/content owners fail packaging; a `System` prefix never grants attribution.

Private release `runtime-notices.json` records 101 actual framework/content/font files, byte lengths and SHA256. Twenty-seven groups map to these files; 18 additional candidate groups are explicitly conservative. This is a file/source map, not function-level native link analysis. Provenance pins its report and notice input digest. Public `licenses/` contains the page, full aggregate, metadata and original texts, compressed and cached through the existing release pipeline.

## Verification

Six notice tests exercise changed/missing texts, input-lock drift, SDK/package/source distinctions, unknown sources, required offline integrity and changed licensed font bytes. The first attempted full build passed 173 C# + 57 JS tests and native publish, then correctly stopped before packaging at missing CoreLib attribution. Its failure log remains in `artifacts/p6-notices/runtime-notices.log`. The verified mapping subsequently succeeds against that actual publish; no permissive prefix fallback was introduced. Final complete packaging evidence is recorded below.

Desktop IAB Chromium, 1280×720, `/draw/`, port 5220: actual English Help link has the scoped relative URL, `target=_blank`, `rel=noopener`; Arabic Help has its translated label and identical URL. The IAB click focused the link without exposing a new tab, so new-tab launch is not claimed. The observed URL was opened directly for page verification. The page has all 45 semantic sections under the artifact CSP, with no warning/error logs.

After online reload/worker installation, the owned server was stopped. The notices page reloaded, its Mono license link rendered the full original text, back returned to the index, and the full aggregate URL rendered offline. The editor restored an actually created rectangle from autosave. Download-anchor click produced no captured download event or matching Downloads file in IAB; download completion remains unverified. Offline file access and published-byte integrity are verified separately.

Screenshot: [published notices](evidence/p6-notices-page.png). Candidate build/runtime reports and retained evidence document the final gate.

After restarting the same port with the fully packaged verified folder, the actual native v4 seven-tool fixture opens through the file chooser and migrates to the current scene. Fit drawing paints all seven objects; autosave settles and no app warning/error logs are recorded. [Current editor preview](evidence/p6-notices-editor-preview.png) retains this normal application view.

Final `artifacts/p6-notices-verified` gate: 173 C# + 57 JavaScript tests passed; Release build and native-linked trimmed publish passed with zero build warnings/errors. All 399 ZIP entries compare exactly with `wwwroot`. Offline version `1f8c7e239ffa40e6`, 133 offline assets, identity 29,888,233 bytes, Brotli inventory 8,138,132 bytes, gzip 10,752,620 bytes. ZIP SHA256 `bfe3d2f08b693e4ab2c0082fd9945e18dfd864b71214a125a439ac4c8fe7fcf1`. Sibling source digest remains `4480b3d6900729991876b5fd315c46f1ff229006bd54e951651fd9eb5f23bbe1`. The 399 published payload/encoding bytes equal the browser-tested candidate. Full aggregate length is 523,698 bytes; browser text decoding normalizes whitespace/line endings and is not used as a byte hash.

Retained [runtime/source mapping](evidence/p6-notices-runtime.json), [asset report](evidence/p6-notices-assets.json), [provenance](evidence/p6-notices-provenance.json), [browser observations](evidence/p6-notices-browser.json) and [publish equivalence](evidence/p6-notices-publish-equivalence.json) support this scope. Original candidate failure is retained alongside the successful build logs; it was an attribution refusal, not a native publish failure.

## Deliberate updates and remaining review

Ordinary builds only verify retained generated files. For a dependency/license change, review exact package/source/toolchain versions and full texts, capture into a new absent directory, and review manifest/material differences. Preserve the old bundle before replacing it. Generate into an absent `wwwroot/licenses` directory, rerun tests/full build, and exercise the actual page/offline navigation. Refresh is never automatic in CI.

The supplied Beep.Skia MIT header remains `[year] [fullname]`; owner correction requires verified ownership information. Vis.Modules' recorded upstream repository is unavailable; its exact archive declares MIT with owner metadata, and the shared owner MIT text has a verified BeepDM source. These remain explicit for independent rights/header review. No ownership, years, sign-offs or legal clearance have been inferred. Independent user/operations/build guide review, devices, public hosting and release acceptance remain separate gates.

## Offline byte-download follow-up — 2026-10-06

The [new notice download gate](P6_OFFLINE_NOTICES.md) verifies actual directly controlled IAB online and stopped-server reload downloads with exact 523,698-byte files. The generated page uses the existing byte-download helper and visible pending/failure/retry status; original texts/metadata/aggregate remain unchanged. Fresh native release passes 289 C# + 77 JS, 135 offline assets/405 ZIP entries. Actual Help popup navigation, update refusal/activation and normal/offline native restoration pass. Separate popup download completion and independent rights/header/guide/device/host/CI/install/acceptance remain open. Earlier entries retain their historical captures.
