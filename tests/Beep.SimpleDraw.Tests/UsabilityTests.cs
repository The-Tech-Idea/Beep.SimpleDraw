using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class UsabilityTests
{
    [Fact] public void ActualKeyboardOnlyBrowserDownloadRestoresCommittedContentAndMovement()
    {
        var board = new DrawingSession(); board.OpenDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "keyboard.json")));
        var shapes = board.Manager.GetComponents().Cast<SketchShape>().ToArray();
        Assert.Equal(new[] { SketchShapeKind.Rectangle, SketchShapeKind.Line, SketchShapeKind.Freehand, SketchShapeKind.Text }, shapes.Select(s=>s.Kind));
        Assert.Equal(641, shapes[0].X); Assert.Equal(60, shapes[0].Width); Assert.Equal(40, shapes[0].Height);
        Assert.Equal("Keyboard idea", shapes[3].DisplayText); Assert.False(board.Manager.CanUndo);
        Assert.DoesNotContain(shapes, s=>s.DisplayText.Contains("Cancelled"));
    }
    [Theory]
    [InlineData(DrawingTool.Rectangle, SketchShapeKind.Rectangle)]
    [InlineData(DrawingTool.Ellipse, SketchShapeKind.Ellipse)]
    [InlineData(DrawingTool.Diamond, SketchShapeKind.Diamond)]
    [InlineData(DrawingTool.Line, SketchShapeKind.Line)]
    [InlineData(DrawingTool.Arrow, SketchShapeKind.Arrow)]
    [InlineData(DrawingTool.Freehand, SketchShapeKind.Freehand)]
    public void KeyboardCreatesThroughExistingGestureAndCommitsOnce(DrawingTool tool, SketchShapeKind kind)
    {
        var board = new DrawingSession { Tool = tool }; board.ResizeViewport(320, 600);
        board.KeyboardEnter(); Assert.True(board.HasKeyboardGesture);
        board.MoveKeyboardCursor(new(40, 0)); board.MoveKeyboardCursor(new(0, 30)); board.KeyboardEnter();
        var shape = Assert.IsType<SketchShape>(Assert.Single(board.Manager.GetComponents()));
        Assert.Equal(kind, shape.Kind); Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        Assert.False(board.HasGesture); Assert.False(board.HasKeyboardGesture);
        board.Undo(); Assert.Empty(board.Manager.GetComponents()); board.Redo(); Assert.Single(board.Manager.GetComponents());
    }

    [Fact] public void RotationCancelsKeyboardPreviewAndPreservesHistoryAndTextDraft()
    {
        var board = new DrawingSession { Tool = DrawingTool.Rectangle }; board.KeyboardEnter(); board.MoveKeyboardCursor(new(80, 50));
        board.ResizeViewport(600, 320);
        Assert.Null(board.Draft); Assert.False(board.HasGesture); Assert.False(board.Manager.CanUndo);
        Assert.Equal(new SKPoint(300, 160), board.KeyboardCursor);
        board.Tool = DrawingTool.Text; board.KeyboardEnter(); var anchor = board.TextAnchor;
        board.ResizeViewport(320, 600); Assert.True(board.HasTextEdit); Assert.Equal(anchor, board.TextAnchor);
        board.CommitText("Kept draft"); Assert.Single(board.Manager.GetComponents());
    }

    [Fact] public void PinchPromotionRollsBackMoveAndKeepsAnchorUnderMovingCentroidWithoutHistory()
    {
        var board = new DrawingSession { Tool = DrawingTool.Rectangle };
        board.PointerDown(new(100, 100)); board.PointerUp(new(200, 180));
        var shape = (SketchShape)Assert.Single(board.Manager.GetComponents()); var json = SimpleDrawDocument.Serialize(board.Manager);
        board.Tool = DrawingTool.Select; board.PointerDown(new(120, 120)); board.PointerMove(new(150, 140));
        board.BeginViewportGesture(new(160, 120)); Assert.Equal(100, shape.X);
        var anchor = board.ScreenToWorld(new(160, 120)); board.UpdateViewportGesture(new(180, 140), 2);
        Assert.Equal(anchor, board.ScreenToWorld(new(180, 140))); Assert.Equal(2, board.Manager.Zoom);
        board.UpdateViewportGesture(new(200, 150), 100); Assert.Equal(5, board.Manager.Zoom);
        board.EndViewportGesture(); Assert.False(board.HasGesture);
        Assert.Equal(1, board.Manager.HistoryManager.UndoCount); Assert.Equal(json, SimpleDrawDocument.Serialize(board.Manager));
    }

    [Fact] public void NumericResizeScalesSelectionAndUndoRestoresIdentityAndGeometry()
    {
        var board = new DrawingSession { Tool = DrawingTool.Rectangle };
        board.PointerDown(new(100,100)); board.PointerUp(new(200,200));
        board.PointerDown(new(250,100)); board.PointerUp(new(300,200));
        board.Tool = DrawingTool.Select; board.Manager.SelectAllComponents();
        var before = SimpleDrawDocument.Serialize(board.Manager); var count = board.Manager.HistoryManager.UndoCount;
        board.ResizeSelection(400, 300);
        Assert.Equal(400, board.SelectionBounds!.Value.Width); Assert.Equal(300, board.SelectionBounds.Value.Height);
        Assert.Equal(count + 1, board.Manager.HistoryManager.UndoCount); board.Undo();
        Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager));
        Assert.Throws<ArgumentException>(() => board.ResizeSelection(float.NaN, 80));
        Assert.Throws<ArgumentException>(() => board.ResizeSelection(200_000, 80));
        Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager));
    }

    [Fact] public void FitDrawingUsesCurrentCssViewportAndDoesNotChangeDocumentOrHistory()
    {
        var board = new DrawingSession { Tool = DrawingTool.Rectangle }; board.ResizeViewport(320, 640);
        board.PointerDown(new(1000,1000)); board.PointerUp(new(1400,1200)); var before = SimpleDrawDocument.Serialize(board.Manager);
        board.FitDrawing(); var s = board.Manager.GetComponents()[0];
        Assert.InRange(s.X*board.Manager.Zoom+board.Manager.PanOffset.X, 23, 25);
        Assert.InRange((s.X+s.Width)*board.Manager.Zoom+board.Manager.PanOffset.X, 295, 297);
        Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager)); Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        var cursor = board.KeyboardCursor; board.MoveKeyboardCursor(new(float.NaN, 0)); Assert.Equal(cursor, board.KeyboardCursor);
        board.MoveKeyboardCursor(new(-1000, 1000)); Assert.Equal(new SKPoint(0, 640), board.KeyboardCursor);
    }
}
