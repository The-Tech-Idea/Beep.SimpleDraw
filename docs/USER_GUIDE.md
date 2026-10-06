# SimpleDraw user guide
SimpleDraw uses a docked Beep workspace: tools and styling on the left, an unobstructed canvas in the center, and Beep.Skia libraries, properties and export on the right. On narrow screens the rails stack around the canvas.

SimpleDraw keeps drawings on your device. Save drawing downloads an editable file you can move to another browser or computer. Export image downloads PNG/SVG for sharing. Browser autosave is convenient but can be cleared or evicted; keep native backups of work you care about.

## Draw and edit
Choose a tool, then drag on the canvas. Pen follows your stroke; eraser removes objects touched by its sweep. Text opens an editor: Done or Ctrl+Enter commits; Escape cancels. Double-click text, or select it and use Edit text, to change it. Unsupported font characters report a validation error instead of being replaced silently.

| Tool | Shortcut |
|---|---|
| Select | V |
| Hand/pan | H; middle mouse also pans |
| Rectangle / ellipse / diamond | R / O / D |
| Line / arrow / pen | L / A / P |
| Text / eraser | T / E |

In Select, click an object and drag to move it. Shift-click adds/removes objects; the Objects list offers the same explicit selection. Grouped objects select and move together. Drag resize handles, or use Width/Height in Style. Line/arrow handles change endpoints. Style applies to selected objects in Select mode and sets defaults for new objects in a drawing mode. Mixed selections may show no single chosen swatch. Smooth/Sketch changes the seeded sketch appearance; repainting does not randomly change it.

Undo/redo restores committed operations, including an entire eraser sweep or template insertion. Delete removes the selection. Ctrl+D duplicates with fresh identities; Ctrl+G groups and Ctrl+Shift+G ungroups. Inspector controls change paint order. On macOS use Command for the usual control shortcuts. Ctrl+Z undoes; Ctrl+Shift+Z redoes. Arrow keys move a selection by one drawing unit, Shift by ten. Escape cancels a gesture.

## Move around and use the keyboard
Scroll zooms around the pointer. Hand/middle mouse pans; Fit drawing brings existing objects into view. Two touches on the canvas cancel the current preview and then pan/zoom around their midpoint; lift both before drawing again. Active pen input ignores incidental touch. Physical touch-device acceptance is still open; see [verification scope](P4_TEST_MATRIX.md).

For keyboard drawing, press a tool shortcut to focus the canvas. Enter starts a shape; arrows move its cursor by five CSS pixels, Shift by twenty; Enter finishes. This works for rectangle, ellipse, diamond, line, arrow and pen. T then Enter opens text. Native fields/buttons retain their own editing keys. Tab reaches named controls and the Objects list. Dialogs contain focus; Escape cancels text/help/replacement and returns focus. The language button changes English/Arabic interface labels and direction; it does not translate drawing text. Some diagnostics and editorial review remain open.

On narrow screens tools wrap, Style starts collapsed and inspector content scrolls. Beep.Skia libraries starts open; choose a family and search its shapes. Connectors, Library and Export image are collapsed panels you can open when needed.

## Beep.Skia components and connectors
The default Beep.Skia libraries panel embeds 249 components across 16 diagram families. Choose a family, search within it and click a shape to insert it. Select an inserted component to change its named properties, dimensions and supported styles. Apply property is undoable; Undo/Redo also refreshes its value field. Switching interface language preserves an unfinished property draft. Complex property values use JSON and invalid values leave the component unchanged. Some family components draw panels or charts; insertion does not provide an external data source or execute their domain workflow.

FlowChart includes the reviewed process, decision and start/end nodes. Connected workflow inserts three nodes with two connectors as one undoable operation. The [catalog guide](DEFAULT_SKIA_CATALOG.md) lists families and verification scope.

Open Connectors, choose From node/output port and To node/input port, then choose a label and Straight/Elbow/Curve route. Connect nodes becomes available only for distinct nodes and available directional ports. Each port accepts one connector. Remove connector is undoable; moving nodes keeps their endpoints attached. These are visual workflow diagrams; the app does not execute them. Connector strokes are 2 px; labels use the bundled 14 px font, up to 128 single-line characters.

## Reuse a library
Select useful objects, open Library, enter an entry name and choose Save selection. Insert adds a fresh copy; Undo removes the whole insertion. Internal connectors between selected nodes are retained; edges to objects outside the selection are omitted. Removing an entry does not remove the drawing; Undo removal restores that library entry.

Download library keeps an editable portable copy. Import library validates and merges entries; it does not replace the board. Libraries have their own saving state/writer. If changes are read-only, close the other library writer and retry, or use a file-only library and download it. File-only changes do not silently overwrite browser storage.

## Files, saving and errors
Committed edits autosave after a short delay. Wait for saved status; pending/error means your latest edits are not safely stored yet. Save drawing downloads `.simpledraw.json`. Open validates a native file before replacing your drawing; New/Open offer to preserve unsaved work. PNG/SVG do not reopen as editable objects, and `.excalidraw` import is not supported.

Native drawings are limited to 2 MiB/2,000 objects; pen strokes have a 4,096-sample limit. Unsupported versions/types/properties or invalid geometry are refused while keeping the current scene. Library files are limited to 1 MiB/100 entries; each entry to 256 KiB/100 objects. Exports that exceed the safe allocation limit are rejected before rendering. Error messages describe the refused operation.

The same origin/base path has one local drawing writer. Another tab can close/retry or work on a file-only snapshot; it cannot overwrite that writer. If storage is full/unavailable, download the native drawing first, resolve the storage issue and retry. If saved bytes are unreadable, download them before explicitly replacing the recovery record. A file-only board must be downloaded to keep changes.

Export image offers PNG/SVG, scale and paper/transparent background. SVG includes outlined text and needs no installed font; keep native JSON if you need to edit text later.

Keyboard shortcuts also links to Software notices. This page lists included software and font attributions, full original license texts and a combined notice collection. Its files remain available offline after the app's offline cache installs. Download all software notices saves the complete original collection; Read all software notices opens it for reading. If retrieval fails, the page reports the failure and lets you retry. The notice texts retain their original language.

## Offline use and updates
Offline use needs a successful online load and completed offline-cache installation first. Serve the app over HTTPS or development loopback HTTP; opening an HTML file directly is unsupported. Desktop server-stopped reload/edit/save and guarded updates are verified; formal installation and the browser/device matrix remain open.

When Update app appears, finish/cancel drafts and gestures, wait for saved status and close other editor tabs. Activate the update; the app reloads and restores the drawing. Keep a native backup before changing origin, hosting path or clearing browser storage: those actions do not carry autosave/library records to the new location.


## Scroll and zoom

Scroll the mouse wheel or trackpad over the canvas to pan. Shift+wheel pans horizontally when the device supplies vertical wheel motion. Ctrl/Cmd+wheel zooms around the pointer. A trackpad can pan both axes directly. The hand tool and middle mouse also pan; zoom buttons and Fit drawing remain available. Viewport navigation does not edit object geometry or add an undo entry. Scrolling over a dock scrolls that dock instead of changing the canvas.
