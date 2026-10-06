# Phase 1: Reproducible static build — CI/CD design
## 0. Problem statement
The app uses project references into ../Beep.Skia (src/Beep.SimpleDraw.Core/Beep.SimpleDraw.Core.csproj and Web csproj). The selected four Beep projects consume two owner package versions not published on nuget.org. Repository-local cache assumptions cannot reproduce this build. Beep.Skia and Model also run machine-local DLL/package copy targets by default.
## 1. Goals
1. Pin SDK 10.0.401, the SDK-supplied wasm manifest/10.0.12 packs, Node 24.14.0, exact NuGet versions/content hashes and Beep source.
2. Rebuild from a clean source checkout with a new package directory, without an undeclared local feed or DLL.
3. Test before native-linked Release publishing, verify every offline asset/encoding and package one static folder.
## 2. Design
Use a pinned Beep.Skia base commit plus a checked-in reviewed patch and explicit new source files. The dependency lock records the exact normalized source hashes. Prepare only an absent script-owned sibling checkout; existing user checkouts are verified and never reset or overwritten. Preserve the one project reference architecture.
Bundle the two exact MIT owner package artifacts under dependencies/packages and verify SHA256, version and nuspec source provenance. NuGet.Config maps those exact IDs to that feed and all remaining IDs to nuget.org. Commit per-project net10 lock files; restore in locked mode.
Add BeepSkiaLocalBuildCopies=false to this app's references/build script; other framework consumers retain the existing default. Pin SDK with disabled roll-forward. CI installs wasm-tools from the SDK's manifests with --skip-manifest-update and verifies manifest/packs rather than updating to floating advertising manifests.
Build script verifies pins, locked-restores, runs app and JS suites, publishes to a fresh directory, optionally sets a validated base href, refreshes offline integrity, inventories and verifies assets, and packages the wwwroot folder. Static hosting uses root/subpath and CSP/cache/wasm MIME examples, without a runtime database, backend or container dependency.
## 3. Surface
GitHub Actions pull_request/push/workflow_dispatch: verify input pins -> restore -> C#/JS tests -> native publish -> integrity/package checks -> upload static artifact and provenance. Windows-2025 runner; actions pinned to audited commit SHAs; contents:read only; no deployment or secrets. Build cache keys include exact input locks. Dependabot proposes weekly reviewed updates; no automatic major merges.
## 4. Files to change
- global.json, NuGet.Config, dependencies/*, project lock files: pinned inputs.
- scripts/prepare-dependency.ps1, scripts/build.ps1, Node pin/asset helpers: build surface.
- .github/workflows/verify.yml and dependabot.yml: CI configuration.
- Beep.Skia/Beep.Skia.csproj and Beep.Skia.Model.csproj: opt out of local copy targets.
- P6 plans, deployment/checklist/user/notice docs: evidence and operations.
## 5. Build / ship checklist
Only documented pinned inputs. Test failure stops publish. Offline manifest/encoding mismatch stops packaging. Never overwrite a source checkout or previous artifact. Cache hits never replace integrity checks. No public deployment performed by CI.
## 6. Backward compatibility
New app. Existing Beep local copy defaults retained for other consumers; SimpleDraw opts out. No parallel component contracts or render path.
## 7. Verification
Clean package restore; new isolated source checkout prepared from base/patch; locked build/test/native publish; verify source/asset/encoding/hash refusal on deliberate corruption; root/subpath HTTP/browser/offline checks. CI source configuration and local equivalent evidence are distinct from an actual hosted CI run.
## 8. Risks
Missing owner dependency versions: bundle licensed exact artifacts with provenance. Drift: locked restore/hash refusal. Hosted runner/toolchain availability: record separately from current local checks. Browser/device and owner acceptance remain external gates.
## 9. Out of scope
Containers, hosting charges, public deployment, credentials, branch-protection changes, fabricated ship approval.
## 10. Sub-task execution order
6.A.1 pinned packages/toolchain/source; 6.A.2 preparation/build/CI/integrity tests; then P6 hosting, benchmark and documentation tasks. User already authorized implementing the P1–P6 plan and sibling changes.
