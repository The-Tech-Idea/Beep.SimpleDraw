using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.SimpleDraw.Tests;

// Feasibility experiment only: the supplied device rectangle is deliberately
// fixed and is not a production damage detector.
public sealed class RetainedClipTrialTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(.41f, false, true)] [InlineData(.875f, false, true)] [InlineData(1.75f, false, true)]
    [InlineData(.41f, true, true)] [InlineData(.875f, true, true)] [InlineData(1.75f, true, true)]
    [InlineData(.41f, false, false)] [InlineData(.875f, false, false)] [InlineData(1.75f, false, false)]
    [InlineData(.41f, true, false)] [InlineData(.875f, true, false)] [InlineData(1.75f, true, false)]
    public void RecreatedSurfaceWithRetainedPixelsMustMatchFullFrame(float zoom, bool grid, bool isolatedOverlap)
    {
        SimpleDrawFonts.EnsureLoaded();
        var board = new DrawingSession();
        board.Manager.Zoom = zoom; board.Manager.PanOffset = new(12.375f, 7.125f);
        board.Manager.ShowGrid = grid;
        using var text = new SketchShape { Kind = SketchShapeKind.Text, X = 30.25f, Y = 40.125f,
            DisplayText = "WWW iii\nidea", TextFontSize = 24, Stroke = "#e06b56" };
        board.Manager.AddComponent(text);
        using var overlap = new SketchShape { Kind = SketchShapeKind.Ellipse, X = 70, Y = 60,
            Width = 250, Height = 130, Fill = "#88c9e5d3", Stroke = "#80334155", Opacity = isolatedOverlap ? .7f : 1 };
        board.Manager.AddComponent(overlap);
        var others = new List<SketchShape>();
        try
        {
            for (var i = 0; i < 80; i++)
            {
                var other = new SketchShape { Kind = SketchShapeKind.Text, X = 210 + i % 4 * 30.25f,
                    Y = 20 + i / 4 * 13.125f, DisplayText = "Overlapping WWW", TextFontSize = 13.25f,
                    Stroke = "#80334155" };
                others.Add(other); board.Manager.AddComponent(other);
            }
            using var retained = new SKBitmap(new SKImageInfo(500, 400));
            using var expected = new SKBitmap(new SKImageInfo(500, 400));
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
                Paint(expected, false); Paint(retained, true);
                var e = expected.GetPixelSpan().ToArray(); var a = retained.GetPixelSpan().ToArray();
                var differing = Enumerable.Range(0, e.Length).Where(i => e[i] != a[i]).ToArray();
                var max = differing.Length == 0 ? 0 : differing.Max(i => Math.Abs(e[i] - a[i]));
                var bounds = differing.Length == 0 ? "none" : $"{differing.Min(i => i / 4 % 500)},{differing.Min(i => i / 2000)}..{differing.Max(i => i / 4 % 500)},{differing.Max(i => i / 2000)}";
                output.WriteLine($"zoom={zoom}; grid={grid}; isolated overlap={isolatedOverlap}; step={step}; changed channels={differing.Length}; max={max}; device bounds={bounds}");
                Assert.Equal(e, a);
            }

            void Paint(SKBitmap bitmap, bool clipped)
            {
                // Mirror vendor CPU surface recreation over the same allocation.
                using var surface = SKSurface.Create(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
                var canvas = surface.Canvas; var saves = canvas.SaveCount; canvas.Save();
                if (clipped) canvas.ClipRect(new SKRect(15, 15, 350, 320), SKClipOperation.Intersect, false);
                canvas.Clear(SKColor.Parse("#ffd9e1e7")); board.Manager.Draw(canvas);
                canvas.RestoreToCount(saves);
            }
        }
        finally { foreach (var other in others) other.Dispose(); }
    }
}
