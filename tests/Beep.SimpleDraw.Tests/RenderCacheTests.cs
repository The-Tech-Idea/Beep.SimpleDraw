using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class RenderCacheTests
{
    public RenderCacheTests() => SimpleDrawFonts.EnsureLoaded();
    [Theory]
    [InlineData(SketchShapeKind.Rectangle)] [InlineData(SketchShapeKind.Ellipse)] [InlineData(SketchShapeKind.Diamond)]
    [InlineData(SketchShapeKind.Line)] [InlineData(SketchShapeKind.Arrow)] [InlineData(SketchShapeKind.Freehand)] [InlineData(SketchShapeKind.Text)]
    public void OpaqueSketchLeavesPreserveNativeCanvasMatrixClipAndSaveStack(SketchShapeKind kind)
    {
        using var shape = new SketchShape { Kind = kind, X = 20.25f, Y = 30.125f, Width = 100, Height = 60,
            DisplayText = "Canvas\nstate", Roughness = 1.5f, SketchSeed = 42, Stroke = "#80334155" };
        if (shape.IsPath) shape.PathData = "0,0;0.3,0.7;1,1";
        using var surface = SKSurface.Create(new SKImageInfo(500, 400));
        var canvas = surface.Canvas; canvas.Translate(12.375f, 7.125f); canvas.Scale(.875f);
        canvas.ClipRect(new SKRect(5, 10, 450, 350));
        var matrix = canvas.TotalMatrix; var clip = canvas.LocalClipBounds; var saves = canvas.SaveCount;
        for (int i = 0; i < 3; i++)
        {
            shape.X += .125f; shape.StrokeWidth = 2 + i; shape.Draw(canvas, new DrawingContext());
            Assert.Equal(matrix, canvas.TotalMatrix); Assert.Equal(clip, canvas.LocalClipBounds); Assert.Equal(saves, canvas.SaveCount);
        }
    }
    [Theory] [InlineData(SketchShapeKind.Rectangle)] [InlineData(SketchShapeKind.Text)]
    public void SketchOpacityKeepsTheOriginalIsolatedLayerPixels(SketchShapeKind kind)
    {
        using var shape = new SketchShape { Kind = kind, X = 50.25f, Y = 70.125f, Width = 180, Height = 90,
            DisplayText = "Opacity\nlayer", Opacity = .4f, Stroke = "#e06b56", Fill = "#88c9e5d3" };
        using var expected = SKSurface.Create(new SKImageInfo(500, 400)); using var actual = SKSurface.Create(new SKImageInfo(500, 400));
        expected.Canvas.Clear(SKColors.White); actual.Canvas.Clear(SKColors.White);
        using var layerPaint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(.4f * 255)) };
        expected.Canvas.SaveLayer(new SKRect(shape.X, shape.Y, shape.X + shape.Width, shape.Y + shape.Height), layerPaint);
        shape.Opacity = 1; shape.Draw(expected.Canvas, new DrawingContext()); expected.Canvas.Restore();
        shape.Opacity = .4f; var saves = actual.Canvas.SaveCount; shape.Draw(actual.Canvas, new DrawingContext());
        Assert.Equal(saves, actual.Canvas.SaveCount);
        using var e = expected.Snapshot(); using var a = actual.Snapshot();
        using var ed = e.Encode(SKEncodedImageFormat.Png, 100); using var ad = a.Encode(SKEncodedImageFormat.Png, 100);
        Assert.Equal(ed.ToArray(), ad.ToArray());
    }
    private sealed class UnbalancedChild : SkiaComponent
    {
        protected override void DrawContent(SKCanvas canvas, DrawingContext context) { }
        public override void Draw(SKCanvas canvas, DrawingContext context)
        { canvas.Save(); canvas.Translate(73, 51); canvas.ClipRect(new SKRect(0, 0, 10, 10)); }
    }
    [Fact]
    public void SketchWithChildContentRetainsParentCanvasIsolation()
    {
        using var shape = new SketchShape(); using var child = new UnbalancedChild(); shape.Children.Add(child);
        using var surface = SKSurface.Create(new SKImageInfo(500, 400)); var canvas = surface.Canvas;
        canvas.Translate(12.375f, 7.125f); canvas.Scale(.875f);
        var matrix = canvas.TotalMatrix; var clip = canvas.LocalClipBounds; var saves = canvas.SaveCount;
        shape.Draw(canvas, new DrawingContext());
        Assert.Equal(matrix, canvas.TotalMatrix); Assert.Equal(clip, canvas.LocalClipBounds); Assert.Equal(saves, canvas.SaveCount);
    }
    [Theory]
    [InlineData(50f, 70f)] [InlineData(50.25f, 70.125f)]
    public void CachedTextMatchesTheOriginalAbsoluteGlyphCommands(float x, float y)
    {
        var board = new DrawingSession();
        var shape = new SketchShape { Name = "Text cache", Kind = SketchShapeKind.Text, X = x, Y = y,
            DisplayText = "Cache this\nidea", TextFontSize = 24, Stroke = "#e06b56" };
        var size = shape.MeasureText(); shape.Width = size.Width; shape.Height = size.Height;
        board.Manager.AddComponent(shape);
        using var surface = SKSurface.Create(new SKImageInfo(500, 400));
        using var font = new SKFont(TypefaceCache.GetRequired(SketchShape.TextFontFamily), shape.TextFontSize)
            { Hinting = SKFontHinting.None, Subpixel = true, LinearMetrics = true, Edging = SKFontEdging.Antialias };
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(shape.Stroke) };
        foreach (var dx in new[] { 0f, 1f, 17f })
        {
            shape.X = x + dx; surface.Canvas.Clear(SKColors.White);
            var baseline = shape.Y + 4 - font.Metrics.Ascent;
            foreach (var line in shape.DisplayText.Split('\n'))
            {
                DrawOriginalGlyphs(surface.Canvas, font, line, new(shape.X + 4, baseline), paint);
                baseline += shape.TextFontSize * 1.4f;
            }
            using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            Assert.Equal(data.ToArray(), Paint(board));
        }
        shape.Dispose();
    }
    // Independent pre-cache reference: do not derive expected pixels through the
    // production cache helper.
    private static void DrawOriginalGlyphs(SKCanvas canvas, SKFont font, string text, SKPoint baseline, SKPaint paint)
    {
        var glyphs = font.GetGlyphs(text);
        var positions = font.GetGlyphPositions(glyphs, baseline);
        for (int i = 0; i < glyphs.Length; i++)
        {
            using var path = font.GetGlyphPath(glyphs[i]);
            if (path is null) continue;
            canvas.Save();
            try { canvas.Translate(positions[i]); canvas.DrawPath(path, paint); }
            finally { canvas.Restore(); }
        }
    }
    [Theory]
    [InlineData(.41f)] [InlineData(.875f)] [InlineData(1.75f)]
    public void RecordedTextMatchesOriginalPixelsAcrossMovementLayoutColorAndViewport(float zoom)
    {
        var board = new DrawingSession();
        using var shape = new SketchShape { Kind = SketchShapeKind.Text, X = 50.25f, Y = 70.125f,
            DisplayText = "Cache this\nidea", TextFontSize = 24, Stroke = "#e06b56" };
        board.Manager.AddComponent(shape);
        using var expected = SKSurface.Create(new SKImageInfo(500, 400));
        using var actual = SKSurface.Create(new SKImageInfo(500, 400));
        for (int step = 0; step < 10; step++)
        {
            shape.X += step % 2 == 0 ? 1 : -1; shape.Y += .125f;
            if (step == 2) shape.TextFontSize = 36;
            if (step == 4) shape.DisplayText = "  Move  cache\n \nWW iii ";
            if (step == 6) shape.Stroke = "#80334155";
            if (step == 8) { shape.TextFontSize = 13.25f; shape.DisplayText = "New layout\nwide WWW"; }
            var size = shape.MeasureText(); shape.Width = size.Width; shape.Height = size.Height;
            using var font = new SKFont(TypefaceCache.GetRequired(SketchShape.TextFontFamily), shape.TextFontSize)
                { Hinting = SKFontHinting.None, Subpixel = true, LinearMetrics = true, Edging = SKFontEdging.Antialias };
            using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(shape.Stroke) };
            expected.Canvas.Clear(SKColors.White); actual.Canvas.Clear(SKColors.White);
            expected.Canvas.Save(); actual.Canvas.Save();
            expected.Canvas.Translate(12.375f, 7.125f); actual.Canvas.Translate(12.375f, 7.125f);
            expected.Canvas.Scale(zoom); actual.Canvas.Scale(zoom);
            var baseline = shape.Y + 4 - font.Metrics.Ascent;
            foreach (var line in shape.DisplayText.Split('\n'))
            {
                DrawOriginalGlyphs(expected.Canvas, font, line, new(shape.X + 4, baseline), paint);
                baseline += shape.TextFontSize * 1.4f;
            }
            board.Manager.RenderingHelper.DrawForExport(actual.Canvas);
            expected.Canvas.Restore(); actual.Canvas.Restore();
            using var expectedImage = expected.Snapshot(); using var actualImage = actual.Snapshot();
            using var expectedData = expectedImage.Encode(SKEncodedImageFormat.Png, 100);
            using var actualData = actualImage.Encode(SKEncodedImageFormat.Png, 100);
            Assert.Equal(expectedData.ToArray(), actualData.ToArray());
        }
    }
    private static byte[] Paint(DrawingSession board)
    {
        using var surface = SKSurface.Create(new SKImageInfo(500, 400));
        surface.Canvas.Clear(SKColors.White); board.Manager.RenderingHelper.DrawForExport(surface.Canvas);
        using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    private static void FreshMatches(DrawingSession board)
    {
        var json = SimpleDrawDocument.Serialize(board.Manager);
        var fresh = new DrawingSession(); fresh.OpenDocument(json);
        try { Assert.Equal(Paint(fresh), Paint(board)); Assert.Equal(Paint(board), Paint(board)); }
        finally { foreach (var shape in fresh.Manager.GetComponents()) shape.Dispose(); }
    }
    [Theory]
    [InlineData(SketchShapeKind.Rectangle)] [InlineData(SketchShapeKind.Ellipse)] [InlineData(SketchShapeKind.Diamond)]
    [InlineData(SketchShapeKind.Line)] [InlineData(SketchShapeKind.Arrow)] [InlineData(SketchShapeKind.Freehand)] [InlineData(SketchShapeKind.Text)]
    public void ReusedNativeDrawingResourcesMatchFreshSceneAfterEveryPixelAffectingMutation(SketchShapeKind kind)
    {
        var board = new DrawingSession();
        var shape = new SketchShape { Name = "Cache 1", Kind = kind, X = 50, Y = 70, Width = 140, Height = 80, Roughness = 1.5f, SketchSeed = 42 };
        if (shape.IsPath) shape.PathData = kind == SketchShapeKind.Freehand ? "0,0;0.3,0.7;1,1" : "0,0;1,1";
        if (kind == SketchShapeKind.Text) { shape.DisplayText = "Cache this\nidea"; var size = shape.MeasureText(); shape.Width = size.Width; shape.Height = size.Height; }
        board.Manager.AddComponent(shape); FreshMatches(board);
        shape.X += 23; shape.Y += 9; FreshMatches(board);
        shape.Stroke = "#e06b56"; shape.Fill = "#88c9e5d3"; shape.StrokeWidth = 4; FreshMatches(board);
        if (kind == SketchShapeKind.Text)
        {
            shape.DisplayText = "Changed\ntext"; shape.TextFontSize = 36;
            var size = shape.MeasureText(); shape.Width = size.Width; shape.Height = size.Height;
            FreshMatches(board);
        }
        else { shape.Width += 31; shape.Height += 19; FreshMatches(board); }
        shape.SketchSeed = 57; shape.Roughness = 0; FreshMatches(board);
        shape.Roughness = 2; FreshMatches(board);
        if (shape.IsPath) { shape.PathData = kind == SketchShapeKind.Freehand ? "1,0;0.7,0.2;0,1" : "1,0;0,1"; FreshMatches(board); }
        if (kind != SketchShapeKind.Text) { shape.Kind = SketchShapeKind.Rectangle; shape.PathData = ""; FreshMatches(board); }
        shape.Dispose(); shape.Dispose();
        Assert.Equal(Paint(new DrawingSession()), Paint(board));
    }
    [Fact]
    public void MoveUndoRedoInvalidateOnlyTheNormalSharedDrawingState()
    {
        var board = new DrawingSession(); board.Tool = DrawingTool.Rectangle;
        board.PointerDown(new(50, 50)); board.PointerUp(new(180, 130));
        var original = Paint(board); board.MoveSelected(new(30, 20)); FreshMatches(board);
        Assert.NotEqual(original, Paint(board)); board.Undo(); Assert.Equal(original, Paint(board));
        board.Redo(); FreshMatches(board); board.Undo(); Assert.Equal(original, Paint(board));
        board.Tool = DrawingTool.Select; board.SetStyle(stroke: "#e06b56", width: 4); FreshMatches(board);
        board.Undo(); Assert.Equal(original, Paint(board));
        foreach (var shape in board.Manager.GetComponents()) shape.Dispose();
    }
}
