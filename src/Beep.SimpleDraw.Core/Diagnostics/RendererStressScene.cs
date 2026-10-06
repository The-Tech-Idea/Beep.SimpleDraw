using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.SimpleDraw.Core.Diagnostics;

/// <summary>Disposable renderer-only workload; never imported into the product document.</summary>
public sealed class RendererStressScene : IDisposable
{
    public const int ObjectCount = 5000;
    private readonly List<SketchShape> _shapes = new(ObjectCount);
    private bool _disposed;
    public DrawingManager Manager { get; } = new() { ShowGrid = false, SnapToGrid = false, ShowSelectionHandles = false };
    public SketchShape EditedShape { get; }
    public long Edits { get; private set; }
    public string Case { get; }
    public SKRect WorldBounds { get; }

    public RendererStressScene(bool mixed)
    {
        SimpleDrawFonts.EnsureLoaded();
        Case = mixed ? "5000 mixed sketch tools / text edit" : "5000 rough rectangles / rectangle edit";
        const int columns = 71;
        const float cell = 900f / columns;
        try
        {
            for (var i = 0; i < ObjectCount; i++)
            {
                var shape = new SketchShape
                {
                    Name = "Renderer stress " + (i + 1), Kind = mixed ? (SketchShapeKind)(i % 7) : SketchShapeKind.Rectangle,
                    X = 30 + i % columns * cell, Y = 30 + i / columns * cell,
                    Width = cell * .7f, Height = cell * .5f,
                    Roughness = 1.5f, SketchSeed = i + 1, Stroke = "#334155", Fill = "#22b9a7d2"
                };
                _shapes.Add(shape);
                if (shape.IsPath) shape.PathData = shape.Kind == SketchShapeKind.Freehand ? "0,0;0.2,0.7;0.5,0.2;1,1" : "0,0;1,1";
                if (shape.Kind == SketchShapeKind.Text)
                {
                    shape.DisplayText = "Stress this\nidea";
                    var size = shape.MeasureText(); shape.Width = size.Width; shape.Height = size.Height;
                }
                Manager.AddComponent(shape);
            }
            WorldBounds = new(_shapes.Min(s => s.X) - 8, _shapes.Min(s => s.Y) - 8,
                _shapes.Max(s => s.X + s.Width) + 8, _shapes.Max(s => s.Y + s.Height) + 8);
            EditedShape = _shapes[mixed ? 6 : 0];
            Manager.SelectionManager.SelectComponent(EditedShape);
            Manager.HistoryManager.Clear();
        }
        catch { Dispose(); throw; }
    }

    public void Fit(float width, float height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        Manager.Zoom = Math.Clamp(Math.Min(width / WorldBounds.Width, height / WorldBounds.Height) * .9f, .1f, 5f);
        Manager.PanOffset = new((width - WorldBounds.Width * Manager.Zoom) / 2 - WorldBounds.Left * Manager.Zoom,
            (height - WorldBounds.Height * Manager.Zoom) / 2 - WorldBounds.Top * Manager.Zoom);
    }

    public void Edit(bool right)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Manager.GetComponents().Count != ObjectCount || Manager.SelectionManager.SelectedComponents.Count != 1
            || !ReferenceEquals(Manager.SelectionManager.SelectedComponents[0], EditedShape))
            throw new InvalidOperationException("The renderer stress scene or selected object changed.");
        var before = EditedShape.X;
        Manager.MoveSelectedComponents(new(right ? 1 : -1, 0));
        if (EditedShape.X != before + (right ? 1 : -1)) throw new InvalidOperationException("The shared history move did not execute.");
        Edits++;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Manager.HistoryManager.Clear();
        Manager.ClearComponents();
        // The shared renderer retains its last partition lists between frames.
        // An empty paint releases those references before this temporary scene exits.
        using (var empty = SKSurface.Create(new SKImageInfo(1, 1))) Manager.Draw(empty.Canvas);
        foreach (var shape in _shapes) shape.Dispose();
        _shapes.Clear();
    }
}
