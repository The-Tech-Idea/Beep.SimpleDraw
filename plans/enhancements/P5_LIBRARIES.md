# Stage 5: Default Beep.Skia catalog and personal diagram reuse — tasks
[SDD](P5_LIBRARIES_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: P2 selection/style and P3 safe document persistence. Estimate: 4–7 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 5.A.1 | Define local library schema and store | Automated + desktop browser verified | Strict v1 library; independent channel using existing local store; 79 C# + 25 JS suite |
| 5.A.2 | Implement selection save/insert with identity remapping | Automated + desktop browser verified for sketch and connected family entries | Fresh component/group IDs, atomic insertion/merge, one board undo; actual files in docs/evidence |
| 5.B.1 | Select minimal Beep families and test WASM payload | Automated + desktop browser verified for three reviewed nodes and connectors | Real Process/Decision/StartEnd WASM editing, native v6, strict directional endpoints, shared exports; 162 C# + 25 JS suite; final payload delta recorded |
| 5.B.2 | Register reviewed templates and collapsed palette | Implemented and desktop verified for templates and connectors | Collapsed first-open palette, generic family/sketch selection/history/styles/groups/resize/duplicate, validated label editing; unknown properties refused |
| 5.M.1 | Verify family roundtrip and simple-first UX | Engineering workflows and library integrity desktop verified; external gates open | Actual native/library/PNG/SVG, connected insertion/history/export/offline, 320px palette; actual library quota/pending download/retry/reload and corrupt/future payload exact-byte recovery passed on fresh origins 5229/5230; independent acceptance pending |

## Verification scenarios
Selection-library-document roundtrip, fresh identity and connector remapping, family properties/history, browser trimming and incremental payload. PO checks that first-open UI remains simple.

## Closure record
Implementation: P5A delivered; P5B family and connector engineering workflows are verified in the later record below. Release build/publish passed with zero build warnings/errors. 79 C# and 25 JavaScript tests passed. Published desktop evidence covers save selection, insert, atomic undo/redo, file download/import, invalid future-version rejection without mutation, merge duplicate names, removal/undo with focus return, reload, independent writer/file-only copy, and server-stopped offline insertion/reload. Final preview: artifacts/p5-library-ready/wwwroot, port 5203. Independent review/owner acceptance: pending.

Actual downloaded [library](../../docs/evidence/p5-browser-library.json) and [inserted board](../../docs/evidence/p5-browser-insertion.simpledraw.json) are regression fixtures. [Library verification](../../docs/P5_LIBRARY_VERIFICATION.md) records the scope and remaining gates.

## Next action
P5 engineering evidence is recorded in [connector verification](../../docs/P5_CONNECTOR_VERIFICATION.md) and [library verification](../../docs/P5_LIBRARY_VERIFICATION.md). The later desktop integrity increment closes actual library quota/pending download/retry/reload and corrupt/future-payload recovery using the unchanged P6 release; 70 JS checks pass. Continue P6 mixed performance and the external usability/deployment matrix; independent product/maintainer acceptance remains open.
