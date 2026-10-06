# ADR 001: Standalone Blazor WebAssembly using Beep.Skia
Date: 2026-10-05. Status: adopted for implementation start.

## Context
The workspace contains an empty SimpleDraw app and a substantial Beep.Skia component scene. The user asks for portability, simplicity, Excalidraw-like workflows, and consideration of Beep.Skia. The user authorizes Beep.Skia updates.

## Alternatives
| Choice | Strength | Cost |
|---|---|---|
| Standalone Blazor WASM + Beep.Skia | Direct C# scene/history reuse; static deploy; offline-capable | Native WASM build toolchain; payload and trimming validation |
| React + Excalidraw package | Mature complete whiteboard | Bypasses owner's core; JS/.NET scene integration adds complexity |
| Blazor Server + Skia image transport | Easy server native rendering | Requires persistent service/network; cannot supply standalone offline editor |

## Decision
Adopt standalone .NET 10 WASM and repair the active Beep.Skia Blazor host. Add SketchShape to existing core components. Keep one DrawingManager and its serializer/history. No parallel HTML Canvas renderer or second scene model.
Build first a real vertical slice, then measure publish payload and browser behavior before expanding functionality.

## Consequences and gates
Static portable distribution needs no .NET installation for users. Developers need the SDK, wasm-tools, package feeds, and sibling source during the initial integration.
The old host's center-point input and missing markup must be fixed. Core package transitive dependencies require release trimming/native checks.
Pin Skia managed/native versions together. Baseline broad Beep.Skia regression is separate from focused tests. If portability fails, fix/extract dependencies in Beep.Skia with evidence before reconsidering the architecture.

## Sources checked
- [Excalidraw](https://github.com/excalidraw/excalidraw): reference workflow/features.
- [Microsoft standalone PWA guide](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app/?view=aspnetcore-10.0): static/offline deployment behavior.
- [SkiaSharp package guide](https://github.com/mono/SkiaSharp/blob/main/documentation/dev/packages.md): Blazor controls and WASM native assets.
- Local Beep.Skia source audit: see SDD reuse section.
