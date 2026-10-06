using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.SimpleDraw.Tests;

public sealed class DrawingOverlayTests
{
    private readonly ITestOutputHelper _output;
    private int _frames;
    public DrawingOverlayTests(ITestOutputHelper output) => _output=output;
    public static IEnumerable<object[]> Viewports() => new[] { .41f, .875f, 1.75f }
        .SelectMany(zoom => new[] { 1.25f, 2f }.Select(dpi => new object[] { zoom, dpi }));
    public static IEnumerable<object[]> Drafts() => new[] { DrawingTool.Rectangle, DrawingTool.Ellipse,
        DrawingTool.Diamond, DrawingTool.Line, DrawingTool.Arrow, DrawingTool.Freehand }
        .SelectMany(tool => Viewports().Select(viewport => new object[] { tool, viewport[0], viewport[1] }));

    private static DrawingSession Board(float zoom)
    {
        SimpleDrawFonts.EnsureLoaded();
        var board = new DrawingSession(); board.ResizeViewport(640,480);
        board.Manager.Zoom = zoom; board.Manager.PanOffset = new(12.375f,7.125f);
        var origin = board.ScreenToWorld(new(160,130));
        board.Manager.AddComponent(new SketchShape { Kind = SketchShapeKind.Ellipse, X = origin.X, Y = origin.Y,
            Width = 160/zoom, Height = 130/zoom, Fill = "#88c9e5d3", Stroke = "#334155", Opacity = .7f, Roughness = 1.5f, SketchSeed = 42 });
        board.Manager.AddComponent(new SketchShape { Kind = SketchShapeKind.Rectangle, X = origin.X + 35/zoom, Y = origin.Y + 20/zoom,
            Width = 130/zoom, Height = 100/zoom, Fill = "#887c3aed", Stroke = "#e06b56", Opacity = .7f });
        board.Manager.HistoryManager.Clear(); return board;
    }
    private void EqualFrame(DrawingSession board, SceneFrameCache cache, float dpi, int cssWidth = 640, int cssHeight = 480)
    {
        var info = new SKImageInfo((int)(cssWidth*dpi),(int)(cssHeight*dpi));
        using var expected = new SKBitmap(info); using var actual = new SKBitmap(info); using var overlay = new SKBitmap(info);
        using var normalSurface = SKSurface.Create(info,expected.GetPixels(),expected.RowBytes);
        using var cachedSurface = SKSurface.Create(info,actual.GetPixels(),actual.RowBytes);
        using var inkSurface = SKSurface.Create(info,overlay.GetPixels(),overlay.RowBytes);
        foreach (var canvas in new[] { normalSurface.Canvas,cachedSurface.Canvas,inkSurface.Canvas })
        { canvas.Save(); canvas.Translate(.125f,.375f); canvas.Scale(dpi); }
        var history = board.Manager.HistoryManager.UndoCount;
        var states = board.Manager.GetComponents().Select(s => (s,s.X,s.Y,s.Width,s.Height,s.IsVisible)).ToArray();
        normalSurface.Canvas.Clear(SKColors.White); board.Manager.Draw(normalSurface.Canvas); board.DrawOverlay(normalSurface.Canvas);
        var footprint = board.GetOverlayInkBounds(); Assert.True(footprint.HasValue);
        inkSurface.Canvas.Clear(SKColors.Transparent); board.DrawOverlay(inkSurface.Canvas);
        var deviceInk = inkSurface.Canvas.TotalMatrix.MapRect(footprint.Value); deviceInk.Inflate(2,2);
        var pixels = overlay.GetPixelSpan();
        for (var y=0;y<info.Height;y++) for (var x=0;x<info.Width;x++)
            if (pixels[y*overlay.RowBytes+x*4+3] != 0)
                Assert.True(!footprint.Value.IsEmpty && x>=deviceInk.Left && x<=deviceInk.Right && y>=deviceInk.Top && y<=deviceInk.Bottom,
                    $"Overlay pixel {x},{y} outside {deviceInk}, zoom={board.Manager.Zoom}, dpi={dpi}");
        board.Manager.RenderingHelper.DrawFrame(cachedSurface,SKColors.White,cache,board.DrawOverlay,board.GetOverlayInkBounds);
        Assert.True(expected.GetPixelSpan().SequenceEqual(actual.GetPixelSpan()),
            $"Actual session overlay differs: zoom={board.Manager.Zoom}, dpi={dpi}, damage={cache.LastDamage}");
        Assert.Equal(history,board.Manager.HistoryManager.UndoCount);
        Assert.Equal(states,board.Manager.GetComponents().Select(s => (s,s.X,s.Y,s.Width,s.Height,s.IsVisible)).ToArray());
        _output.WriteLine($"overlay frame={++_frames}; zoom={board.Manager.Zoom}; dpi={dpi}; css={cssWidth}x{cssHeight}; exact pixels=True; footprint contained=True");
    }

    [Theory] [MemberData(nameof(Drafts))]
    public void EveryPointerDraftMoveStyleCancelCommitAndUndoMatchesOrdinaryRenderer(DrawingTool tool,float zoom,float dpi)
    {
        var board=Board(zoom); board.Tool=tool; board.StrokeWidth=20; board.Roughness=2;
        using var cache=new SceneFrameCache(); EqualFrame(board,cache,dpi);
        board.PointerDown(new(200.125f,170.375f)); EqualFrame(board,cache,dpi);
        board.PointerMove(new(240.375f,170.375f)); EqualFrame(board,cache,dpi); // horizontal/degenerate ink
        board.PointerMove(new(260.125f,220.625f)); EqualFrame(board,cache,dpi);
        board.Draft!.Stroke="#2875bc"; board.Draft.Opacity=.3f; EqualFrame(board,cache,dpi);
        board.CancelGesture(); EqualFrame(board,cache,dpi);
        Assert.Equal(0,board.Manager.HistoryManager.UndoCount); Assert.Equal(2,board.Manager.GetComponents().Count);
        board.PointerDown(new(190.25f,180.125f)); board.PointerMove(new(230.5f,210.75f)); EqualFrame(board,cache,dpi);
        board.PointerUp(new(265.125f,240.375f)); EqualFrame(board,cache,dpi);
        Assert.Equal(1,board.Manager.HistoryManager.UndoCount); Assert.Equal(3,board.Manager.GetComponents().Count);
        board.Undo(); EqualFrame(board,cache,dpi); board.Redo(); EqualFrame(board,cache,dpi);
        Assert.True(cache.PartialFrameCount>=4);
    }

    [Theory] [MemberData(nameof(Viewports))]
    public void RealSelectionMarqueeEraserAndEndpointHandlesMatchFullFrames(float zoom,float dpi)
    {
        var board=Board(zoom); using var cache=new SceneFrameCache(); EqualFrame(board,cache,dpi);
        board.Tool=DrawingTool.Select; board.SelectShape(board.Manager.GetComponents()[0]); EqualFrame(board,cache,dpi);
        board.SelectShape(board.Manager.GetComponents()[1],true); EqualFrame(board,cache,dpi);
        board.Manager.MoveSelectedComponents(new(.375f,.125f)); EqualFrame(board,cache,dpi); board.Undo(); EqualFrame(board,cache,dpi);
        board.PointerDown(new(30.125f,40.375f)); EqualFrame(board,cache,dpi);
        board.PointerMove(new(90.25f,40.375f)); EqualFrame(board,cache,dpi); // degenerate marquee
        board.PointerMove(new(300.25f,260.125f)); EqualFrame(board,cache,dpi); board.CancelGesture(); EqualFrame(board,cache,dpi);
        board.Tool=DrawingTool.Eraser; board.PointerDown(new(220,180)); Assert.NotEmpty(board.ErasePreview); EqualFrame(board,cache,dpi);
        board.PointerMove(new(240,200)); EqualFrame(board,cache,dpi); board.CancelGesture(); EqualFrame(board,cache,dpi);
        foreach (var kind in new[] { SketchShapeKind.Line,SketchShapeKind.Arrow })
        {
            var line=new SketchShape { Kind=kind,StrokeWidth=20 }; line.SetWorldPath(new[] { board.ScreenToWorld(new(120,300)),board.ScreenToWorld(new(360,300)) });
            board.Manager.AddComponent(line); board.Tool=DrawingTool.Select; board.SelectShape(line); EqualFrame(board,cache,dpi);
            board.PointerDown(new(360,300)); board.PointerMove(new(340.125f,330.375f)); EqualFrame(board,cache,dpi);
            board.PointerUp(new(340.125f,330.375f)); EqualFrame(board,cache,dpi); board.Undo(); EqualFrame(board,cache,dpi);
        }
    }

    [Theory] [MemberData(nameof(Viewports))]
    public void KeyboardCursorAndDraftAreRemovedAfterCancelOrViewportResize(float zoom,float dpi)
    {
        var board=Board(zoom); board.Tool=DrawingTool.Rectangle; using var cache=new SceneFrameCache(); EqualFrame(board,cache,dpi);
        board.KeyboardEnter(); EqualFrame(board,cache,dpi); board.MoveKeyboardCursor(new(30.125f,0)); EqualFrame(board,cache,dpi);
        board.MoveKeyboardCursor(new(0,30.375f)); EqualFrame(board,cache,dpi);
        board.CancelGesture(); EqualFrame(board,cache,dpi); Assert.False(board.HasKeyboardGesture);
        board.KeyboardEnter(); board.MoveKeyboardCursor(new(20,20)); EqualFrame(board,cache,dpi);
        board.ResizeViewport(320,360); Assert.False(board.HasKeyboardGesture); Assert.Null(board.Draft);
        EqualFrame(board,cache,dpi,320,360); Assert.Equal(0,board.Manager.HistoryManager.UndoCount);
        Assert.Equal(2,board.Manager.GetComponents().Count);
        board.ResizeViewport(640,480); EqualFrame(board,cache,dpi);
    }
}
