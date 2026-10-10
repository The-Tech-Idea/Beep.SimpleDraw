# SimpleDraw 1.0.1 release handoff

Destination selected by the owner: `https://github.com/The-Tech-Idea/Beep.SimpleDraw`.
Suggested release tag: `simpledraw-plugins-v1.0.1` (not created or verified unused).
Publish this as a **local Windows x64 marketplace bundle**. Official directory
submission and remote connectors remain separate. No new repository is needed.

## Release assets prepared locally

- `artifacts/simpledraw-plugins-1.0.1-2026-10-10-r2.zip`
- `artifacts/simpledraw-plugins-1.0.1-2026-10-10-r2.zip.sha256`

Archive SHA-256:
`278d8f4120fba138d685b209bca0ebcdd2df5e7f97dfb9e88438c1d9c5e49f8c`

The first same-day candidate is superseded by r2, which includes the explicit
tool metadata correction. Retain existing local artifacts; upload only the r2
candidate and its matching digest. Do not upload workspaces, test profiles,
private configuration, credentials or the entire `artifacts/` directory.

## Proposed release description

SimpleDraw's local plugins let Codex and Claude Code create, edit and render
diagrams on Windows x64. Version 1.0.1 bundles the native-version-23 MCP server,
69 tools and 249 Beep.Skia components. It retains PC-local **Link an agent**
pairing and revision checks, adds standard/dark icons and accurate tool hints,
and includes Apache-2.0 terms for SimpleDraw plus original dependency notices.

Install the .NET 10 ASP.NET Core Runtime or SDK, download the ZIP and its digest,
verify the SHA-256, and extract to a permanent directory. Run the marketplace
installation commands in the enclosed README from that extracted root.

This bundle does not require IdentityServer registration. It is a local plugin
package; it does not supply a hosted MCP service, Claude chat/ChatGPT connector,
or Claude Desktop MCPB extension. Other operating systems are not accepted by
this release. Official directory review/publication has not occurred.

Verification includes extracted launchers for both clients, isolated Codex/Claude
installation, Claude strict manifest validation, native/PNG/SVG rendering,
Arabic tables, version-20 migration, local live pairing/push/poll and refusal
of unsupported future documents, stale revisions and ended link sessions.
The tests do not establish pairing with the deployed website or a model-driven
reviewer demonstration.

## Source and draft status

The binary asset was built from the owner's newer local version-23 working
snapshot. This scoped release branch does not include the full unpublished
drawing-engine source or identity work. Its older engine source does not
reproduce the 69-tool asset. This limitation is intentional and must remain in
the draft release description until the corresponding source is separately
reviewed and published. Do not describe the release branch as the exact binary
source or its current full-product CI as evidence for this asset.

The package builder can repackage an accepted binary with its exact discovery
and audited notices. The broader local build-input/notice audit source is not
included in this preparation-only commit. Use the bundle's preserved audit and
original notices when reviewing the actual asset; this is not a fresh engine
build recipe for the older repository source.

## Repository publishing steps

Commit only reviewed plugin-related source, licence/assets/docs and focused
checks. Existing unrelated identity/website work must be reviewed separately.
The binary bundle belongs in release assets; `artifacts/` stays ignored.
An ordinary checkout of this source repository lacks the packaged `server/`
directories: use the extracted release ZIP's marketplace root. Do not advertise
direct marketplace installation from the source repository until it actually
contains a tested marketplace and a complete version-pinned server delivery path.

The owner authorized scoped Git commit/push and a draft release after preparation.
Local master remains unchanged; the plugin release uses one remote release branch
and a temporary detached checkout. Repository administrator access was verified.
Full-suite CI is skipped for this preparation commit under the owner's focused-
checks rule; it is not a runtime-source release or claim of full-product acceptance.

## Official directory requirements remaining

Anthropic's official plugin checklist has a per-file limit below 5 MiB; this
local bundle's `libSkiaSharp.dll` is 12,254,048 bytes. A compliant pinned delivery
method and its actual reviewer acceptance remain necessary. Do not submit this
ZIP as if strict CLI validation resolved that gate.

OpenAI's official shared directory and Claude remote connectors require a
verified stable public HTTPS MCP endpoint. The domain alone does not provide
that endpoint. Public policies/support/terms, verified publisher/domain, an
accessible recorded demonstration and model-driven review cases remain pending.
Five positive and three negative cases are retained in
`public-review-cases.draft.json`, with their unrun public status unchanged.
Existing identity registrations are preserved; no provider-specific identity
integration is required solely by local plugin publication.
