using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.SimpleDraw.Tests;

// Native feasibility only: leaf-ink contributor filtering, fixed damage, no history/overlay damage detector.
public sealed class FullCanvasContributorTrialTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(.41f, false, true)] [InlineData(.875f, false, true)] [InlineData(1.75f, false, true)]
    [InlineData(.41f, true, true)] [InlineData(.875f, true, true)] [InlineData(1.75f, true, true)]
    [InlineData(.41f, false, false)] [InlineData(.875f, false, false)] [InlineData(1.75f, false, false)]
    [InlineData(.41f, true, false)] [InlineData(.875f, true, false)] [InlineData(1.75f, true, false)]
    public void FullCanvasContributorsWithRegionalPixelCopyMustMatchFullFrame(float zoom, bool grid, bool isolatedOverlap)
    {
        SimpleDrawFonts.EnsureLoaded();
        var board = new DrawingSession(); var manager = board.Manager;
        manager.Zoom = zoom; manager.PanOffset = new(12.375f, 7.125f); manager.ShowGrid = grid;
        using var text = new SketchShape { Kind = SketchShapeKind.Text, X = 30.25f, Y = 40.125f,
            DisplayText = "WWW iii\nidea", TextFontSize = 24, Stroke = "#e06b56" };
        manager.AddComponent(text);
        using var overlap = new SketchShape { Kind = SketchShapeKind.Ellipse, X = 70, Y = 60,
            Width = 250, Height = 130, Fill = "#88c9e5d3", Stroke = "#80334155", Opacity = isolatedOverlap ? .7f : 1 };
        manager.AddComponent(overlap);
        var others = new List<SketchShape>(); var excluded = new List<SketchShape>();
        try
        {
            for (var i = 0; i < 80; i++)
            {
                var other = new SketchShape { Kind = SketchShapeKind.Text, X = 210 + i % 4 * 30.25f,
                    Y = 20 + i / 4 * 13.125f, DisplayText = "Overlapping WWW", TextFontSize = 13.25f,
                    Stroke = "#80334155" };
                others.Add(other); manager.AddComponent(other);
                // Visible in the retained right-hand region; strictly outside the copied rectangle.
                var omitted = new SketchShape { Kind = SketchShapeKind.Text,
                    X = (370 + i % 3 * 30 - manager.PanOffset.X) / zoom,
                    Y = (25 + i / 3 * 13 - manager.PanOffset.Y) / zoom,
                    DisplayText = "Unchanged", TextFontSize = 20 / zoom, Stroke = "#80334155" };
                excluded.Add(omitted); manager.AddComponent(omitted);
            }
            using var retained = new SKBitmap(new SKImageInfo(500, 400));
            using var expected = new SKBitmap(new SKImageInfo(500, 400));
            using var scratch = new SKBitmap(new SKImageInfo(500, 400));
            Paint(retained, false);
            for (var step = 0; step < 8; step++)
            {
                text.X += step % 2 == 0 ? 1 : -1; text.Y += .125f;
                if (step == 1) text.TextFontSize = 13.25f;
                if (step == 2) text.Stroke = "#80334155";
                if (step == 3) text.DisplayText = "New\nlayout";
                if (step == 4) text.TextFontSize = 36;
                if (step == 5) text.Opacity = .4f;
                if (step == 6) text.DisplayText = "";
                if (step == 7) { text.Opacity = 1; text.DisplayText = "WWW iii"; }
                var size = text.MeasureText(); text.Width = size.Width; text.Height = size.Height;
                Paint(expected, false); Paint(scratch, true);
                var before = retained.GetPixelSpan().ToArray(); var source = scratch.GetPixelSpan();
                var target = retained.GetPixelSpan();
                for (var y = 15; y < 320; y++) source.Slice(y * scratch.RowBytes + 15 * 4, 335 * 4)
                    .CopyTo(target.Slice(y * retained.RowBytes + 15 * 4, 335 * 4));
                var e = expected.GetPixelSpan().ToArray(); var a = retained.GetPixelSpan().ToArray();
                var changed = e.Zip(a).Count(pair => pair.First != pair.Second);
                output.WriteLine($"zoom={zoom}; grid={grid}; isolated overlap={isolatedOverlap}; step={step}; changed channels={changed}; omitted=80");
                Assert.Equal(e, a);
                // Explicitly prove the visible omitted area survived the byte copy.
                for (var y = 0; y < 400; y++) Assert.Equal(before.AsSpan(y * retained.RowBytes + 350 * 4, 150 * 4).ToArray(),
                    a.AsSpan(y * retained.RowBytes + 350 * 4, 150 * 4).ToArray());
            }
            void Paint(SKBitmap bitmap, bool subset)
            {
                var omitted = new List<SketchShape>();
                if (subset) foreach (var shape in others.Concat(excluded).Prepend(overlap).Prepend(text))
                {
                    Assert.True(shape.TryGetInkBounds(out var ink));
                    var device = new SKRect(ink.Left * zoom + manager.PanOffset.X, ink.Top * zoom + manager.PanOffset.Y,
                        ink.Right * zoom + manager.PanOffset.X, ink.Bottom * zoom + manager.PanOffset.Y);
                    device.Inflate(2, 2);
                    if (!device.IntersectsWith(new SKRect(15, 15, 350, 320))) { shape.IsVisible = false; omitted.Add(shape); }
                }
                if (subset) Assert.True(omitted.Count >= 80);
                try
                {
                    using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
                    surface.Canvas.Clear(SKColor.Parse("#ffd9e1e7"));
                    var matrix = surface.Canvas.TotalMatrix; var clip = surface.Canvas.DeviceClipBounds; var saves = surface.Canvas.SaveCount;
                    manager.Draw(surface.Canvas);
                    Assert.Equal(matrix, surface.Canvas.TotalMatrix); Assert.Equal(clip, surface.Canvas.DeviceClipBounds);
                    Assert.Equal(saves, surface.Canvas.SaveCount);
                }
                finally { foreach (var shape in omitted) shape.IsVisible = true; }
            }
        }
        finally { foreach (var other in others.Concat(excluded)) other.Dispose(); }
    }
}
