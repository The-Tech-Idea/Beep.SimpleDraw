using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class SceneFrameCacheTests
{
    public static IEnumerable<object[]> Cases() => Enum.GetValues<SketchShapeKind>()
        .SelectMany(kind => new[] { .41f, .875f, 1.75f }.Select(zoom => new object[] { kind, zoom }));

    private static SketchShape Shape(SketchShapeKind kind, float x = 35.125f) => new()
    {
        Kind = kind, X = x, Y = 43.375f, Width = 110, Height = 75,
        Stroke = "#b0334155", Fill = "#88c9e5d3", Opacity = .7f, Roughness = 1.5f,
        SketchSeed = 42, DisplayText = "WWW iii\nIdea", TextFontSize = 24,
        PathData = "0,0;0.3,0.7;1,1"
    };
    private static void EqualFrame(DrawingManager manager, SceneFrameCache cache, float dpi = 1,
        SKColor? background = null, Action<SKCanvas>? overlay = null, Func<SKRect?>? bounds = null,
        bool clip = false, int width = 640)
    {
        using var expected = new SKBitmap(new SKImageInfo(width, 480));
        using var actual = new SKBitmap(expected.Info);
        using var ordinary = SKSurface.Create(expected.Info, expected.GetPixels(), expected.RowBytes);
        using var retained = SKSurface.Create(actual.Info, actual.GetPixels(), actual.RowBytes);
        foreach (var canvas in new[] { ordinary.Canvas, retained.Canvas })
        {
            canvas.Save(); canvas.Translate(.125f, .375f); canvas.Scale(dpi);
            if (clip) canvas.ClipRect(new SKRect(15, 20, 350, 280));
        }
        var color = background ?? SKColors.White;
        ordinary.Canvas.Clear(color); manager.Draw(ordinary.Canvas); overlay?.Invoke(ordinary.Canvas);
        // Each call uses a new allocation: correctness cannot depend on target pixels surviving.
        manager.RenderingHelper.DrawFrame(retained, color, cache, overlay, bounds);
        Assert.True(expected.GetPixelSpan().SequenceEqual(actual.GetPixelSpan()),
            $"Raster differs: damage={cache.LastDamage}, drawn={cache.LastDrawnComponents}, skipped={cache.LastSkippedComponents}");
    }

    [Theory] [MemberData(nameof(Cases))]
    public void AutomaticDamageMatchesFullRendererAfterEveryVisualMutation(SketchShapeKind kind, float zoom)
    {
        SimpleDrawFonts.EnsureLoaded();
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false, Zoom = zoom, PanOffset = new(12.125f, 7.375f) };
        using var moving = Shape(kind); using var overlapping = Shape(SketchShapeKind.Ellipse, 90);
        using var distant = Shape(SketchShapeKind.Text, 480);
        manager.AddComponent(moving); manager.AddComponent(overlapping); manager.AddComponent(distant);
        using var cache = new SceneFrameCache();
        EqualFrame(manager, cache, 1.25f); Assert.Equal(1, cache.FullFrameCount);
        EqualFrame(manager, cache, 1.25f); Assert.Equal(1, cache.ReusedFrameCount); Assert.Equal(3, cache.LastSkippedComponents);
        var history = manager.HistoryManager.UndoCount;
        for (var step = 0; step < 10; step++)
        {
            switch (step)
            {
                case 0: moving.X += .375f; moving.Y += .125f; break;
                case 1: moving.StrokeWidth = 20; moving.Roughness = 2; break;
                case 2: moving.Width = 25; moving.Height = 12; break;
                case 3: moving.DisplayText = "New WWW\n \niii"; moving.TextFontSize = 36; break;
                case 4: moving.PathData = "1,0;0.7,0.2;0,1"; moving.SketchSeed = 173; break;
                case 5: moving.Stroke = "#e06b56"; moving.Fill = "#227c3aed"; moving.Opacity = .3f; break;
                case 6: moving.IsVisible = false; break;
                case 7: moving.IsVisible = true; break;
                case 8: moving.Kind = SketchShapeKind.Text; moving.DisplayText = ""; break;
                case 9: moving.Kind = kind; moving.DisplayText = "Changed"; break;
            }
            EqualFrame(manager, cache, 1.25f);
        }
        Assert.Equal(history, manager.HistoryManager.UndoCount);
        Assert.True(moving.IsVisible); Assert.True(overlapping.IsVisible); Assert.True(distant.IsVisible);
        Assert.Equal(1, cache.FullFrameCount); Assert.True(cache.PartialFrameCount >= 8);
    }

    [Fact]
    public void MembershipOrderHistoryAndDocumentReplacementInvalidateRetainedPixels()
    {
        var board = new DrawingSession(); var manager = board.Manager;
        using var first = Shape(SketchShapeKind.Rectangle); using var second = Shape(SketchShapeKind.Ellipse, 80);
        foreach (var shape in new[] { first, second }) { shape.Name = "Shape"; shape.DisplayText = ""; shape.PathData = ""; }
        manager.AddComponent(first); manager.AddComponent(second); using var cache = new SceneFrameCache();
        EqualFrame(manager, cache);
        manager.SetComponentOrder(manager.GetComponents().Reverse().ToArray()); EqualFrame(manager, cache);
        board.Undo(); EqualFrame(manager, cache); board.Redo(); EqualFrame(manager, cache);
        manager.RemoveComponent(first); EqualFrame(manager, cache);
        board.Undo(); EqualFrame(manager, cache); board.Redo(); EqualFrame(manager, cache);
        var native = SimpleDrawDocument.Serialize(manager); board.OpenDocument(native); EqualFrame(manager, cache);
        Assert.Equal(8, cache.FullFrameCount);
        var other = new DrawingSession(); other.OpenDocument(native); EqualFrame(other.Manager, cache);
        Assert.Equal(9, cache.FullFrameCount);
    }

    [Fact]
    public void ViewportGridBackgroundSizeAndDeviceMatrixChangesRepaintWholeFrame()
    {
        var manager = new DrawingManager { ShowSelectionHandles = false, ShowGrid = false };
        using var shape = Shape(SketchShapeKind.Rectangle); manager.AddComponent(shape); using var cache = new SceneFrameCache();
        EqualFrame(manager, cache); manager.Zoom = .875f; EqualFrame(manager, cache);
        manager.PanOffset = new(-12.5f, 17.25f); EqualFrame(manager, cache);
        manager.ShowGrid = true; EqualFrame(manager, cache); manager.GridSpacing = 35; EqualFrame(manager, cache);
        shape.X += .25f; EqualFrame(manager, cache); Assert.Equal(1, cache.PartialFrameCount);
        EqualFrame(manager, cache, 1.5f); EqualFrame(manager, cache, 1.5f, SKColors.AliceBlue);
        EqualFrame(manager, cache, 1.5f, SKColors.AliceBlue, width: 700);
        Assert.Equal(8, cache.FullFrameCount);
    }

    [Fact]
    public void OverlayChangesInsideSameBoundsMoveAndRemovalMatchFullRenderer()
    {
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false };
        using var shape = Shape(SketchShapeKind.Ellipse); manager.AddComponent(shape); using var cache = new SceneFrameCache();
        var rect = new SKRect(60.125f, 55.375f, 100.125f, 90.375f); var color = SKColors.Red;
        void Overlay(SKCanvas canvas) { using var paint = new SKPaint { Color = color, IsAntialias = true }; canvas.DrawOval(rect, paint); }
        EqualFrame(manager, cache, 1.25f, overlay: Overlay, bounds: () => rect);
        color = SKColors.Purple; EqualFrame(manager, cache, 1.25f, overlay: Overlay, bounds: () => rect);
        rect.Offset(180, 120); EqualFrame(manager, cache, 1.25f, overlay: Overlay, bounds: () => rect);
        EqualFrame(manager, cache, 1.25f); EqualFrame(manager, cache, 1.25f);
        Assert.Equal(1, cache.FullFrameCount); Assert.Equal(3, cache.PartialFrameCount); Assert.Equal(1, cache.ReusedFrameCount);
    }

    [Fact]
    public void UnknownEffectsAndClipUseOrdinaryRenderingThenRequireAFullCacheFrame()
    {
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false };
        using var shape = Shape(SketchShapeKind.Rectangle); manager.AddComponent(shape); using var cache = new SceneFrameCache();
        EqualFrame(manager, cache);
        void Overlay(SKCanvas canvas) { using var paint = new SKPaint { Color = SKColors.Blue }; canvas.DrawCircle(400, 300, 8, paint); }
        EqualFrame(manager, cache, overlay: Overlay); EqualFrame(manager, cache);
        EqualFrame(manager, cache, clip: true); EqualFrame(manager, cache);
        shape.IsStatic = true; EqualFrame(manager, cache); shape.IsStatic = false; EqualFrame(manager, cache);
        using var child = Shape(SketchShapeKind.Ellipse, 180); shape.Children.Add(child); EqualFrame(manager, cache);
        shape.Children.Clear(); EqualFrame(manager, cache);
        manager.WorldOverlay = Overlay; EqualFrame(manager, cache); manager.WorldOverlay = null; EqualFrame(manager, cache);
        manager.ShowSelectionHandles = true; EqualFrame(manager, cache); manager.ShowSelectionHandles = false; EqualFrame(manager, cache);
        Assert.Equal(13, cache.FullFrameCount);
        var connected = new DrawingSession(); connected.InsertConnectedTemplate();
        EqualFrame(connected.Manager, cache); EqualFrame(connected.Manager, cache);
        Assert.Equal(15, cache.FullFrameCount);
    }

    [Fact]
    public void FailedOverlayOrFootprintCannotLeaveAReusablePartialFrame()
    {
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false };
        using var shape = Shape(SketchShapeKind.Rectangle); manager.AddComponent(shape); using var cache = new SceneFrameCache();
        EqualFrame(manager, cache);
        using var surface = SKSurface.Create(new SKImageInfo(640, 480));
        Assert.Throws<InvalidOperationException>(() => manager.RenderingHelper.DrawFrame(surface, SKColors.White, cache,
            _ => throw new InvalidOperationException(), () => new SKRect(20, 20, 50, 50)));
        EqualFrame(manager, cache);
        Assert.Throws<InvalidOperationException>(() => manager.RenderingHelper.DrawFrame(surface, SKColors.White, cache,
            _ => { }, () => throw new InvalidOperationException()));
        EqualFrame(manager, cache); Assert.Equal(3, cache.FullFrameCount);
    }

    [Fact]
    public void PhaseProfileCountsActualContributorsAndEverySkippedComponent()
    {
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false };
        using var first = Shape(SketchShapeKind.Text); using var second = Shape(SketchShapeKind.Ellipse, 480);
        manager.AddComponent(first); manager.AddComponent(second); using var cache = new SceneFrameCache();
        EqualFrame(manager, cache); first.X += 1;
        SceneRenderProfile? profile = null; manager.RenderingHelper.FrameMeasured = value => profile = value;
        EqualFrame(manager, cache);
        Assert.NotNull(profile); Assert.Equal(2, profile.Groups.Values.Sum(group => group.Attempts + group.SkippedAttempts));
        Assert.Equal(1, cache.LastDrawnComponents); Assert.Equal(1, cache.LastSkippedComponents);
        Assert.Equal(1, profile.Groups.Values.Sum(group => group.SkippedAttempts));
    }
}
