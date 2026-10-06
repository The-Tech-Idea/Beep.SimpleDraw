using System.Globalization;
using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class DrawingTests
{
    private static DrawingSession Board()
    {
        var board = new DrawingSession { Tool = DrawingTool.Rectangle };
        board.PointerDown(new(100, 100)); board.PointerMove(new(220, 180)); board.PointerUp(new(220, 180));
        return board;
    }

    [Fact] public void ReverseDragNormalizesAndCreatesOneUndoEntry()
    {
        var board = new DrawingSession { Tool = DrawingTool.Diamond };
        board.PointerDown(new(220, 180)); board.PointerMove(new(150, 130)); board.PointerUp(new(100, 100));
        var shape = Assert.IsType<SketchShape>(Assert.Single(board.Manager.GetComponents()));
        Assert.Equal(100, shape.X); Assert.Equal(120, shape.Width);
        Assert.Equal(SketchShapeKind.Diamond, shape.Kind);
        Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        board.Undo(); Assert.Empty(board.Manager.GetComponents());
        board.Redo(); Assert.Single(board.Manager.GetComponents());
    }

    [Fact] public void DragUndoAndCancelRestoreOriginalCoordinates()
    {
        var board = Board(); board.Tool = DrawingTool.Select;
        board.PointerDown(new(130, 130)); board.PointerMove(new(150, 150)); board.PointerUp(new(160, 160));
        var shape = Assert.IsType<SketchShape>(Assert.Single(board.Manager.GetComponents()));
        Assert.Equal(130, shape.X); Assert.Equal(2, board.Manager.HistoryManager.UndoCount);
        board.Undo(); Assert.Equal(100, shape.X);
        board.Redo(); Assert.Equal(130, shape.X);
        board.PointerDown(new(150, 150)); board.PointerMove(new(190, 190)); board.CancelGesture();
        Assert.Equal(130, shape.X); Assert.Equal(2, board.Manager.HistoryManager.UndoCount);
    }

    [Fact] public void CancelledOrTinyCreationDoesNotEnterSceneOrHistory()
    {
        var board = new DrawingSession { Tool = DrawingTool.Ellipse };
        board.PointerDown(new(10, 10)); board.PointerMove(new(90, 90)); board.CancelGesture();
        Assert.Empty(board.Manager.GetComponents()); Assert.Null(board.Draft);
        board.PointerDown(new(10, 10)); board.PointerUp(new(11, 11));
        Assert.Empty(board.Manager.GetComponents()); Assert.False(board.Manager.CanUndo);
    }

    [Fact] public void ZoomPreservesAnchorAndDrawingUsesWorldCoordinates()
    {
        var board = new DrawingSession { Tool = DrawingTool.Ellipse };
        board.Manager.PanOffset = new(20, 30);
        var before = board.ScreenToWorld(new(200, 200));
        board.ZoomAt(new(200, 200), 2);
        Assert.Equal(before, board.ScreenToWorld(new(200, 200)));
        board.PointerDown(new(200, 200)); board.PointerUp(new(400, 360));
        var shape = Assert.Single(board.Manager.GetComponents());
        Assert.Equal(before.X, shape.X); Assert.Equal(100, shape.Width);
    }

    [Fact] public void PanDoesNotEnterDrawingHistory()
    {
        var board = Board(); var count = board.Manager.HistoryManager.UndoCount;
        board.PointerDown(new(50, 50), pan: true); board.PointerUp(new(80, 70));
        Assert.Equal(new SKPoint(30, 20), board.Manager.PanOffset);
        Assert.Equal(count, board.Manager.HistoryManager.UndoCount);
    }

    [Fact] public void NativeFileRoundtripsAllShapesAndStylesAcrossCulture()
    {
        var board = Board();
        foreach (var kind in new[] { DrawingTool.Ellipse, DrawingTool.Diamond })
        { board.Tool = kind; board.Stroke = "#7c3aed"; board.Fill = "#ede9fe"; board.StrokeWidth = 2.5f;
            board.PointerDown(new(300, 100)); board.PointerUp(new(430, 190)); }
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var json = SimpleDrawDocument.Serialize(board.Manager);
            Assert.DoesNotContain("Version=", json);
            var restored = new DrawingSession(); restored.OpenDocument(json);
            Assert.Equal(3, restored.Manager.GetComponents().Count);
            var last = Assert.IsType<SketchShape>(restored.Manager.GetComponents()[2]);
            Assert.Equal(SketchShapeKind.Diamond, last.Kind); Assert.Equal(2.5f, last.StrokeWidth);
            Assert.Equal("#ede9fe", last.Fill); Assert.False(restored.Manager.CanUndo);
            Assert.Equal(json, SimpleDrawDocument.Serialize(restored.Manager));
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }

    [Theory]
    [InlineData("type", "System.IO.FileInfo")]
    [InlineData("width", -1)]
    [InlineData("width", 100001)]
    [InlineData("name", null)]
    public void InvalidImportPreservesCurrentScene(string key, object? value)
    {
        var board = Board(); var original = Assert.Single(board.Manager.GetComponents());
        var node = JsonNode.Parse(SimpleDrawDocument.Serialize(board.Manager))!;
        var component = node["diagram"]!["components"]![0]!;
        component[key] = value is int number ? JsonValue.Create(number) : JsonValue.Create((string?)value);
        Assert.Throws<InvalidDataException>(() => board.OpenDocument(node.ToJsonString()));
        Assert.Same(original, Assert.Single(board.Manager.GetComponents()));
    }

    [Fact] public void UnsupportedSchemaAndOversizedInputAreRejected()
    {
        var json = JsonNode.Parse(SimpleDrawDocument.Serialize(Board().Manager))!;
        json["version"] = 42;
        Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Parse(json.ToJsonString()));
        Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Parse(new string('x', SimpleDrawDocument.MaxBytes + 1)));
    }

    [Fact] public void InvalidColorAndUnknownPropertiesAreRejected()
    {
        var json = JsonNode.Parse(SimpleDrawDocument.Serialize(Board().Manager))!;
        json["diagram"]!["components"]![0]!["propertyBag"]!["Stroke"] = "url(evil)";
        Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Parse(json.ToJsonString()));
        json["diagram"]!["components"]![0]!["propertyBag"]!["Stroke"] = "#334155";
        json["diagram"]!["components"]![0]!["propertyBag"]!["IsStatic"] = "true";
        Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Parse(json.ToJsonString()));
    }

    [Fact] public void ShapeHitTestsUseGeometryRatherThanBoundingBox()
    {
        var shape = new SketchShape { Kind = SketchShapeKind.Ellipse, X = 0, Y = 0, Width = 100, Height = 100 };
        Assert.True(shape.ContainsPoint(new(50, 50))); Assert.False(shape.ContainsPoint(new(2, 2)));
        shape.Kind = SketchShapeKind.Diamond;
        Assert.True(shape.ContainsPoint(new(50, 1))); Assert.False(shape.ContainsPoint(new(90, 90)));
    }

    [Fact] public void ExistingTrustedSceneLoaderRestoresNewSketchComponent()
    {
        var board = Board(); var original = (SketchShape)board.Manager.GetComponents()[0];
        original.Kind = SketchShapeKind.Diamond; original.Stroke = "#7c3aed"; original.Fill = "#ede9fe";
        original.StrokeWidth = 4;
        var restored = new DrawingManager(); restored.LoadFromDto(board.Manager.ToDto());
        var shape = Assert.IsType<SketchShape>(Assert.Single(restored.GetComponents()));
        Assert.Equal(original.Kind, shape.Kind); Assert.Equal(original.Fill, shape.Fill);
        Assert.Equal(original.Stroke, shape.Stroke); Assert.Equal(original.StrokeWidth, shape.StrokeWidth);
        Assert.Equal(original.X, shape.X); Assert.Equal(original.Width, shape.Width);
    }

    [Fact] public void SharedRendererPaintsShapesAndExportExcludesViewport()
    {
        var board = Board(); var shape = (SketchShape)board.Manager.GetComponents()[0];
        shape.Fill = "#7c3aed";
        board.Manager.PanOffset = new(500, 500);
        using var surface = SKSurface.Create(new SKImageInfo(300, 220));
        surface.Canvas.Clear(SKColors.White);
        board.Manager.RenderingHelper.DrawForExport(surface.Canvas);
        using var bitmap = SKBitmap.FromImage(surface.Snapshot());
        Assert.Equal(SKColor.Parse("#7c3aed"), bitmap.GetPixel(150, 140));
        Assert.Equal(SKColors.White, bitmap.GetPixel(10, 10));
    }

    [Fact] public void DivergentEditClearsRedoAndDeleteCanBeUndone()
    {
        var board = Board(); board.DeleteSelected(); Assert.Empty(board.Manager.GetComponents());
        board.Undo(); Assert.Single(board.Manager.GetComponents());
        board.Tool = DrawingTool.Ellipse; board.PointerDown(new(300, 100)); board.PointerUp(new(400, 200));
        Assert.False(board.Manager.CanRedo); Assert.Equal(2, board.Manager.GetComponents().Count);
    }
}
