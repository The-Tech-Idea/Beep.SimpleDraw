# Stage 3: Reliable local documents and exports — SDD
Date: 2026-10-05. Status: implemented with automated and desktop browser evidence; see master tracker for remaining release/install gates.
Estimate: 4–6 engineer-days. Entry dependencies: P1 envelope; P2 tool schemas for full roundtrip.

## 0. Problem statement
Browser storage adapter persists a versioned envelope after a debounce, with revision-aware completion and explicit dirty/error states. Initial small documents may use localStorage; move to IndexedDB through the same storage boundary when adding assets/multiple boards. Keep corrupt bytes for download; do not autosave an empty board over them. Use a writer lease/storage event to prevent two tabs silently overwriting. Render PNG/SVG through the shared export renderer with dimension/pixel budgets. Update production cache atomically; new worker activation must not interrupt a dirty edit.

## 1. Goals and acceptance criteria
1. Autosave committed changes and restore the latest successful revision.
2. Recover safely from corruption, quota denial and a second tab.
3. Export native JSON, PNG and SVG with content bounds and no adorners.
4. Install published PWA and reload offline.

## 2. Design
Browser storage adapter persists a versioned envelope after a debounce, with revision-aware completion and explicit dirty/error states. Initial small documents may use localStorage; move to IndexedDB through the same storage boundary when adding assets/multiple boards. Keep corrupt bytes for download; do not autosave an empty board over them. Use a writer lease/storage event to prevent two tabs silently overwriting. Render PNG/SVG through the shared export renderer with dimension/pixel budgets. Update production cache atomically; new worker activation must not interrupt a dirty edit.
Implementation uses the [architecture contract](../../docs/SDD.md) and the active Beep.Skia scene. Proposed new signatures below are planned contracts until implemented; audited existing methods are enumerated in the global SDD.

## 3. API surface
Implemented BrowserDocumentStore.ConnectAsync/SaveAsync/RetryAsync/GuardAsync over documents.js. The module reports loaded/empty/saved/failed/recovery/conflict/unavailable results. It holds a Web Lock for the editor lifetime, compares original bytes before each synchronous atomic localStorage setItem and retains unreadable bytes. Without Web Locks, the user explicitly enters file-only editing. AutosaveState owns revision acknowledgments; it is not a second scene or history.
Storage key: beep.simpledraw:<base pathname>:document:1; record {version:1, revision, document:<strict native JSON>}, capped at 3 MiB. Native extension .simpledraw.json, v4 with Beep DTO schema 3. Readable snapshots can be opened as file-only copies by competing tabs; they never write local storage. A dirty tab must download its edits before retrying and reloading the writer.
DrawingExport.Layout/Png/Svg use existing RenderingHelper.DrawForExport, content bounds with arrowheads/stroke/rough margin, scales 0.25–4 and 8192-per-side/16-million-pixel limits. UI exposes 0.5/1/2/4 scale and background choice. Skia SVG renders text as glyph outlines, avoiding external font references. No server API.

## 4. Files to change
| Action | File | Estimated lines | Risk |
|---|---|---|---|
| New | src/Beep.SimpleDraw.Web/Services/BrowserDocumentStore.cs | ~130 | high |
| New | src/Beep.SimpleDraw.Web/wwwroot/js/documents.js | ~120 | high |
| Modify | src/Beep.SimpleDraw.Core/SimpleDrawDocument.cs | ~80 | high |
| Modify | src/Beep.SimpleDraw.Web/wwwroot/service-worker.published.js | ~70 | high |

## 5. Error handling matrix
Corrupt/future stored file blocks replacement autosave until explicit recovery/new-board choice. Quota failure keeps dirty revision. Export oversize rejected before allocation. Conflict switches to read-only or explicit user resolution.
The active document and last successful save are preserved across failed operations. User-visible failure is part of the stage gate.

## 6. Compatibility
New application; no legacy scene compatibility layer. Preserve public Beep.Skia contracts used by other hosts. Native document changes require explicit version/migration policy, never silent substitution.

## 7. Verification
Browser tests with reload, offline, quota exception, malformed stored bytes, two writers and updates while dirty. Decode real PNG dimensions; XML-parse SVG; native roundtrip preserves every tool and membership.
Implemented harnesses: tests/documents.test.mjs, tests/service-worker.test.mjs and DocumentReliabilityTests. tests/browser-faults.html is copied only into ignored verification artifacts on a fresh synthetic origin; it is not application content. Actual browser evidence includes server-stopped offline reload/edit/restore, quota fill/failure/retry, recovery-byte download and competing writer refusal. Update/install matrix status remains explicit in the verification record.
Automated tests, actual browser evidence and owner acceptance are distinct. Update [task list](P3_DOCUMENTS.md) and master tracker with commands, date, scope and open gaps.

## 8. Risks
Browser eviction/private mode means storage is not permanent: clear local-save status and portable export. Worker assets/native linking need real release offline test. Quota/race failures need fault injection.
The worker waits by default. Explicit update activation requires a clean committed local revision and no text/gesture, then the worker requires at most one editor client. Cache keys, navigation fallback and integrity requests are base-path scoped; font bytes/licenses are cached. Formal installation and other browser/device checks remain P4/P6 release gates.

## 9. Out of scope
Cloud backups, login, remote synchronization.

## 10. Sub-task execution order
3.A.1. Add version-aware local save/restore adapter. Verify against the corresponding stage acceptance scenario before advancing.
3.A.2. Add autosave revision tracking and dirty/failure status. Verify against the corresponding stage acceptance scenario before advancing.
3.A.3. Add corrupt-file recovery and multi-tab writer coordination. Verify against the corresponding stage acceptance scenario before advancing.
3.B.1. Add bounded PNG/SVG exports through shared renderer. Verify against the corresponding stage acceptance scenario before advancing.
3.B.2. Add production PWA cache/update behavior. Verify against the corresponding stage acceptance scenario before advancing.
3.M.1. Verify corruption/quota/conflict/offline/recovery matrix. Verify against the corresponding stage acceptance scenario before advancing.

## 11. RACI and handoff
Apply [RACI](../../docs/RACI.md), stage 3 row and review handoff. ENG performs implementation; QA records actual evidence; ENG owns implementation delivery; SK reviews Beep.Skia changes; OPS is consulted for release/service operations. No review sign-off is implied by a green build.

## 12. Exit gate
All acceptance scenarios must have evidence or an explicit open task. Update the tracker honestly; never mark the entire stage complete because source files exist.
