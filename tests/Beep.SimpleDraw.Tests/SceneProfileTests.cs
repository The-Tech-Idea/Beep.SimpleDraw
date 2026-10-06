using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class SceneProfileTests
{
    private sealed class StaticOverlay : SkiaComponent
    {
        protected override void DrawContent(SKCanvas canvas, DrawingContext context)
        {
            using var paint = new SKPaint { Color = SKColors.Red, IsAntialias = true };
            canvas.DrawCircle(19.125f, 21.375f, 5, paint);
        }
    }
    [Theory]
    [InlineData(.41f)] [InlineData(.875f)] [InlineData(1.75f)]
    public void OptInScopesPreserveExactScenePixelsCanvasStateAndHistory(float zoom)
    {
        SimpleDrawFonts.EnsureLoaded();
        var board = new DrawingSession(); var manager = board.Manager;
        var kinds = Enum.GetValues<SketchShapeKind>();
        foreach (var kind in kinds)
        {
            var shape = new SketchShape { Kind = kind, X = 50.25f + (int)kind * 30, Y = 60.125f + (int)kind * 20,
                Width = 100, Height = 60, DisplayText = "CPU\nscopes", TextFontSize = 24,
                Stroke = "#80334155", Fill = "#806d28d9", Opacity = .8f, Roughness = 1, SketchSeed = 42 };
            if (shape.IsPath) shape.PathData = "0,0;0.3,0.7;1,1";
            manager.AddComponent(shape);
        }
        manager.AddComponent(new StaticOverlay { IsStatic = true });
        manager.ShowGrid = true; manager.Zoom = zoom; manager.PanOffset = new(12.375f, 7.125f);
        manager.WorldOverlay = canvas => { using var paint = new SKPaint { Color = SKColors.Blue }; canvas.DrawRect(5, 10, 30, 20, paint); };
        var history = manager.HistoryManager.UndoCount;
        var reports = new List<SceneRenderProfile>();
        var ordinary = Paint(manager);
        manager.RenderingHelper.FrameMeasured = reports.Add;
        Assert.Equal(ordinary, Paint(manager));
        Assert.Equal(ordinary, Paint(manager));
        manager.RenderingHelper.FrameMeasured = null;
        Assert.Equal(ordinary, Paint(manager));
        Assert.Equal(2, reports.Count); Assert.NotSame(reports[0], reports[1]);
        Assert.Equal(history, manager.HistoryManager.UndoCount);
        foreach (var profile in reports)
        {
            Assert.Equal(kinds.Select(k => "Sketch." + k).Order(), profile.Groups.Keys.Order());
            Assert.All(profile.Groups.Values, group => { Assert.Equal(1, group.Attempts); Assert.True(double.IsFinite(group.Milliseconds)); Assert.True(group.Milliseconds >= 0); });
            var phases = new[] { profile.PreparationMs, profile.GridMs, profile.ConnectionsMs, profile.AdornersMs, profile.StaticMs };
            Assert.All(phases, ms => { Assert.True(double.IsFinite(ms)); Assert.True(ms >= 0); });
            Assert.True(profile.TotalMs > 0);
            Assert.True(phases.Sum() + profile.Groups.Values.Sum(group => group.Milliseconds) <= profile.TotalMs + .01);
        }
    }
    private static byte[] Paint(DrawingManager manager)
    {
        using var surface = SKSurface.Create(new SKImageInfo(500, 400)); var canvas = surface.Canvas;
        canvas.Clear(SKColors.White); canvas.Translate(3.125f, 5.25f); canvas.ClipRect(new SKRect(5, 10, 450, 350));
        var matrix = canvas.TotalMatrix; var clip = canvas.LocalClipBounds; var saves = canvas.SaveCount;
        manager.Draw(canvas);
        Assert.Equal(matrix, canvas.TotalMatrix); Assert.Equal(clip, canvas.LocalClipBounds); Assert.Equal(saves, canvas.SaveCount);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
