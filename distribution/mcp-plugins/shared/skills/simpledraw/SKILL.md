---
name: simpledraw
description: Create, inspect, edit, connect, animate, save and render diagrams using the local Beep SimpleDraw MCP tools and embedded Beep.Skia libraries, and work live on a board open in SimpleDraw.
---

Use the SimpleDraw MCP tools supplied by this plugin. Discover actual tool names from the host; it may prefix them with the plugin/server namespace.

1. Use `list_components` with pagination to discover the 249 components across 16 families. Use `describe_component` on the exact returned type key before setting its properties. The returned metadata includes types, defaults, enum choices, JSON-backed values and portable/runtime-only status.
2. Use `create_drawing` for a new drawing. Use `list_drawings` and `get_drawing` when the user requests an existing drawing. Only change an existing drawing when the request authorizes that change.
3. Use the latest returned revision as `expectedRevision` on every mutation. When a revision conflicts, read the latest drawing and reconsider the edit before retrying. Codex and Claude can share the same durable workspace.
4. Use `add_component` or `add_sketch`, then `set_component_property`, `set_style`, `edit_text`, `set_path`, `move_elements` and `resize_elements`. Set only properties marked portable/editable. Other public CLR properties are discoverable metadata, not editable document fields. Port counts on the three original flowchart types are currently read-only. For a flowchart, `import_mermaid` places and connects the steps in one call; `add_next_step` adds the next step from a shape or workflow step.
5. Connectors:
   - Join workflow and catalog objects with `get_ports` and `connect_nodes`, or `connect_elements` (two elements: a port connector, or an attached arrow between shapes).
   - Change a connector with `edit_connector` (labels, route, colour, width, dashes, the ten arrowheads, flow) and move one end with `reconnect_connector`; a connector is identified by its start and end port IDs.
   - Remove one with `remove_connection`. Use `delete_elements` with `withAttachedLines`, and `delete_and_reconnect` to take a step out of a flow.
   - Inspect with `get_connections` (one element's connections) and `find_loose_lines`.
   - Set the drawing's `set_line_settings`: line jumps, and elbow routes that go round objects. `reset_route` clears the bends of lines and arrows.
6. Flow: `set_style`, `add_sketch` and `edit_connector` take `flow` (None, Dashes, Dots), `flowSpeed` and `flowDirection`, so lines and connectors animate on the board. Still exports show the first moment; `export_drawing` with format `gif` makes a looping animation. A video for LinkedIn is recorded in the browser (Export image › Video for LinkedIn), not by these tools.
7. Tables: `add_table` (rows, columns, optional starting `cells`, `header`, `rightToLeft` for Arabic), `get_table` to read them, `set_table_cells` to write a block of cells (the table grows to fit), `change_table` to insert, delete, merge, unmerge or move rows and columns, and `style_table_cells` for cell fill and alignment, the header row, direction, column width and row height. `set_style` on a table sets its colour, background, line width, text size, font and opacity. Rows grow to fit their text.
8. Use `select_elements`, `group_elements`, `reorder_elements`, `duplicate_elements`, `delete_elements`, `undo` and `redo` for document operations.
9. Use `save_library_selection`, `insert_library_entry`, `import_library` and `remove_library_entry` to manage reusable groups. Library metadata edits are not part of drawing undo.
10. Use `set_viewport` and `render_drawing` to inspect the result. Use `export_drawing` for native `.simpledraw.json`, PNG, SVG, PDF, GIF or library artifacts. Use `import_drawing` for a native, Excalidraw or draw.io document supplied by the user. Return an actual rendered image or verified artifact path, not a claim based only on tool success.

The `simpledraw://guide` resource describes the server contract. Drawing and component resources provide native documents and schemas. Persistent boards and artifacts live in the user's local application data directory, outside the installed plugin cache. History and selection are session state.

Live link: when the person has SimpleDraw open in a browser on this computer, `link_board` returns a one-time eight-digit code. Ask them to choose Link agent in SimpleDraw and enter it within five minutes. While linked, every edit made with these tools appears on their board as one undo step, and their edits arrive as new revisions (read the drawing again after a revision conflict). `unlink_board` ends the link. The link needs the server started with `--link-origin` set to the app's address, and works only from that address; without a link, hand over an exported `.simpledraw.json` file to open in SimpleDraw instead.
