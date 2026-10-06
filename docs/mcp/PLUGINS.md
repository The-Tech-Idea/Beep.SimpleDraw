# Installable SimpleDraw plugins

The distribution contains two native host packages backed by the same tested server and compiled Beep.Skia libraries. Neither needs a SimpleDraw or Beep.Skia source checkout. Both expose 31 MCP tools, 249 components across 16 families, portable property schemas, native files, PNG/SVG export and reusable libraries.

## Install

Download and extract `simpledraw-plugins-v1.zip` to a permanent directory. Run these commands from the extracted directory, which contains `.agents/`, `.claude-plugin/` and `plugins/`.

Install the .NET 10 SDK or .NET 10 ASP.NET Core Runtime first. `dotnet --list-runtimes` must include both `Microsoft.NETCore.App 10.x` and `Microsoft.AspNetCore.App 10.x`. The ordinary .NET Runtime alone is insufficient. No Node.js or PowerShell runtime is required by the plugin server. Native Skia dependencies must be available on the host; operating systems other than the tested Windows host require their own acceptance.

### Codex CLI / local desktop client

```sh
codex plugin marketplace add .
codex plugin add simpledraw@beep-tools
codex plugin list
```

### Claude Code

```sh
claude plugin marketplace add .
claude plugin install simpledraw@beep-tools --scope user
claude plugin list
claude plugin details simpledraw
```

Start a new session after installation. Ask: "Use SimpleDraw to create a connected workflow with Beep.Skia components, render it, and export the native drawing." The included `simpledraw` skill guides discovery, property editing and revision-safe changes; in Claude Code it is `/simpledraw:simpledraw`.

Keep the extracted marketplace directory for updates. This is an installable local marketplace archive, not a public directory listing. A publisher can host this entire directory in a Git repository later and users can register its actual repository URL instead of `.`. No repository or public marketplace publication is implied by this package.

## What is included

`plugins/simpledraw-codex` uses the supported Codex manifest and relative MCP working directory. `plugins/simpledraw-claude` uses the Claude Code manifest and `${CLAUDE_PLUGIN_ROOT}` in its MCP arguments. Each includes its own copy of the same compiled server, native runtime assets, all Beep.Skia assemblies, discovery manifests and workflow skill. Host configuration differs; the drawing implementation is shared.

Both clients default to the same local application data workspace, `Beep.SimpleDraw/mcp`. This is outside the plugin cache and survives plugin updates/uninstall. All mutations require the current document revision. Tool output and native artifacts are the integration surface; an open browser canvas is not live-synchronized. Every portable property is editable; other writable CLR properties are labeled runtime-only. Claude Desktop is not a Claude Code plugin host; its separate MCP configuration remains in README.md.

`manifest.json` records SHA256 for every distributed file except itself. `simpledraw-plugins-v1.zip.sha256` authenticates the archive against the publisher-provided digest. A digest verifies integrity, not publisher identity. The package script refuses to overwrite an existing release.

## Remove

```sh
codex plugin remove simpledraw@beep-tools
claude plugin uninstall simpledraw@beep-tools --scope user
```

If you previously installed the standalone server as `simpledraw`, remove that own entry after installing the plugin to avoid duplicate tools: `codex mcp remove simpledraw` and `claude mcp remove simpledraw --scope user`. Leave unrelated servers intact.

## Build a distribution

Publish and verify the server using `scripts/publish-mcp.ps1`, then run `scripts/package-mcp-plugins.ps1 -ServerRelease <published-directory> -Output <new-directory>`. The builder validates the payload's tool/component counts and hashes the copied package. It preserves existing server releases. Local engineering evidence and client installation results are in `docs/evidence/mcp-plugin-*`; interactive model sessions and other OS acceptance remain separate gates.

Formats and installation follow [OpenAI plugin packaging](https://developers.openai.com/plugins/build/plugins), [Codex MCP plugin path resolution](https://github.com/openai/codex/blob/main/codex-rs/codex-mcp/src/plugin_config.rs), [Claude plugin reference](https://code.claude.com/docs/en/plugins-reference) and [Claude marketplaces](https://code.claude.com/docs/en/plugin-marketplaces).
