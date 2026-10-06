# M5 software design

## Package structure

The release root contains `.agents/plugins/marketplace.json` for Codex, `.claude-plugin/marketplace.json` for Claude Code, and two host-specific packages under `plugins/`. Both publish `simpledraw@beep-tools` and bundle identical server DLLs plus the shared workflow skill. Only host metadata and path resolution differ: Codex resolves relative `cwd` to its installed plugin root; Claude expands `${CLAUDE_PLUGIN_ROOT}` in stdio arguments.

`distribution/mcp-plugins` owns source templates. `scripts/package-mcp-plugins.ps1` copies a previously published server, validates discovery counts, omits developer config/acceptance files, records payload SHA256 and builds a dot-directory-preserving ZIP. Existing release paths are rejected. No package install hook, download-at-startup, source checkout dependency or shell launcher is needed.

## Runtime

The framework-dependent DLL requires Microsoft.NETCore.App and Microsoft.AspNetCore.App 10.x. Both clients use the server's existing default application-data workspace; writes remain revision guarded and cross-process serialized. Data is outside plugin cache. Native Skia assets are bundled; other operating systems require their own runtime/library acceptance.

## Verification and limits

CLI installation alone is not model-driven acceptance. Inspect resolved cached manifests and directly exercise their configured launchers over JSON-RPC; archive extraction must preserve hidden manifests and work from an unrelated path. Local plugin installation is not universal public marketplace publication. Claude Desktop uses standalone MCP configuration and is not the Claude Code plugin host.
