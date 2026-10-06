# Release readiness checklist
Date: 2026-10-06. Candidate evidence is not release approval. [Master tracker](../plans/enhancements/MASTER_TRACKER.md) is authoritative; [RACI](RACI.md) identifies decision roles.


## Selected local acceptance environment — 2026-10-06

The user answered the remaining host/device environment question with **“local embedded chromium”**. This supersedes earlier resource-blocked status records for implementation delivery. P1–P6 implementation and the authorized docked/default-library/MCP additions are delivered and verified in that selected environment. Public plugin publication and optional P7 collaboration remain explicitly deferred.

Current immutable candidate: `artifacts/simpledraw-v1-final`; preview: `http://127.0.0.1:5243/draw/`. Current proof: 585 C# and 79 JavaScript checks; all 151 offline assets/453 ZIP entries; 249 embedded components; actual offline native exports; update preserving the user's ten objects; local startup 2,032.6 ms; repeated mixed-1,000-object p95 25.6/26.2 ms. See [final delivery](FINAL_DELIVERY.md) and its raw evidence. This is local Chromium proof, not measured public broadband or a cold OS process.

Other engines, physical touch/screen readers/high-DPI devices, public HTTPS/OS PWA installation and hosted CI are unverified future validation, outside the user's selected environment. Formal independent role signatures remain unsigned and are not inferred from environment selection. No feature requirement or portability architecture was removed; implementation completion is distinct from future production certification. Historical blocked audits below retain their original context and no longer define the current delivery status.


## Current evidence
| Gate | Status | Evidence/remaining action |
|---|---|---|
| Declared/pinned SDK, workloads, source and packages | Verified locally | [Build contract](BUILD_REPRODUCIBILITY.md); 498 source inputs/nineteen portable sibling locks/56 archive pins |
| Fresh source/isolated-feed restore and audited cache | Verified locally | [P6 evidence](P6_BUILD_HOST_VERIFICATION.md); 1,106 extracted files and tamper refusals |
| Focused build/tests/native Release | Verified locally | [Consolidated candidate](FINAL_DELIVERY.md): 585 C# + 79 JS, zero build/publish warnings/errors; fresh Web publish intermediates and native-Skia selection checked |
| Offline hashes/SRI/encodings/font/ZIP integrity | Verified locally | Consolidated candidate: 151 offline assets and 453 exact archive entries; [actual update/normal/offline native bytes verified](P6_OFFLINE_NOTICES.md) |
| Whole-folder Brotli budget | Verified inventory | 8,457,280 bytes <25 MiB; prior controlled model separately records observed body bytes |
| Root/subpath CSP, exports, offline edit/reload, guarded update | Desktop verified | Actual stopped-server and same-origin update evidence; public-host/browser matrix open |
| Startup and p95 edit-to-paint | In progress | Prior rectangle p95 39.4/35.7 ms passes; final consolidated mixed p95 25.6/26.2 ms passes 50. [Current raw evidence](FINAL_DELIVERY.md). [Production session overlays](P6_SESSION_OVERLAYS.md) add 48 cases / 528 exact frames with actual responsive/native gates. Controlled startup passes at 2.96–3.13 s; physical-provider/public-host/cold-process open |
| 100/1,000-object cases, 2,000-native boundary and 5,000 renderer stress | Desktop evidence recorded | Actual cases and 2,000 cap verified; separate 5,000 rectangle/mixed shared-renderer runs complete 140 moves, restored geometry and product isolation; [protocol](P6_PERFORMANCE.md) |
| Complete dependency/native/runtime notices | Engineering distribution verified; review open | [Notice evidence](P6_NOTICES_VERIFICATION.md): 24 delivered files, all 117 current runtime payloads mapped, stopped-server page/text access; independent ownership/header review and separate popup completion open; [directly controlled IAB online/offline exact downloads pass](P6_OFFLINE_NOTICES.md) |
| User/operations/build guidance | Implemented | [User guide](USER_GUIDE.md), [deployment](DEPLOYMENT.md), [build](BUILD_REPRODUCIBILITY.md); independent review open |
| Hosted CI | Configured, unrun | Pinned read-only workflow; app has no Git repository/remote in this workspace |
| Intended public HTTPS host + formal PWA install/update | Open | Operator must record actual host/browser/install results; no public deployment authorized/claimed |
| Physical touch, screen readers, actual zoom/high-DPI, other engines | Open | [P4 matrix](P4_TEST_MATRIX.md) |
| Library-specific quota/corruption UI | Desktop verified | [Library integrity evidence](P5_LIBRARY_VERIFICATION.md): actual quota failure/pending download/retry/reload, corrupt/future payload exact-byte recovery and independent drawing preservation |
| Complete existing sibling test-project regression | Automated verified | [Current expanded-catalog sibling regression](DEFAULT_SKIA_CATALOG.md): 504 passed, zero failed/skipped; ordinary target graph built with warnings-as-errors; net8 host build also passes. Manual desktop/device checks remain separate |
| Owner/maintainer/QA/operator acceptance | Unsigned | Roles and artifacts below; do not infer acceptance from green checks |

## Before a release decision
Record candidate path/base/version, source/package/toolchain pins, archive SHA256 and immutable evidence. Complete performance/notices/device/host gates or record a concrete accepted exception by the accountable owner; an empty checkbox is not acceptance. Verify the intended host's MIME/cache/compression/CSP and download/offline/update behavior with that exact candidate. Retain a previous schema-compatible candidate and native/library backup procedure.

## Acceptance record
| Role | Required review | Person/date/result |
|---|---|---|
| ENG | Implementation, build/provenance and known limitations | Focused implementation evidence recorded; independent sign-off pending |
| SK | Shared contracts/default targets/history/rendering and cross-repo distribution | Unassigned / unsigned |
| QA/UX | Actual matrix, performance, recovery and usability evidence | Independent reviewer unassigned / unsigned |
| OPS | Intended hosting, promotion/update/rollback and operational guide | Unassigned / unsigned |
| PO | Scope and release approval; explicit commissioning for optional collaboration | Unsigned |

P7 collaboration remains deferred and needs explicit commissioning. Local implementation authorization already covers ongoing P1–P6 work; these records are release/acceptance gates, not requests to pause engineering.

## Current execution resources — 2026-10-06

The [live surface inventory](evidence/p6-current-execution-surfaces.json) exposes embedded Chromium and the app panel, with no native app/device control. A requested Edge tab returned `Browser is not available: edge`. This environment cannot execute actual Android/iOS touch, NVDA/VoiceOver, other-engine, real browser zoom/device-scale or OS-setting acceptance. Viewport reflow and native raster transforms remain separate evidence. The workspace has no app `.git` directory or configured app remote; hosted workflow execution is not proved by its local YAML or build. No intended HTTPS host/operator or independent review assignments have been supplied.

The pending resource request asks which devices/browsers/screen readers/host can be provided and who will perform owner/Beep.Skia acceptance. No response or sign-off has been received. The unsigned rows and external gates above remain open; implementation authorization does not substitute for finished-product acceptance. The current portable artifact and verified local preview remain available for that handoff.

Historical status: three consecutive resource audits found the same unavailable inputs, and the full goal was marked blocked. It has since resumed. Three consecutive resumed resource audits found no new connected resources or reviewer/operator results; the goal is blocked again pending those inputs. The saved release preview was restarted after its previous process disappeared; reload restored seven objects without captured warnings/errors. [Recovery evidence](evidence/p6-resumed-preview.json) records unchanged source inputs and ZIP integrity. The goal is blocked with the full original scope; the external gates remain open and P7 remains deferred.


Latest goal-state check: three consecutive audits in the latest resumed run again found unchanged external resources and unsigned acceptance. The goal is blocked pending device/browser/screen-reader access, intended host/CI access and actual independent reviewer/operator results. The preview remains running; no new engineering, device, host or acceptance completion is claimed.


## Current catalog engineering continuation

The user explicitly requested default Beep.Skia libraries after the historical resource audits above. The 249-component catalog is implemented and locally verified; the active implementation continues through available local checks. Candidate `artifacts/default-skia-catalog-usability-final` is served at http://127.0.0.1:5243/draw/. [Current delivery](DEFAULT_SKIA_CATALOG.md) records exact source/build/browser scope. Historical goal-state paragraphs above describe earlier audits, not a request to suspend authorized engineering. Current indexed candidate passes the ordinary mixed gate at p95 41.3/25.6 ms; [fresh performance evidence](DEFAULT_SKIA_PERFORMANCE.md) replaces carried-forward timing assumptions. External certification and independent acceptance remain open.

Current catalog UI and property-history checks pass in [the usability delivery](DEFAULT_SKIA_USABILITY.md), including all-family 320px English/Arabic targets and current mixed p95 33.2 ms. Earlier timings and candidate records remain historical.


Latest C7 engineering verification: `artifacts/default-skia-catalog-port-stable` is adopted in the local preview; 568 C# / 77 JS and 504 ordinary sibling tests pass. Both original and current 249-component native galleries save byte-exactly through the corrected trimmed WASM; device reload restores 249 objects. The user preview restores ten objects after Update app. Source fingerprint `42d01696deb91a9e3e62c64fd374a0d372960e6cf68f04a68f643c0f01707fea`; 498 inputs; offline `248433f1f2308a04`; Brotli inventory 8,456,690 bytes. See docs/DEFAULT_SKIA_GALLERY.md (from repository root) for failed-before/fixed-after evidence, fixture reproduction, package hashes and verification limits. C7 local engineering checks are complete; independent device/host/release acceptance and broader plan gates remain open. Prior active/in-progress notes above are historical snapshots.


Latest docked workspace engineering delivery: final-v2 adopted at preview 5243; 585 C# / 79 JS / 504 sibling checks passed. Wheel pans, modified wheel zooms, separate docked rails and canvas-local text editing are published. Final responsive, translated-title and clipped WellLogs browser evidence plus package hashes are recorded in docs/DEFAULT_SKIA_SHELL_AND_BOUNDS.md (repository root). Local engineering delivery is verified; independent device/accessibility/host gates remain open. Earlier in-progress notes are historical.

## Consolidated local delivery - 2026-10-06

Current candidate: artifacts/simpledraw-v1-final, adopted at preview 5243/draw/. See FINAL_DELIVERY.md for current hashes, 585 C# / 79 JS gates, offline/ZIP integrity, MCP-native browser handoff, text/property history, offline editing, narrow bilingual layout and user-update restoration. Earlier counts/paths/status paragraphs are historical. The current goal is active; no resource audit or independent acceptance is inferred from this engineering delivery. Public plugin publication and optional collaboration are deferred. External acceptance remains open.

