# Beep SimpleDraw local plugins

SimpleDraw lets Codex and Claude Code create, edit, connect, save and render
drawings on your computer. This Windows x64 package includes the local MCP
server, 69 tools, 249 Beep.Skia components across 16 families, property schemas,
native drawing files, PNG/SVG export and the SimpleDraw workflow skill.
Publisher: The Tech Idea. Price: free, with no payments or purchases.

## Installation

Install the .NET 10 ASP.NET Core Runtime or SDK. `dotnet --list-runtimes` must
show both `Microsoft.NETCore.App 10.x` and `Microsoft.AspNetCore.App 10.x`.
The ordinary .NET Runtime alone is insufficient. This package contains Windows
x64 native libraries; other operating systems and architectures are not covered.

Download the versioned marketplace ZIP from the
[project's releases](https://github.com/The-Tech-Idea/Beep.SimpleDraw/releases),
check its SHA-256 against the digest supplied with that release, and extract it
to a permanent folder. A digest checks integrity; it does not establish publisher
identity. A prepared local ZIP does not mean a release has been uploaded.

Run from the extracted **marketplace root**, containing `.agents/`,
`.claude-plugin/` and `plugins/` (two folders above this README when reading it
inside an individual plugin):

```sh
codex plugin marketplace add .
codex plugin add simpledraw@beep-tools
codex plugin list
```

For Claude Code:

```sh
claude plugin marketplace add .
claude plugin install simpledraw@beep-tools --scope user
claude plugin list
```

Keep the extracted marketplace folder. Start a new agent session and ask:
"Use SimpleDraw to create a connected workflow, render it, and export its native
drawing." In Claude Code, the included skill is `/simpledraw:simpledraw`.
If your client does not support plugin marketplaces, update the client using its
normal update process. No client update is performed by this package.

## Link to a browser drawing

Ask your local agent to link a drawing using the SimpleDraw tools. Enter its
eight-digit code in **Link an agent** on
[SimpleDraw](https://draw.thetechidea.app) within five minutes. The browser must
be on the same computer. Allow the site's local-network permission when prompted.
Once paired, document changes can pass between the browser and local MCP server
using revision checks. Unlinked drawings can be exchanged as native files.
The website URL is an allowed pairing origin, not a hosted MCP endpoint.

The supplied server uses native document version 23. Local protocol acceptance
includes version-20 migration, Arabic tables and refusal of future versions and
stale revisions. This does not establish compatibility with a particular deployed
browser build. If linking reports an unsupported SimpleDraw file version, check
both the deployed website version and the installed plugin binary.

## Local data and permissions

The default drawing workspace is `Beep.SimpleDraw/mcp` under the operating
system's local application-data directory, outside the plugin cache. The two
clients can use the same workspace. Drawings and exported files persist after
plugin uninstall; manage them yourself. Pairing binds a local browser session to
a drawing. Do not put secrets into drawing text. Content returned to your agent
is processed under that agent provider's settings and terms. This plugin does not
require IdentityServer registration or a hosted drawing account.

## Package and licence

The original SimpleDraw code is Apache-2.0; see `LICENSE.txt` and `NOTICE.txt`.
Dependencies retain their own licences and original notices under `notices/`.
The notice audit identifies the exact package archives and payload files checked;
it does not claim complete legal review. The upstream Beep.Skia MIT notice still
has copyright-holder/year placeholders, which remain an owner review item.

The root `manifest.json` records every distributed file's SHA-256 except itself.
Debug symbols, unrelated settings and non-Windows native binaries are excluded.
The package builder refuses to overwrite an existing release.

This is a local marketplace package for supported local clients. It is not a
Claude Desktop MCPB extension or a hosted connector for Claude chat/ChatGPT.
Official directory approval and publication are separate. Anthropic's current
official plugin checklist limits individual files to under 5 MiB; the native
Skia DLL in this local bundle exceeds that limit and needs a compliant delivery
approach before official submission.

## Removal and support

```sh
codex plugin remove simpledraw@beep-tools
claude plugin uninstall simpledraw@beep-tools --scope user
```

Uninstalling does not erase your drawings. For setup problems, report the client
version, plugin version, operating system and redacted error at the
[project repository](https://github.com/The-Tech-Idea/Beep.SimpleDraw). Do not
include credentials, pairing codes or private drawings in a public report.
