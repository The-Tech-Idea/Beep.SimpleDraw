using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Beep.Skia.Flowchart;

namespace Beep.SimpleDraw.Core.Documents;

public sealed record ExportLayout(SKRect Bounds, int Width, int Height, float Scale);
public static class DrawingExport
{
    public const int MaxDimension = 8192;
    public const long MaxPixels = 16_000_000;
    public static ExportLayout Layout(DrawingManager manager, float scale = 1)
    {
        if (!float.IsFinite(scale) || scale < .25f || scale > 4) throw new InvalidDataException("Export scale must be 0.25 through 4.");
        SimpleDrawDocument.Serialize(manager); // Same untrusted/live geometry boundary as native files.
        var shapes = manager.GetComponents().ToArray();
        if (shapes.Length == 0) throw new InvalidDataException("Add something to the drawing before exporting an image.");
        // Arrowheads, stroke, rough outline and antialiasing fit inside this conservative margin.
        var margin = shapes.Max(s => s is SketchShape sketch ? sketch.StrokeWidth / 2 + sketch.Roughness : s is FlowchartControl flow ? flow.NodeStrokeWidth/2+10 : 32) + 8;
        var points = shapes.SelectMany(s => s is SketchShape { Kind: SketchShapeKind.Arrow } arrow
            ? new[] { new SKPoint(s.X, s.Y), new SKPoint(s.X + s.Width, s.Y + s.Height), arrow.ArrowHead().Left, arrow.ArrowHead().Right }
            : new[] { new SKPoint(s.X, s.Y), new SKPoint(s.X + s.Width, s.Y + s.Height) }).ToArray();
        var bounds = new SKRect(points.Min(p => p.X) - margin, points.Min(p => p.Y) - margin,
            points.Max(p => p.X) + margin, points.Max(p => p.Y) + margin);
        foreach (var line in manager.GetLines().Cast<ConnectionLine>()) bounds = SKRect.Union(bounds, line.GetPathAndLabelBounds());
        var w = Math.Ceiling(bounds.Width * (double)scale); var h = Math.Ceiling(bounds.Height * (double)scale);
        if (w > MaxDimension || h > MaxDimension || w * h > MaxPixels)
            throw new InvalidDataException("Export exceeds 8192 pixels per side or 16 million pixels. Reduce scale or move shapes closer together.");
        return new(bounds, Math.Max(1, (int)w), Math.Max(1, (int)h), scale);
    }
    private static void Paint(DrawingManager manager, SKCanvas canvas, ExportLayout layout, bool background)
    {
        if (background) canvas.DrawColor(SKColor.Parse("#fcfcf9"));
        canvas.Scale(layout.Scale); canvas.Translate(-layout.Bounds.Left, -layout.Bounds.Top);
        manager.RenderingHelper.DrawForExport(canvas);
    }
    public static byte[] Png(DrawingManager manager, float scale = 1, bool background = true)
    {
        var layout = Layout(manager, scale);
        using var surface = SKSurface.Create(new SKImageInfo(layout.Width, layout.Height))
            ?? throw new InvalidDataException("The image could not be allocated. Try a smaller export.");
        surface.Canvas.Clear(SKColors.Transparent); Paint(manager, surface.Canvas, layout, background);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    public static byte[] Svg(DrawingManager manager, float scale = 1, bool background = true)
    {
        var layout = Layout(manager, scale);
        using var stream = new SKDynamicMemoryWStream();
        // Text is converted to glyph outlines by Skia; no external fonts or active markup.
        using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, layout.Width, layout.Height), stream))
            Paint(manager, canvas, layout, background);
        using var data = stream.DetachAsData(); return data.ToArray();
    }
}
