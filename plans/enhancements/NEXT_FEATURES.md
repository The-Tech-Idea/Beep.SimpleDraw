# SimpleDraw feature candidates

These are engineering recommendations, not measured user-demand rankings or commissioned stages. Extend the existing DrawingSession, native contracts and Beep.Skia renderer.

| Order | Feature | User value | Implementation boundary |
|---|---|---|---|
| 1 | Align/distribute, grid snapping and guides | Neat diagrams quickly | Existing multi-selection/history; one undo command per arrangement |
| 2 | Clipboard objects and pasted/imported images | Reuse work and annotate screenshots | Fresh object identities; bounded native image assets with offline/export support |
| 3 | Beep.Skia starter templates | Faster workflows, ERDs, UML and network diagrams | Existing library graph insertion and connector identities |
| 4 | Excalidraw and draw.io import | Move existing work from other apps | Explicit format adapters into current native documents; report unsupported elements |
| 5 | Pages, layers and object locking | Organize large drawings | Versioned native schema and clear selection/render/export semantics |
| 6 | Mermaid import and automatic layout | Turn text/AI output into editable diagrams | Map parsed graphs to actual Beep.Skia nodes/ports; undoable layout |
| 7 | Frames, presentation and PDF export | Meeting/report handoff | Export regions, shared renderer and offline fonts |
| 8 | Comments and live collaboration | Team review/editing | Optional P7 server/persistence/conflict/auth design |

Recommended next slice: alignment/snapping, clipboard/images and starter templates. Prioritize imports if users have existing collections to migrate; collect representative source files before committing to fidelity.

Comparison evidence: [draw.io import](https://www.drawio.com/docs/manual/open-diagram-file/), [Mermaid insertion](https://www.drawio.com/docs/manual/mermaid/), [Miro frames](https://help.miro.com/hc/en-us/articles/360018261813-Frames) and [templates](https://help.miro.com/hc/en-us/articles/360017572134-Templates). These establish comparison features, not demand or delivery status.
