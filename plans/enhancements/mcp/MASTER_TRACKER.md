# SimpleDraw MCP master tracker

| Stage | Scope | State | Evidence |
|---|---|---|---|
| M1 | 249 catalog entries, 16 families, property metadata, transports/resources | Local gate passed | MCP tests and wire manifests |
| M2 | Shared drawing operations, revision control and history | Local gate passed | Workspace tests and stdio smoke |
| M3 | Persistent boards, personal libraries, native/PNG/SVG artifacts and inline images | Local gate passed | Native graph restart and real exports |
| M4 | Release package and Claude/Codex configuration | Local delivery passed; model sessions open | Locked publish and installed CLI checks |

Each stage has its own plan, SDD and RACI document in this directory. The server exposes the portable document property contract and lists other writable CLR properties as runtime-only. Browser exchange is via native files; existing tabs are not a live remote-control channel. Independent model-driven client sessions, other operating systems, device/accessibility and remote-host sign-offs remain separate from local engineering verification.


## Local engineering acceptance - 2026-10-06

Published `artifacts/mcp-local-v1-final`; archive `artifacts/simpledraw-mcp-local-v1.zip`. MCP server SHA256 `7cf31b1db4e7c5b9c6623d2d9320de11653cb29d240a2c3e90ee5b6e47e056d6`; archive SHA256 `e2ec1897703aa2970c67b919c3e41efab409cbd4dfb79fcb4ac9912086b6884d`. 91 published payload hashes verified against the 92-entry ZIP (including manifest). Beep.Skia remains at the verified 498-input source fingerprint e9f5faabe9dac9aee4d8fa38def10d5fddd98a74b3b0bf47e2ef2cef78618c9f.

M1-M3 local gates pass: 255 MCP cases including actual discovery/insertion/native export for every one of the 249 component types, plus 585 core regression cases. The final published DLL passes 292 successful wire tool calls across all 31 tools, both explicit invalid/stale error cases, 249 component descriptions, inline PNG, native graph import/export identity, SVG, resources and library/history operations. HTTP discovery/paging passes; foreign Origin and Host return 403, malformed JSON-RPC returns 400. Both transports are exercised against the actual release, not only direct method calls. Before-fix logs remain historical evidence.

M4 local delivery passes: Codex global stdio server `simpledraw` is enabled; Claude Code user-scoped server `simpledraw` reports Connected. Both point to the final DLL and workspace/mcp. Claude Desktop configuration is provided in docs/mcp/claude-desktop.json; no Desktop installation/connection is claimed. Client startup/restart or a new chat is required to load newly registered tools. A full interactive model session in each client, additional operating systems and remote-host acceptance remain open. Runtime-only CLR properties are identified but are outside portable native editing; browser exchange is via native files.

Evidence: docs/evidence/mcp-client-acceptance.json, mcp-tool-manifest.json, mcp-component-schemas.json, mcp-stdio-acceptance.json, mcp-http-acceptance.json, mcp-all-component-insertion.log, mcp-core-regression-fixed.log and the final publish log. Existing web preview and user drawing were retained.

## M5 plugin delivery - 2026-10-06

Native Codex and Claude Code plugins are installed and enabled as simpledraw@beep-tools. Claude plugin MCP health reports Connected, with one skill and one MCP server. Both installed cached launchers pass initialization/discovery, property lookup, native/PNG exports, inline rendering and resources. Both extracted launchers pass from an unrelated path containing spaces. All 141 manifest payload hashes verify; hidden host manifests are present. Archive artifacts/simpledraw-plugins-v1.zip SHA256: 59775c5f9ed5b5d9a0c4d48e864deafe078b1102104ba8a716f62c141c1c6148. Shared server DLL SHA256: 7cf31b1db4e7c5b9c6623d2d9320de11653cb29d240a2c3e90ee5b6e47e056d6.

Our standalone registrations were removed after verification to avoid duplicate tools. The old repo workspace contained no files. Both plugins use the default application-data workspace outside their caches. Evidence: docs/evidence/mcp-plugin-* installation/detail/connection logs, cached and portable acceptance.json records, and mcp-plugin-archive-acceptance.json. Public directory publication was explicitly deferred. Model sessions and other-OS acceptance remain open. See the M5 plan/SDD/RACI and docs/mcp/PLUGINS.md.
