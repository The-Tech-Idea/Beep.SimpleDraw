# Stage 7: Optional collaboration after local app acceptance — SDD
Date: 2026-10-05. Status: planned; see master tracker for verified progress.
Estimate: 8–15 engineer-days. Entry dependencies: P6 accepted local release; explicit commissioning by owner.

## 0. Problem statement
This phase is deferred by default. Evaluate Beep.Skia.Collaboration contracts before proposing a protocol. Document stable element identity, ordering, operation IDs, revisions, deletion tombstones and undo ownership. Define an optional ASP.NET Core service with room authorization, WebSocket quotas, expiring read-only links and deletion/retention policy. Encryption requires a reviewed threat model and key lifecycle; do not label transport TLS as end-to-end encryption. Remote operations use the same scene/command boundary, not a parallel editor.

## 1. Goals and acceptance criteria
1. Decide whether remote rooms are needed without burdening local editing.
2. If commissioned, converge concurrent edits and enforce access/expiry limits.
3. Keep local drawing usable when the collaboration service is unavailable.

## 2. Design
This phase is deferred by default. Evaluate Beep.Skia.Collaboration contracts before proposing a protocol. Document stable element identity, ordering, operation IDs, revisions, deletion tombstones and undo ownership. Define an optional ASP.NET Core service with room authorization, WebSocket quotas, expiring read-only links and deletion/retention policy. Encryption requires a reviewed threat model and key lifecycle; do not label transport TLS as end-to-end encryption. Remote operations use the same scene/command boundary, not a parallel editor.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
Future protocol must specify join authorization, initial snapshot version, operation acknowledgements, reconnect cursor, conflict policy and rate limits. Endpoint schemas intentionally not invented before collaboration audit.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| New | docs/decisions/002-collaboration-protocol.md | ~200 | high |
| Modify | src/Beep.SimpleDraw.Core/DrawingSession.cs | ~100 | high |
| New | src/Beep.SimpleDraw.Collaboration/ (only if commissioned) | TBD | high |

## 5. Error handling matrix
Disconnect shows local-only mode and preserves edits; unauthorized room rejects without state disclosure; invalid/conflicting operations do not mutate the scene; queues have bounded growth.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Concurrent replica/reconnect/tombstone/undo tests, authorization and quota tests, security review and load test. Verify no service requests in default local mode.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P7_COLLABORATION.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Collaborative undo and key management significantly increase product complexity; keep the phase optional and require operator ownership. Existing Beep collaboration source must be audited before implementation.

## 9. Out of scope
MVP requirement, automatic activation, authentication imposed on local users.

## 10. Sub-task execution order
7.A.1. Owner commissions or declines service scope. Verify against the corresponding stage acceptance scenario before advancing.
7.A.2. Audit Beep collaboration and decide protocol/threat model. Verify against the corresponding stage acceptance scenario before advancing.
7.B.1. Implement optional rooms, access and bounded synchronization. Verify against the corresponding stage acceptance scenario before advancing.
7.B.2. Implement reconnect/conflict/retention and reviewed sharing. Verify against the corresponding stage acceptance scenario before advancing.
7.M.1. Run convergence/security/operations acceptance. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 7 row and review handoff. ENG performs implementation; QA records actual evidence; PO owns product/scope acceptance; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.
