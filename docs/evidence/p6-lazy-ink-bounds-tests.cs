using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class InkBoundsTests
{
    public static IEnumerable<object[]> ShapesAndZooms() => Enum.GetValues<SketchShapeKind>()
        .SelectMany(kind => new[] { .41f, .875f, 1.75f }.Select(zoom => new object[] { kind, zoom }));
    [Theory] [MemberData(nameof(ShapesAndZooms))]
    public void ReportedFootprintContainsEveryActualRasterPixelAfterMutations(SketchShapeKind kind, float zoom)
    {
        SimpleDrawFonts.EnsureLoaded();
        using var shape = new SketchShape { Kind = kind, X = 30.25f, Y = 40.125f, Width = 120, Height = 70,
            DisplayText = "Wide WWW iii\nidea", TextFontSize = 24, Stroke = "#80334155", Fill = "#88c9e5d3", SketchSeed = 42 };
        if (shape.IsPath) shape.PathData = "0,0;0.3,0.7;1,1";
        using var bitmap = new SKBitmap(new SKImageInfo(500, 400));
        for (var step = 0; step < 8; step++)
        {
            shape.X += .125f; shape.Y += .375f;
            if (step == 1) { shape.StrokeWidth = 20; shape.Roughness = 2; }
            if (step == 2) { shape.Width = 25; shape.Height = 12; }
            if (step == 3 && shape.Kind == SketchShapeKind.Text) { shape.DisplayText = "  WWW iii \n \nNew"; shape.TextFontSize = 13.25f; }
            if (step == 4) shape.Opacity = .4f;
            if (step == 5 && shape.IsPath) shape.PathData = "1,0;0.7,0.2;0,1";
            if (step == 6) { shape.Opacity = 1; shape.Stroke = "#e06b56"; shape.Roughness = 0; }
            if (step == 7 && shape.Kind == SketchShapeKind.Text) { shape.DisplayText = "Changed WWW"; shape.TextFontSize = 36; }
            using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
            var canvas = surface.Canvas; canvas.Clear(SKColors.Transparent);
            canvas.Translate(12.375f, 7.125f); canvas.Scale(zoom);
            shape.Draw(canvas, new DrawingContext());
            var ordinaryPixels = bitmap.GetPixelSpan().ToArray();
            // Query after a normal draw, then again after bounds were cached.
            Assert.True(shape.TryGetInkBounds(out var ink));
            Assert.True(shape.TryGetInkBounds(out var repeated)); Assert.Equal(ink, repeated);
            var device = canvas.TotalMatrix.MapRect(ink); device.Inflate(2, 2);
            canvas.Clear(SKColors.Transparent);
            shape.Draw(canvas, new DrawingContext());
            Assert.True(ordinaryPixels.AsSpan().SequenceEqual(bitmap.GetPixelSpan()),
                $"{kind}, zoom={zoom}, step={step}: a bounds query changed raster pixels");
            var pixels = bitmap.GetPixelSpan(); var painted = 0;
            for (var y = 0; y < bitmap.Height; y++) for (var x = 0; x < bitmap.Width; x++)
            {
                if (pixels[y * bitmap.RowBytes + x * 4 + 3] == 0) continue;
                painted++; Assert.True(x >= device.Left && x <= device.Right && y >= device.Top && y <= device.Bottom,
                    $"{kind}, zoom={zoom}, step={step}: pixel {x},{y} outside {device}");
            }
            Assert.True(painted > 0);
        }
    }
    [Fact]
    public void UnknownChildContentCannotClaimALeafFootprint()
    {
        using var parent = new SketchShape(); using var child = new SketchShape { X = 900, Y = 900 };
        parent.Children.Add(child); Assert.False(parent.TryGetInkBounds(out _));
    }
    [Fact]
    public void EmptyAndInvisibleLeavesHaveNoInk()
    {
        using var shape = new SketchShape { Kind = SketchShapeKind.Text, DisplayText = "" };
        Assert.True(shape.TryGetInkBounds(out var empty)); Assert.True(empty.IsEmpty);
        shape.Kind = SketchShapeKind.Rectangle; shape.IsVisible = false;
        Assert.True(shape.TryGetInkBounds(out empty)); Assert.True(empty.IsEmpty);
    }
}
