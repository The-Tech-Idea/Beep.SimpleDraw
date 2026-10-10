# SimpleDraw local plugin release acceptance — 2026-10-10

## Candidate and scope

Owner selected Apache-2.0 for SimpleDraw, standard/dark icons, and the existing
public repository `https://github.com/The-Tech-Idea/Beep.SimpleDraw` as the
release location. Publisher answer: The Tech Idea; countries: all available;
price: free, no payments or purchases. These are owner choices, not portal
verification or approval.

Final candidate: `artifacts/simpledraw-plugins-1.0.1-2026-10-10-r2.zip`
(27,211,880 bytes; approximately 26 MiB), with its adjacent `.sha256` file.

SHA-256: `278d8f4120fba138d685b209bca0ebcdd2df5e7f97dfb9e88438c1d9c5e49f8c`

Bundled MCP DLL SHA-256:
`e0473b3c0c174a9dced5cb09b3d621bfe097d413edbe0014adb7c870949a8bf7`

The first same-day ZIP is superseded by r2 and retained. Acceptance applies to
Windows x64 local stdio tools and loopback pairing. No hosted endpoint, deployed
website pairing, model-driven review cases, public release or official directory
acceptance is established. Existing IdentityServer registrations/configuration
were preserved; this preparation added no identity functionality.

## Changes verified

- Both manifests use version 1.0.1 and Apache-2.0, retaining the `simpledraw`
  identity and `beep-tools` marketplace. Codex's subtitle fits 30 characters.
- Standard/dark square PNG assets are included and referenced by Codex. Both
  are 1280 × 1280 and below 5 MiB. Generated source images remain in the original
  image-generation directory; project copies are under shared plugin assets.
- All 69 tools now return explicit Boolean read/write, destructive and
  open-world hints plus human-readable titles. Export/render are writes because
  they create local artifact files. Tools interact with the bounded workspace
  and opt-in local board, without arbitrary external-service operations.
  Conservative destructive hints cover edits/removals; additive creation/export
  does not claim destructive changes. Existing drawing authorization and
  revision validation remain enforced.
- The existing package audit owner now supports MCP publish auditing. All
  16 dependency archives matched their reviewed SHA-256 pins and the runtime's
  content-hash declarations. Seventeen Windows/runtime DLLs matched their
  archive entries byte for byte. Forty-five original notice/metadata files are
  retained per plugin, including fonts and source/vendor collections.
- Missing archive licence texts for the four MCP/AI dependencies were retrieved
  from repository commits identified by their exact nuspec metadata and pinned
  in `dependencies/notices/mcp-sources.lock.json`. The C# SDK's original licence
  transition statement is retained; a package's SPDX field is not substituted
  for its complete original terms.
- Packaging uses only dependency-declared DLL paths, required runtime/discovery
  JSON, skill/assets/readmes and verified licences/notices. It excludes PDBs,
  private settings, test workspaces and other architectures/platforms. The
  extracted archive's 196 files matched the inventory and hashes exactly.

## Focused acceptance

Raw local evidence is in `artifacts/plugin-release-check-1.0.1-2026-10-10-r2/`:

| Check | Result and evidence |
| --- | --- |
| Pinned sibling source | Verified 515 source files with `pin-inputs.mjs verify`; no pin refresh or Git command |
| MCP Release build | Locked project restore and warning-as-error publish succeeded to a new directory; no Host/SDK feature implementation |
| Protocol/catalog/edit/export | `discovery/mcp-stdio-acceptance.json`: 69 tools, 249 property schemas, 16 families, 292 calls; native/PNG/SVG, rendering/resources, stale-revision refusal |
| Extracted payload | `inventory.json`: 196 files, matching sizes/SHA-256, icon checks, 16-package notice audits, no private settings/debug symbols |
| Codex and Claude configured launchers | `codex-launcher/acceptance.json`, `claude-launcher/acceptance.json`: exact new DLL hash, explicit hints/titles, tool/schema discovery, native export, real PNG/inline render/resources, JSON-only stdout |
| Codex CLI 0.162.1 install | `codex-marketplace-add.json`, `codex-install.json`, `codex-list.json`: installed/enabled 1.0.1 using a fresh isolated CODEX_HOME |
| Claude Code 2.1.296 install | `claude-marketplace-add.log`, `claude-install.log`, `claude-list.log`: installed/enabled 1.0.1 using a fresh isolated CLAUDE_CONFIG_DIR |
| Installed cache launchers | `codex-installed-launcher/acceptance.json`, `claude-installed-launcher/acceptance.json`: configured launchers execute the new payload and render successfully |
| Claude strict validation | `claude-marketplace-validation.json`, `claude-plugin-validation.json`: success, zero errors/warnings |
| Pairing/version checks for both payload copies | `codex-live/acceptance.json`, `claude-live/acceptance.json`: version 23, exact native import, version-20 migration, Arabic tables, local pair/push/poll; rejects future documents, stale revisions and ended sessions |

These are focused local checks. Existing user client profiles, MCP registrations,
private drawings and production services were not reconfigured. Pairing tests
reach loopback listeners; the production domain appears only as the allowed
Origin string and is not contacted by those tests. No full test suite ran.

## Standing release checklist applicability

The shared owner `C:/Users/f_ald/.codex/APP_RELEASE_REQUIREMENTS.md` remains the
single checklist. This evidence does not replace its legal/provider requirements
or attest whole-product compliance.

| Affected requirement | Actual behavior, focused evidence and remaining dependency |
| --- | --- |
| Signup age policy | This local package has no signup or hosted accounts; no age gate was added. Website/identity audience and registration journeys remain outside this package acceptance. |
| Font delivery and licensing | Renderer uses embedded fonts; original font notices retained and Arabic rendering exercised. No new remote font request or font family was introduced. Browser network/privacy acceptance remains separate. |
| Replay/tracking | No replay or analytics was added. Local payload/settings inventory is verified; no general statement about the website or AI provider telemetry is made. |
| Marketing email | No mail-sending tool or marketing template in this package; owner address/email requirements remain for any applicable website/operator journey. |
| Subscription/renewal terms | Owner confirms free/no commerce, and no billing tool or subscription flow was added. |
| Copyright/DMCA | Original product and dependency terms are included. The preserved Beep.Skia MIT header has upstream year/holder placeholders; source metadata/owner header review remains recorded. Local workspaces do not add an operator-hosted public user-content service. Any hosted sharing service's operator facts and required DMCA registration/walkthrough remain separate owner-guided work. |
| Privacy/security and truthful listings | README describes actual local storage, opt-in pairing, persistent files and agent-provider handling, without inventing contact/retention/legal facts. Archive excludes private configuration/workspaces and accurately labels supported Windows x64 scope. Official privacy/support/terms URLs and portal verification are not supplied by this local test. |

## Publication status and exact remaining gates

The repository is publicly readable, as checked through the provided GitHub URL
on 2026-10-10. No Git command, commit, push, tag, upload, reviewer submission or
publication was performed. [Release handoff](../mcp/GITHUB_RELEASE_1.0.1.md)
contains the concrete assets, digest and proposed release description. Ordinary
source checkout lacks the bundled server; installation must use the extracted
release ZIP until a complete repository marketplace path is implemented/tested.

Anthropic official plugin file-size gate is **Blocked**: each copy of
`libSkiaSharp.dll` is 12,254,048 bytes, exceeding its below-5-MiB checklist limit.
Strict CLI validation does not resolve that gate. A compliant pinned dependency
delivery approach must be implemented and accepted before official submission.

OpenAI official shared directory / Claude remote connector readiness is **Blocked**
on a verified public HTTPS MCP service, real public listing URLs, actual reviewer
access/demo and model-driven case acceptance. Existing five-positive/three-negative
public cases retain **Not run** status. The domain name alone is not an endpoint.
Claude Desktop's MCPB extension route also requires a separate package.

Primary provider requirements checked in the publication review:
[OpenAI submission](https://developers.openai.com/plugins/deploy/submission),
[Claude publication](https://code.claude.com/docs/en/plugins/publish),
[Anthropic preflight](https://claude.com/docs/plugins/pre-submission-checklist).

## Authorized draft handoff

After the local preparation above, the owner authorized scoped commit/push and
a draft GitHub release. The main working checkout remains on master. The remote
plugin release branch contains packaging/client metadata and review material;
the complete unpublished version-23 engine/identity source stays outside it.
The binary's source is therefore the accepted local working snapshot, not a
reproducible checkout of that preparation branch. This limitation must be
disclosed in the draft; no whole-product or branch-source acceptance is claimed.
The CI skip marker prevents the unrelated full static suite from being triggered
for this preparation commit, in accordance with the focused-checks instruction.
