# Default Beep.Skia libraries

User authorization: 2026-10-06, make Beep.Skia libraries default in SimpleDraw and update the sibling libraries as needed.

The default palette must expose the actual diagram components from FlowChart, UML, ERD, MindMap, Network, Business, Cloud, DFD, ECAD, ETL, ML, PM, Security, StateMachine, Quantitative and WellLogs. Desktop host controls and runtime loaders are host infrastructure, not shapes. Use fixed compiled factories, never resolve file-supplied CLR names. Preserve the existing DrawingManager, history, DiagramDto, native file envelope and personal-library path.

| Task | Gate | Status |
|---|---|---|
| C1 | Inventory every family and compile portable project references | Engineering implemented; verification below |
| C2 | Extend existing document/edit/connector/export boundaries with validated properties and identities | Engineering implemented; verification below |
| C3 | Default searchable family palette and component property editor | Implemented and verified within scope below |
| C4 | Every catalog entry: create, native roundtrip, raster/export, selection/history and library insertion | Implemented and verified within scope below |
| C5 | Native browser publish, actual palette/edit/save/reopen/offline smoke and source/distribution pins | Implemented and verified within scope below |

ENG implements and verifies; user/Beep.Skia maintainer is accountable for product/library scope. External release certification remains separately recorded and does not suspend implementation.

Design: compiled catalog descriptors retain concrete component factories and public property metadata for WASM trimming. Extend the strict SimpleDraw boundary using the existing property bags and a detached identity restoration contract. Persist bounded property values with exact key/type validation; refuse unsupported or non-roundtripping state before replacing a drawing. Shared port identities inside one component are allowed when the family exposes one physical port in both lists, but cross-component collisions are refused. Catalog rendering uses the existing bundled typeface cache. Default palette is open, grouped, searchable and keyboard accessible; personal saved libraries remain alongside it.

Implementation inventory: 250 concrete MaterialControl components audited, 249 standalone palette factories across all 16 families. NetworkLink is a zero-size edge, represented through the existing connector workflow. Nineteen sibling projects are referenced and included in solution configuration; 498 sibling source inputs and 56 package archives are pinned. Native file version 7 extends the existing envelope and preserves earlier documents. The shared DrawingContext now distinguishes export selection adorners; UML no longer includes them in exported pixels. Generic catalog edits use validated existing property bags and existing ComponentEditAction history. Coupled label/display properties are normalized before edit validation. All 249 factories pass native roundtrip, movement/resizing undo, copied identities and PNG/SVG checks; the full suite has 542 checks including cross-family connections and property/style undo. Actual corrected WASM browser verification passed for UML property editing, undo/redo, native save/reopen, server-stopped reload and ERD insertion. The original preview update restored all seven objects; only the native envelope version changed. See [verification and usage](../../docs/DEFAULT_SKIA_CATALOG.md).

## Catalog usability follow-through

C6 extends the existing P4 interface-language and responsive contracts to the default catalog. ENG implements the existing UiText resources, logical layout and bidirectional isolation; QA is responsible for actual rendered keyboard/RTL/narrow checks; PO/UX remain accountable/consulted for final editorial acceptance. Family/type/property identifiers stay invariant because they are library API names and persisted keys. Drawing content is not translated. Success messages are translated at render time so switching language updates an already-visible message.

The shape list uses one column in the existing narrow inspector. Words stay intact instead of splitting component names into single-letter fragments. No new catalog model, property store or translation service is introduced. Acceptance: actual published English and Arabic family/search/property controls, search empty state, property edit and undo, readable names at 320px and no horizontal panel overflow. Browser/device/editorial acceptance remains separately scoped.

Status: implemented and verified within the [recorded browser/build scope](../../docs/DEFAULT_SKIA_USABILITY.md).

Rendered checks found and retained two additional issues: property Undo restored canvas state but left the input stale, and the 320px all-family audit found overflow in ECADMicrocontroller and MLHyperparameter labels. The inspector now tracks the last observed committed property value and refreshes only when that value changes; language/ordinary rerenders preserve an unfinished draft. Generated display labels separate acronym prefixes, while type keys remain invariant. Technical names use bidirectional isolation and English language metadata. C6 acceptance also includes property input restoration on undo/redo and draft preservation on language switch.


## C7: published all-family native graph stability

Generate one native board containing all 249 compiled catalog factories and sixteen family boards through the actual DrawingSession insertion path. Preserve default dimensions, properties and real port identities; tile without replacing domain state. Record native hashes/counts and strict managed roundtrips. Import into an isolated published-WASM origin; check the object inventory, family rendering and captured browser diagnostics, then compare an actual downloaded native file semantically. Preserve any failed comparison as evidence.

The first published check found twelve differences in four Business nodes: ConditionBuilder lost an output, EventNode gained an input, and Group/RuleFlow lost both ports. Their LayoutPorts overrides changed topology during rendering. Remove port-count mutation from the existing Business layout overrides. Establish intended zero-port Group/RuleFlow defaults during construction; layout only places existing ports. Preserve edited/native port counts, their identities and connected state through canvas and image rendering. Extend the existing 249-case native/export suite with before/after serialization checks, plus 26 Business cases with explicitly configured library ports.

RACI: ENG responsible for source, fixtures, regression and packaging; SK accountable for shared library design, PO accountable for product scope; QA consulted for browser proof and independent device coverage; OPS informed of the updated release candidate. User-authorized local source work proceeds; these checks do not imply independent acceptance.

Acceptance: 568 C# cases and the existing 77 JS gates pass; the re-published WASM saves all 249 objects with zero semantic native differences; older native defaults remain loadable; family proofs and limitations are recorded in docs/DEFAULT_SKIA_GALLERY.md. Verification is in progress until those results are recorded.


Latest C7 engineering verification: `artifacts/default-skia-catalog-port-stable` is adopted in the local preview; 568 C# / 77 JS and 504 ordinary sibling tests pass. Both original and current 249-component native galleries save byte-exactly through the corrected trimmed WASM; device reload restores 249 objects. The user preview restores ten objects after Update app. Source fingerprint `42d01696deb91a9e3e62c64fd374a0d372960e6cf68f04a68f643c0f01707fea`; 498 inputs; offline `248433f1f2308a04`; Brotli inventory 8,456,690 bytes. See docs/DEFAULT_SKIA_GALLERY.md (from repository root) for failed-before/fixed-after evidence, fixture reproduction, package hashes and verification limits. C7 local engineering checks are complete; independent device/host/release acceptance and broader plan gates remain open. Prior active/in-progress notes above are historical snapshots.
