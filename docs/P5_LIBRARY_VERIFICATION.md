# P5A library verification

Date: 2026-10-05. Engineering evidence only. The first record below is the historical P5A snapshot; later family/connector evidence and the library integrity closure follow. Independent acceptance remains open.

The release solution build completed with zero warnings/errors. The focused C# suite passed 79 tests, and the four Node harnesses passed 25 checks. Publish produced the trimmed/native-linked static app in artifacts/p5-library-ready/wwwroot, verified on port 5203. The current preview at 127.0.0.1:5204 uses artifacts/p5-family-contracts/wwwroot and includes the subsequent shared-contract foundation; its 116-test suite and library import/insert/undo smoke also pass.

## Actual published desktop scenarios

| Scenario | Evidence / outcome |
|---|---|
| Capture all seven sketch kinds | Imported actual P2 native fixture, selected seven objects, saved All tools; board unchanged |
| Insert and history | Seven became fourteen, one undo restored seven, redo restored fourteen; fresh identities/groups verified against actual downloaded files |
| Portable files | Downloaded library and fourteen-object board retained as p5-browser-library.json and p5-browser-insertion.simpledraw.json; native regression tests parse both |
| Reload | Board and independent library restored; library initially collapsed |
| Import/merge | Actual library imported; future library version refused without changing library/board; repeated import merged with fresh entry IDs and unique ordinal UI labels |
| Remove and undo | Removal persisted; undo restored entry/order; final build focused Undo removal after removal and summary after undo |
| Competing writers | Second tab chose file-only drawing; library snapshot was read-only while writer held its independent lock; insertion increased only the copy to 21 objects |
| Offline | Preview server stopped with authoritative process exit; cached app reloaded board/library and inserted an entry offline; one undo restored board |
| Narrow screen | Final 320×720 viewport had scrollWidth 320; expanded side panel ended at y504, export began at y510; scrolled summary remained y184–228 and collapsed through keyboard |
| Final preview | Import/insert/fit at 5203; no warning/error logs in final smoke |

## Boundaries

At the P5A snapshot, the store factory's automated tests covered quota/recovery/CAS and independent channel cleanup; actual library integrity UI was still open. That UI gate is now closed within the desktop scope below. Family/connector engineering evidence is recorded in [connector verification](P5_CONNECTOR_VERIFICATION.md). Physical touch devices, screen readers and other browser engines remain in the P4 matrix. Independent acceptance remains open.

Screenshots: [desktop](evidence/p5-library-desktop.png), [320px expanded](evidence/p5-library-320.png), [320px collapsed](evidence/p5-library-320-collapsed.png), [server-stopped offline](evidence/p5-library-offline.png).

## Actual library integrity UI closure

The unchanged `artifacts/p6-network-profile/publish/wwwroot` was served on fresh loopback origins 5229 and 5230 using `tests/serve-browser-faults.mjs`. The wrapper delegates `/draw/` to the verified static host and serves visible storage controls only under `/__owned-tests/storage/`, outside the app worker scope. Neither fault controls nor their scripts are shipped. CUA operated actual buttons; storage was read through visible test-owned readouts rather than hidden browser evaluation. App warnings/errors were empty in both scenarios.

| Scenario | Actual result |
|---|---|
| Unreadable stored library | `{owned-test-corrupt` made library editing read-only. Original bytes remained stored; the native recovery download preserved all 19 bytes exactly. A real keyboard-created rectangle still saved through the separate drawing channel. |
| Explicit recovery replacement | Start new local library saved an empty library and enabled editing; a captured rectangle selection then saved the baseline entry at revision 1. |
| Real localStorage quota | Owned padding filled 5,239,624 characters on 5229. Saving a second selection displayed the real browser quota exception and Retry library save, retained both entries in memory, and left the complete revision-1 library bytes unchanged. Another retry while full remained failed. |
| Portable pending work | Download library completed as a native 2,932-byte file containing both entries. The local retry control remained present; downloading did not acknowledge a local save. |
| Release and retry | Releasing only owned padding and Retry library save saved revision 2. Its document was byte-identical to the pending download. Reload restored both named entries and the one-object drawing; the retry control disappeared. Drawing saved bytes were identical before/after library failure. |
| Readable envelope, future library schema | A valid version-1 store envelope at revision 7 contained library version 99. The C# parser showed read-only recovery; stored and downloaded envelope bytes matched exactly. Explicit replacement saved current version-1 empty library at revision 0. |

[Retained evidence summary and hashes](evidence/p5-library-recovery-summary.json), [corrupt UI](evidence/p5-library-corrupt-ui.png), [quota failure](evidence/p5-library-quota-failure.png), and [restored entries](evidence/p5-library-quota-restored-entries.png) record the result. Run `node tests/verify-library-browser-evidence.mjs` to revalidate captured byte equality, entry names, revisions, visible recovery/quota/reload states and clean logs. All 70 JavaScript checks pass. Runtime inventory remains 134 offline assets / 8,144,726 Brotli bytes; pinned sibling source remains `b80560e6fcddb0ba973287babfe178312819b5e19de0e94b421a4c5356f46568`. No production source changed or new C# build was needed in this integrity increment; the current release's 188 C# result remains the previous full gate.

This closes library quota and corrupt/future-payload recovery UI on IAB Chromium desktop. Physical devices, other engines, independent product/maintainer acceptance and broad sibling regressions remain open.
