# P5B Beep family integration audit

Date: 2026-10-05. Source audit and implementation worklist, not a completed integration gate.

## Selected scope

Use existing Beep.Skia.Flowchart ProcessNode, DecisionNode and StartEndNode as three opt-in workflow templates. Keep the first-open palette collapsed and retain the same DrawingManager, history, renderer and native envelope. These are concrete existing components, not copied sketch proxies. Other diagram families and automation execution stay outside the first library release.

## Source findings and required work

| Existing contract | Finding | Required integration |
|---|---|---|
| Beep.Skia.FlowChart.csproj | net8/net9 only | Add net10 consistently and verify retained net8 build plus trimmed WASM payload delta |
| DiagramSerialization.cs / DrawingManager.ToDto | Stable component and port IDs; general loader uses reflection and permissive properties | Explicit trusted factories and allowlisted property/line schemas at SimpleDraw boundary |
| SkiaComponent.RestoreIdentity / ConnectionPoint.SetId | Internal identity restoration | Provide a bounded trusted component/port restoration contract, without reflection or arbitrary type loading |
| NodeProperties / FlowchartControl | Label, custom colors, port counts and top/bottom-port policy already exist | Validate and persist reviewed properties loss-free; reuse the existing property container |
| Selected node renderers | Default system font and hardcoded colors ignore some custom properties | Honor existing style properties and use required bundled glyph outlines for browser/native PNG/SVG parity |
| DrawingSession editing | Selection/movement/style/group/resize state presently assumes SketchShape | Generalize supported component editing through one scene/history; do not silently drop family objects from selection or serialization |
| Library insertion | Sketch component/group IDs are remapped; connections currently rejected | Capture internal lines, reject dangling references, remap component/port identities and fresh line objects atomically and prove undo/redo |
| ConnectionLine labels/ports | Shared geometry and labels need portable renderer/crop review | Include complete stroke/arrows/ports in exports; use the same safe text path for labels |

## Gate

Native roundtrip must preserve label/style/port policy and connector endpoints. Every imported value must be validated before board mutation. Tests must prove fresh identities, connected selection duplication/insertion, one undo, rejection without damage, real PNG/SVG rendering and trimmed browser roundtrip. Record raw/Brotli payload delta against P5A. Independent maintainer/product review remains a separate gate.

## Implemented foundation and current evidence

The three existing nodes now have validated detached constructors for ordered component/port identities. FlowChart supports net10 and retains its net8 target. Existing custom colors are honored; bounded NodeStrokeWidth/LabelFontSize are persisted through NodeProperties. RequiredLabelFontFamily is trusted host configuration, using the same TypefaceCache.DrawGlyphRun as SketchShape. GroupId moved to the shared SkiaComponent contract. Connected ports remain unavailable after bounds/layout changes. Shared RenderingHelper now passes LocalClipBounds, fixing translated negative-coordinate exports and viewport culling.

FlowchartDefinitions is the reviewed fixed factory/capture contract over ComponentDto. It permits the three exact stable keys, ten exact properties, at most eight ports per direction, 128 single-line supported label characters, and positive geometry of at least 20 units per side within existing board bounds. It validates identity/count/style/name/label data before construction, uses invariant numeric strings and preserves all reviewed values. File-supplied CLR names, assembly-qualified names, unknown/typed properties, malformed port identities and unsupported glyphs are rejected. The general reflection loader is not called.

The focused suite passes 116 tests: the previous 79 app tests, nine new portable family cases, eight existing sibling flowchart simulator/code-generator regressions, and twenty strict factory cases. Tests include real raster colors, portable SVG paths/styles, required-font failure and canvas restoration, port relayout, translated world clip, culture-independent capture and complete definition roundtrip. The net8 FlowChart build passes with zero warnings/errors.

The subsequent increment connects these factories to native v5 and the collapsed UI palette, as recorded below. Strict connector policy/remapping and full connected workflows remain 5.B/5.M work; the foundation does not close P5.

## Current publish and payload inventory

artifacts/p5-family-contracts/wwwroot publishes successfully with trimming/native linking. In the actual browser on port 5204, the seven-object native fixture rendered correctly and exported SVG without warning/error logs. The downloaded p5-shared-renderer.svg has the same SHA-256 as the earlier verified P3 export: 39faa25d30014e48774b079f13a1553f58b112a7312151b093cd2984a560f347. This verifies existing browser glyph rendering after consolidation; it does not exercise the new family factories in WASM.

| Folder inventory | P5A ready | Family-contract foundation | Delta |
|---|---:|---:|---:|
| All raw files excluding .br/.gz | 28,573,672 bytes | 28,682,595 bytes | +108,923 |
| All .br files | 7,488,362 bytes | 7,517,107 bytes | +28,745 |
| FlowChart WASM asset | absent | 99,093 bytes / 26,246 Brotli | added |

These are published-folder inventories, not network transfers or startup timings. Measure the final actively used template/connector release again before closing the payload gate.

## Family-object integration and actual browser verification

Native v5 now accepts the three reviewed families, preserves ordered port IDs and rejects old-version family masquerades. V4 sketch migration changes only the envelope version. Duplicate/insertion remap every component, port and group ID before validating the combined board. Generic editing uses the existing manager/selection/history and the same scene instances; immutable ComponentEditAction family snapshots reuse the shared SketchEditAction.State for sketches. Mixed styles/grouping/resize, family pointer/keyboard movement, sweep erasing, order, labels and duplication are implemented. Family geometry is at least 20 units per side. Sketch appearance controls explicitly disable for families. Labels validate before mutation, and the inspector preserves an uncommitted label draft across unrelated refreshes.

The suite passes 131 C# tests. New cases cover all three template types, native identity/property roundtrip, mixed history/resize/grouping/styles, cancel/move/erase, fresh port/group IDs, label validation/undo, invalid old/unknown metadata without scene damage, cross-component identity collisions, real family PNG/SVG and actual browser files. JavaScript source is unchanged in this increment; the previously passing 25 checks remain applicable.

On the published build at 5205, first-open templates/library were collapsed. Process/Decision/StartEnd all rendered as real families with bundled glyph labels. Label, stroke/fill and font edits worked. Numeric resize changed 160×70 to 180×90; one undo restored both inspector dimensions and redo restored the resized node. Keyboard placement, three-node grouping, library save/download/insert, whole insertion undo/redo and pointer movement passed. Reload restored six objects; reopening the actual native download also restored six. An extra Execute property was refused without replacing the board. Actual family SVG/PNG downloaded with no app warning/error logs.

The actual files are [native board](evidence/p5-family-browser-board.simpledraw.json), [library](evidence/p5-family-browser-library.json), [SVG](evidence/p5-family-browser-export.svg) and [PNG](evidence/p5-family-browser-export.png). Native tests prove two distinct groups, 24 unique component/port IDs, matching styles/labels/geometry and image dimensions; SVG has no text/font-family/active/external content. At 320×720, scrollWidth stayed 320 and the expanded palette ended at y454, above the export control. [Narrow screenshot](evidence/p5-family-320.png).

artifacts/p5-family-editing/wwwroot contains 28,697,972 raw bytes excluding compressed variants and 7,522,628 .br bytes: +124,300 raw/+34,266 Brotli compared with P5A ready. This is the actively used family-object artifact inventory, not startup/transfer timing. The final source artifact is artifacts/p5-family-verified/wwwroot.

## Next connector work

Existing LineDto has endpoint port IDs and extensive family/runtime metadata; restrict the browser to reviewed drawing semantics and reject unsupported execution/schema/animation values. ConnectionLine currently uses platform fonts and double-centers labels; portable labels/crop bounds need the shared glyph renderer. Existing ConnectComponentsAction redo recreates a line through a generic first-port overload, so exact endpoints/style/object identity are not guaranteed. AddLine/RemoveLine and component deletion currently do not consistently settle port availability/connection metadata. Extend those existing paths and prove exact-line connect/disconnect/delete/batch undo before exposing connector creation. Library capture must include internal connections only, reject dangling imported references and remap every endpoint before insertion. The public importer still rejects all lines until those gates pass.

## Completed connector engineering increment
See [connector verification](P5_CONNECTOR_VERIFICATION.md) for native v6, reviewed static profile, exact-line/paint-order history, pre-paint port geometry, shared glyph labels/bounds, internal-edge subsets, fresh endpoint remapping, collapsed controls and actual connected browser/offline/export evidence. 162 C# and 25 JS tests pass. Final family-object publish before connectors was 28,697,460 raw / 7,522,552 Brotli bytes (port 5206), with the saved family desktop screenshot. Final connected publish is 28,762,011 / 7,538,993 bytes (port 5209). Historical connector defects listed above have been addressed; general family/desktop and independent acceptance gates remain open.
