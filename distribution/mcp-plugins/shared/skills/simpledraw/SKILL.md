---
name: simpledraw
description: Create, inspect, edit, connect, save and render diagrams using the local Beep SimpleDraw MCP tools and embedded Beep.Skia libraries.
---

Use the SimpleDraw MCP tools supplied by this plugin. Discover actual tool names from the host; it may prefix them with the plugin/server namespace.

1. Use `list_components` with pagination to discover the 249 components across 16 families. Use `describe_component` on the exact returned type key before setting its properties. The returned metadata includes types, defaults, enum choices, JSON-backed values and portable/runtime-only status.
2. Use `create_drawing` for a new drawing. Use `list_drawings` and `get_drawing` when the user requests an existing drawing. Only change an existing drawing when the request authorizes that change.
3. Use the latest returned revision as `expectedRevision` on every mutation. When a revision conflicts, read the latest drawing and reconsider the edit before retrying. Codex and Claude can share the same durable workspace.
4. Use `add_component` or `add_sketch`, then `set_component_property`, `set_style`, `edit_text`, `set_path`, `move_elements` and `resize_elements`. Set only properties marked portable/editable. Other public CLR properties are discoverable metadata, not editable document fields. Port counts on the three original flowchart types are currently read-only.
5. Use `get_ports` and `connect_nodes` for connections; `remove_connection` for removal. Use `select_elements`, `group_elements`, `reorder_elements`, `duplicate_elements`, `delete_elements`, `undo` and `redo` for document operations.
6. Use `save_library_selection`, `insert_library_entry`, `import_library` and `remove_library_entry` to manage reusable groups. Library metadata edits are not part of drawing undo.
7. Use `set_viewport` and `render_drawing` to inspect the result. Use `export_drawing` for native `.simpledraw.json`, PNG, SVG or library artifacts. Use `import_drawing` for a native document supplied by the user. Return an actual rendered image or verified artifact path, not a claim based only on tool success.

The `simpledraw://guide` resource describes the server contract. Drawing and component resources provide native documents and schemas. Persistent boards and artifacts live in the user's local application data directory, outside the installed plugin cache. History and selection are session state. The browser imports/exports native files; this plugin does not live-control an already-open browser canvas or execute domain workflows.

For browser handoff, provide the exported `.simpledraw.json` file and tell the user to open/import it in SimpleDraw. Do not imply live synchronization.
