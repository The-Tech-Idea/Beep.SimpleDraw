# Beep.SimpleDraw

A portable, local-first drawing app built with Blazor WebAssembly and Beep.Skia. Tools dock on the left, the canvas in the center, and libraries/properties on the right. It embeds **249 Beep.Skia components across 16 families**.

## Use the app

Open the local [SimpleDraw preview](http://127.0.0.1:5243/draw/) while its server runs. The [user guide](docs/USER_GUIDE.md) covers editing, files, libraries, recovery, keyboard controls and offline use.

- Rectangle, ellipse, diamond, line, arrow, freehand, text and eraser.
- Selection, styles, resize, duplicate, group, ordering, undo and redo.
- Wheel/trackpad pan; Shift+wheel horizontal pan; Ctrl/Cmd+wheel pointer-centered zoom.
- Embedded Beep.Skia libraries, component properties and directional connectors.
- Native editable files, PNG/SVG exports and reusable selection libraries.
- Revision-aware local autosave, recovery, offline cache and guarded updates.
- English/Arabic interface, keyboard drawing and responsive docked layout.

The published app is static and needs no database or application server. Serve it over HTTP/HTTPS; direct file loading is unsupported. Offline use starts after successful cache installation. Native documents are limited to 2 MiB / 2,000 objects. Family components are visual diagrams; inserting one does not execute its domain workflow.

## Current delivery

The consolidated candidate is `artifacts/simpledraw-v1-final`, built for `/draw/`. [Final delivery](docs/FINAL_DELIVERY.md) records package hashes and browser verification. The [release checklist](docs/RELEASE_CHECKLIST.md) separates local engineering verification from physical-device, production-host and independent acceptance.

The authoritative [master tracker](plans/enhancements/MASTER_TRACKER.md), [SDD](docs/SDD.md) and [RACI](docs/RACI.md) retain stage scope and historical evidence. Optional live collaboration and public plugin directory publication remain deferred.

## Codex and Claude Code plugins

The installable archive is `artifacts/simpledraw-plugins-v1.zip`. Each host package bundles the same compiled server, Beep.Skia libraries, **31 MCP tools**, property schemas and a workflow skill. Both are installed locally as `simpledraw@beep-tools`.

Follow [plugin installation](docs/mcp/PLUGINS.md) for other users. The server needs the .NET 10 SDK or ASP.NET Core Runtime. [MCP documentation](docs/mcp/README.md) covers standalone/Claude Desktop setup and the property contract. Browser exchange uses native files, without live synchronization. Public directory submission is deferred at the user's request.

## Build and preview

Prerequisites: pinned SDK 10.0.401, Node 24.14.0 and wasm-tools. [Reproducible builds](docs/BUILD_REPRODUCIBILITY.md) explains source/package/toolchain pins. The build verifies or prepares Beep.Skia from the declared source contract.

```powershell
dotnet workload install wasm-tools --skip-manifest-update
./scripts/build.ps1 -Output artifacts/new-candidate -BasePath /draw/
./scripts/serve-published.ps1 -ReleasePath artifacts/new-candidate -Port 5243
```

Deploy the verified `publish/wwwroot` or `simpledraw-static.zip`, not managed build intermediates. Use new output directories; earlier releases are retained. The [deployment guide](docs/DEPLOYMENT.md) covers base paths, headers, updates and rollback.

[Feature candidates](plans/enhancements/NEXT_FEATURES.md) prioritize possible additions; they are not commissioned stages.
