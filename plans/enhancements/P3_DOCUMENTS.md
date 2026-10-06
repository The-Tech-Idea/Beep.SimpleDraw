# Stage 3: Reliable local documents and exports — tasks
[SDD](P3_DOCUMENTS_DESIGN.md) · [Master tracker](MASTER_TRACKER.md) · [RACI](../../docs/RACI.md)

Entry: P1 envelope; P2 tool schemas for full roundtrip. Estimate: 4–6 engineer-days.

| ID | Task | Status | Evidence |
|---|---|---|---|
| 3.A.1 | Add version-aware local save/restore adapter | Automated + desktop browser verified | Strict native v4 inside storage record v1; saved seven-object board restored after reload |
| 3.A.2 | Add autosave revision tracking and dirty/failure status | Automated + desktop browser verified | Late revision acknowledgment test; real quota failure retained seven in-memory objects and previous bytes; retry succeeded |
| 3.A.3 | Add corrupt-file recovery and multi-tab writer coordination | Automated + desktop browser verified | Web Locks and CAS; second writer refused; actual corrupt bytes downloaded intact before explicit replacement |
| 3.B.1 | Add bounded PNG/SVG exports through shared renderer | Automated + desktop browser verified | Real WASM downloads decode/XML-parse; limits, crop, background and adorner exclusion tested |
| 3.B.2 | Add production PWA cache/update behavior | Automated + desktop browser verified | Server-stopped reload/edit/autosave/restore; updates refused with another client, disabled during text edit, activated only after cancellation and preserved eight objects |
| 3.M.1 | Verify corruption/quota/conflict/offline/recovery matrix | Desktop engineering verified; formal installation open | Actual recovery/quota/writers/file-only/export/offline/update evidence; P4/P6 carry formal installation and broader device/engine checks |

## Verification scenarios
Browser tests with reload, offline, quota exception, malformed stored bytes, two writers and updates while dirty. Decode real PNG dimensions; XML-parse SVG; native roundtrip preserves every tool and membership.

## Closure record
Implementation: all P3 source tasks implemented. Automated evidence: 56 focused C# and 17 Node checks pass; retained net8 host builds with 0 warnings/errors. Browser evidence: published recovery/quota/writers/file-only/export/reload/offline/update workflow, recorded in docs/VERIFICATION.md. Final glyph-path export passed actual WASM smoke with no warning/error logs. Formal installation and independent review/owner acceptance remain pending.

## Next action
Continue P4 usability through P6 release gates, including formal installation and other browser/device verification.
