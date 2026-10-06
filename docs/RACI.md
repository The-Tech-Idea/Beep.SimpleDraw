# RACI and stage ownership
Date: 2026-10-05. Roles describe responsibilities, not staffed people or signatures.

R = performs work; A = owns the decision/outcome; C = consulted before closure; I = informed.
Roles: PO = owner/user; ENG = implementation engineer (Codex for current work); SK = Beep.Skia maintainer (owner may also hold this role); UX = product/accessibility reviewer; QA = verification role; OPS = release/hosting operator.
One A per row. If one person holds several roles, record that assignment at actual acceptance. Codex may execute ENG/QA checks but cannot fabricate owner acceptance.

| Deliverable / decision | PO | ENG | SK | UX | QA | OPS |
|---|---|---|---|---|---|---|
| Scope and success criteria | A | R | C | C | C | I |
| P1: portable app and core host repair | I | R/A | C | C | C | I |
| P1: Beep.Skia public contract review | I | R | A | I | C | I |
| P2: editing behavior and simplicity | A | R | C | C | C | I |
| P3: document integrity and offline persistence | I | R/A | C | C | C | I |
| P4: touch, keyboard, accessibility acceptance | A | R | C | C | R | I |
| P5: palettes and library scope | A | R | C | C | C | I |
| P6: release readiness evidence | I | R | C | C | A | C |
| P6: hosting and release approval | A | R | C | I | C | R |
| P7: optional collaboration activation | A | R | C | C | C | C |
| Cross-repo package ownership/versioning | I | R | A | I | C | C |

## Phase review handoffs
P1: ENG supplies build/tests and browser smoke to SK; contract acceptance is a separate checkbox.
P2: QA replays create/edit/undo scenarios; PO/UX review tool simplicity.
P3: QA demonstrates corrupt-file retention, roundtrip, quota failures and offline restore.
P4: QA/UX verify mouse/touch/keyboard/browser matrix; PO accepts interaction.
P5: PO accepts only useful palettes; SK reviews extension reuse.
P6: QA provides reproducible release evidence; OPS validates hosting; PO owns ship decision.
P7: PO must explicitly commission a service; privacy/security threat model and operational ownership precede activation.

## Current authorization
The user requested the plans and implementation start and explicitly allowed updates to Beep.Skia. This authorizes local source/document changes and checks. It does not represent finished-product acceptance or authorization to publish a service.

## Default Beep.Skia catalog delivery

ENG is responsible for portable references, fixed factories, existing document/history/connector/export integration, source pins and tests. PO is accountable for useful default palette scope; SK is consulted on shared identity/font/export contracts; QA verifies browser/device behavior and OPS verifies hosting. The user explicitly authorized this implementation and sibling updates. Local engineering verification is recorded in [the catalog delivery](DEFAULT_SKIA_CATALOG.md); independent product/maintainer acceptance remains a separate release record.


The C7 all-family native stability follow-through is implemented by ENG with SK accountable for shared port-layout contracts, PO accountable for product scope, QA consulted on published browser verification, and OPS informed of packaging changes. Actual local gates are evidence; they do not replace independent acceptance. See plans/enhancements/P5_DEFAULT_SKIA_CATALOG.md and docs/DEFAULT_SKIA_GALLERY.md.


Docked workspace follow-through: the user explicitly selected the left-tools/center-canvas/right-Beep.Skia layout. ENG implements viewport wheel behavior, shell layout and local evidence; SK is accountable for shared host delta/brush-state contracts; QA consulted on browser/device validation, OPS informed of the static release. The selected layout is authorized product direction, not a substitute for independent accessibility or release certification.


## MCP for Claude and Codex

MCP stage ownership and independent acceptance are recorded per stage in plans/enhancements/mcp/*_RACI.md. ENG implements and verifies the server/client configuration; SK owns drawing/native contracts; PO owns scope; QA owns independent acceptance; OPS receives deployment information.
