# Decision 002: Pin source and bundled owner packages
Date: 2026-10-05. Status: adopted for the authorized P6 implementation.

Keep the existing project-reference architecture. Reconstruct the four reviewed Beep.Skia projects at an exact base commit plus the app's reviewed patch/new files, and verify normalized hashes before builds. Check an existing user checkout without changing it. A published static app has no sibling checkout dependency.

Required owner package versions DataManagementModels 3.1.1 and Vis.Modules 2.0.44 are absent from nuget.org (public-only isolated restore returned NU1102). Their existing nuspecs declare MIT and identify source repository commits. Bundle the exact archives with hashes/provenance and map their two IDs exclusively to the repository feed. Pin the remaining NuGet package graph and archive bytes, use only nuget.org for those packages, and verify extraction contents even on cache hits. This removes undeclared local-feed/global-cache assumptions without claiming those owner versions were publicly released.

Use SDK 10.0.401, Node 24.14.0 and the SDK-supplied wasm/compiler packs. Disable sibling local-copy targets only for the app's builds. Limit framework selection through an explicit property on the four multi-target sibling projects; preserve normal single-target NuGet imports and verify the actual native Skia reference. Commit source/configuration and build evidence; do not publish a service, introduce a parallel renderer/model, or fabricate ship acceptance.

[Build procedure](../BUILD_REPRODUCIBILITY.md) · [P6 tasks](../../plans/enhancements/P6_RELEASE.md) · [RACI](../RACI.md)
