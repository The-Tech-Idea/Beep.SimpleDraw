# SimpleDraw MCP for Claude and Codex

One .NET MCP server uses the existing DrawingSession, fixed SkiaCatalog, native document validation, Beep.Skia DrawingManager, export renderer and SketchLibrary. It supports local stdio and optional loopback Streamable HTTP. No separate Claude-specific drawing engine is required.

## Start

Publish with `./scripts/publish-mcp.ps1 -Output artifacts/mcp-local-v1-final`. Requires .NET 10 with the sibling Beep.Skia source for building; the published folder includes the compiled component libraries. Launch the published DLL with `dotnet <absolute-path>/Beep.SimpleDraw.Mcp.dll --workspace <absolute-workspace-directory>`.

For a shared process with shared in-memory undo, start the same DLL with `--http --port 5246 --workspace <directory>`. Endpoint: `http://127.0.0.1:5246/mcp`. Host and Origin validation accepts this loopback host only. Remote hosted connectors are outside this local deployment.

## Client setup

`docs/mcp/claude-desktop.json` and `docs/mcp/codex.toml` are concrete stdio configuration snippets generated for this release. Both clients use the same workspace. Registration in Claude Code and Codex is performed using their installed CLI configuration commands after protocol verification; see the acceptance record for actual results. Restart the clients or start a new chat to load newly configured tools. Existing chats do not acquire new tools automatically.

For Claude Desktop, merge the `mcpServers.simpledraw` object from the JSON snippet into `%APPDATA%/Claude/claude_desktop_config.json`, preserving other servers. For Claude Code: `claude mcp add --scope user simpledraw -- dotnet <DLL> --workspace <directory>`. For Codex: `codex mcp add simpledraw -- dotnet <DLL> --workspace <directory>`. Codex also accepts the HTTP endpoint with `codex mcp add simpledraw --url http://127.0.0.1:5246/mcp` when the shared server is running.

## Agent workflow

1. `list_components` discovers all 249 embedded standalone components in 16 families, exact compiled type keys and seven sketch kinds. Filter/search or page with offset/limit. Network connections are exposed as connectors.
2. `describe_component` returns the actual portable property contract, default values, scalar versus JSON encoding, enums, help/choices and default port topology. All public writable CLR property names are also reported; properties outside the native document contract are identified as runtime-only. Port-count changes on the three reviewed workflow types require identity topology changes and are explicitly read-only. Structured domain fields already represented as JSON strings remain JSON strings; `jsonBacked` identifies them.
3. `create_drawing` or `import_drawing` returns an ID and revision. All editing calls require `expectedRevision`; use the revision returned by the previous call. On conflict, read the current drawing and reconsider the edit.
4. `add_component` inserts a compiled Beep component at world coordinates. `add_sketch` creates Rectangle/Ellipse/Diamond/Line/Arrow/Freehand/Text. Paths require world-coordinate points. Text uses the same bundled font limits as the web app.
5. Property, text, path, style, move, resize, selection, duplicate, group, reorder and delete tools reuse shared editing/history. `get_ports` exposes actual port identities and availability; `connect_nodes` uses Straight/Orthogonal/Curved routing. Remove connections using stable endpoint IDs from `get_drawing`.
6. `save_library_selection`, `insert_library_entry`, `import_library` and `remove_library_entry` use the existing personal-library format, limits and identity remapping.
7. `render_drawing` returns a real inline PNG to the agent. `export_drawing` writes native JSON, PNG, SVG or library JSON under workspace/artifacts with content-hashed filenames. Open native `.simpledraw.json` files using SimpleDraw's Open control. Agent-managed drawings do not silently replace the drawing in an existing browser tab.

## Persistence and limits

Boards use the existing native version 7 envelope. Workspace metadata contains only ID/name/revision/native document/library. Writes use atomic replacement and a cross-process file lease. Revisions prevent Claude/Codex lost updates. Native data and libraries persist across restarts. Drawing undo/redo and viewport/selection are in-memory state; a restart or another process's edit resets stale history. Library metadata operations do not use drawing undo.

Existing strict limits apply: 2 MiB native document, 2,000 shapes, reviewed port/connector limits, 1 MiB personal library, 100 library entries, export at most 8192px per side/16 million pixels. Workspace cap is 256 boards. The server cannot load arbitrary CLR types, execute domain simulators, or run user-supplied scripts. Runtime-only properties (including domain structures not yet in native serialization) are exposed as metadata but are not editable persisted drawing fields.

## Verification

See `docs/evidence/mcp-tool-manifest.json` for actual wire input schemas, `mcp-component-schemas.json` for all 249 property descriptions, `mcp-stdio-acceptance.json` for real JSON-RPC discovery/edit/connect/library/render/export/resource acceptance, and the MCP test logs. Client registration is distinct from a full interactive model session. Physical-device and remote-host acceptance remain separate.

Protocol implementation follows the [official MCP C# SDK v1 tool contract](https://csharp.sdk.modelcontextprotocol.io/v1/concepts/tools/tools.html) and [transport contract](https://csharp.sdk.modelcontextprotocol.io/v1/concepts/transports/transports.html). SDK 1.4.1 is pinned for the established initialize/stdio protocol used by these clients. Claude local configuration follows [Claude's local MCP guidance](https://support.claude.com/en/articles/10949351-getting-started-with-local-mcp-servers-on-claude-desktop); Codex syntax was verified with the installed `codex mcp add --help`.


## Local engineering acceptance - 2026-10-06

Published `artifacts/mcp-local-v1-final`; archive `artifacts/simpledraw-mcp-local-v1.zip`. MCP server SHA256 `7cf31b1db4e7c5b9c6623d2d9320de11653cb29d240a2c3e90ee5b6e47e056d6`; archive SHA256 `e2ec1897703aa2970c67b919c3e41efab409cbd4dfb79fcb4ac9912086b6884d`. 91 published payload hashes verified against the 92-entry ZIP (including manifest). Beep.Skia remains at the verified 498-input source fingerprint e9f5faabe9dac9aee4d8fa38def10d5fddd98a74b3b0bf47e2ef2cef78618c9f.

M1-M3 local gates pass: 255 MCP cases including actual discovery/insertion/native export for every one of the 249 component types, plus 585 core regression cases. The final published DLL passes 292 successful wire tool calls across all 31 tools, both explicit invalid/stale error cases, 249 component descriptions, inline PNG, native graph import/export identity, SVG, resources and library/history operations. HTTP discovery/paging passes; foreign Origin and Host return 403, malformed JSON-RPC returns 400. Both transports are exercised against the actual release, not only direct method calls. Before-fix logs remain historical evidence.

M4 local delivery passes: Codex global stdio server `simpledraw` is enabled; Claude Code user-scoped server `simpledraw` reports Connected. Both point to the final DLL and workspace/mcp. Claude Desktop configuration is provided in docs/mcp/claude-desktop.json; no Desktop installation/connection is claimed. Client startup/restart or a new chat is required to load newly registered tools. A full interactive model session in each client, additional operating systems and remote-host acceptance remain open. Runtime-only CLR properties are identified but are outside portable native editing; browser exchange is via native files.

Evidence: docs/evidence/mcp-client-acceptance.json, mcp-tool-manifest.json, mcp-component-schemas.json, mcp-stdio-acceptance.json, mcp-http-acceptance.json, mcp-all-component-insertion.log, mcp-core-regression-fixed.log and the final publish log. Existing web preview and user drawing were retained.
