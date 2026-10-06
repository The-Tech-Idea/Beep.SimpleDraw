using System.Globalization;
using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class PathEditingTests
{
    private static SketchShape Draw(DrawingSession session, DrawingTool tool, SKPoint from, SKPoint to)
    {
        session.Tool = tool; session.PointerDown(from); session.PointerUp(to);
        return (SketchShape)session.Manager.GetComponents()[^1];
    }
    [Theory]
    [InlineData(100, 100, 10, 20)]
    [InlineData(100, 100, 10, 100)]
    [InlineData(100, 100, 100, 10)]
    public void ReversedArrowPreservesEndpointsAndPointsTowardRelease(float x1, float y1, float x2, float y2)
    {
        var session = new DrawingSession(); var arrow = Draw(session, DrawingTool.Arrow, new(x1, y1), new(x2, y2));
        Assert.Equal(new SKPoint(x1, y1), arrow.WorldPoint(0));
        Assert.Equal(new SKPoint(x2, y2), arrow.WorldPoint(1));
        var head = arrow.ArrowHead(); var direction = new SKPoint(x2 - x1, y2 - y1);
        var left = head.Left - arrow.WorldPoint(1);
        Assert.True(left.X * direction.X + left.Y * direction.Y < 0);
        Assert.Equal(1, session.Manager.HistoryManager.UndoCount);
        session.Undo(); Assert.Empty(session.Manager.GetComponents()); session.Redo(); Assert.Same(arrow, Assert.Single(session.Manager.GetComponents()));
    }
    [Fact] public void PathHitTestsFollowSegmentsAndArrowHead()
    {
        var shape = new SketchShape { Kind = SketchShapeKind.Arrow };
        shape.SetWorldPath([new(10, 10), new(110, 110)]);
        Assert.True(shape.ContainsPoint(new(50, 53), 4));
        Assert.False(shape.ContainsPoint(new(20, 100), 4));
        Assert.True(shape.ContainsPoint(shape.ArrowHead().Left, 0));
        Assert.True(shape.IntersectsSegment(new(10, 110), new(110, 10), 0));
        Assert.False(shape.IntersectsSegment(new(0, 100), new(0, 200), 0));
    }
    [Fact] public void FreehandSamplesAreLocalBoundedAndOneTransaction()
    {
        var session = new DrawingSession { Tool = DrawingTool.Freehand };
        session.PointerDown(new(100, 100));
        for (int i = 1; i <= 80; i++) session.PointerMove(new(100 + i, 100 + (i % 2 == 0 ? 20 : 0)));
        session.PointerUp(new(180, 120));
        var pen = (SketchShape)Assert.Single(session.Manager.GetComponents());
        Assert.True(pen.PathPoints.Count > 60);
        Assert.All(pen.PathPoints, p => { Assert.InRange(p.X, 0, 1); Assert.InRange(p.Y, 0, 1); });
        var original = pen.WorldPoint(20); pen.X += 30; pen.Y += 40;
        Assert.Equal(original + new SKPoint(30, 40), pen.WorldPoint(20));
        Assert.Equal(1, session.Manager.HistoryManager.UndoCount);
        session.Undo(); Assert.Empty(session.Manager.GetComponents());
        session.Tool = DrawingTool.Freehand; session.PointerDown(new(0, 0)); session.PointerMove(new(30, 20)); session.CancelGesture();
        Assert.Empty(session.Manager.GetComponents()); Assert.True(session.Manager.CanRedo);
        Assert.Throws<ArgumentException>(() => pen.PathData = string.Join(';', Enumerable.Repeat("0,0", SketchShape.MaxPathPoints + 1)));
    }
    [Fact] public void NativePathsRoundtripAcrossCultureAndRejectMalformedGeometryAtomically()
    {
        var session = new DrawingSession(); var arrow = Draw(session, DrawingTool.Arrow, new(240, 120), new(10, 10));
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var json = SimpleDrawDocument.Serialize(session.Manager);
            var restored = new DrawingSession(); restored.OpenDocument(json);
            var shape = (SketchShape)Assert.Single(restored.Manager.GetComponents());
            Assert.Equal(arrow.PathData, shape.PathData); Assert.Equal(arrow.WorldPoint(1), shape.WorldPoint(1));
            Assert.Equal(json, SimpleDrawDocument.Serialize(restored.Manager));
            var node = JsonNode.Parse(json)!; node["diagram"]!["components"]![0]!["propertyBag"]!["PathData"] = "NaN,0;1,1";
            Assert.Throws<InvalidDataException>(() => restored.OpenDocument(node.ToJsonString()));
            Assert.Same(shape, Assert.Single(restored.Manager.GetComponents()));
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }
    [Fact] public void VersionOneMigratesOnlyKnownOriginalShapes()
    {
        var session = new DrawingSession(); Draw(session, DrawingTool.Rectangle, new(10, 20), new(110, 120));
        var node = JsonNode.Parse(SimpleDrawDocument.Serialize(session.Manager))!; node["version"] = 1;
        node["diagram"]!["schemaVersion"] = 2;
        var component = (JsonObject)node["diagram"]!["components"]![0]!; component.Remove("id");
        var bag = (JsonObject)component["propertyBag"]!;
        foreach (var key in new[] { "PathData", "DisplayText", "TextFontSize", "GroupId", "Roughness", "SketchSeed" }) bag.Remove(key);
        var restored = new DrawingSession(); restored.OpenDocument(node.ToJsonString());
        Assert.Single(restored.Manager.GetComponents());
        Assert.Contains($"\"version\": {SimpleDrawDocument.CurrentVersion}", SimpleDrawDocument.Serialize(restored.Manager));
        bag["Kind"] = "Arrow";
        Assert.Throws<InvalidDataException>(() => restored.OpenDocument(node.ToJsonString()));
    }
    [Theory]
    [InlineData("0,0;0,0")]
    [InlineData("0,0")]
    [InlineData("0,0;1.1,1")]
    [InlineData("0,0;0.5,0.5;1,1")]
    public void InvalidOrDegenerateArrowGeometryIsRejected(string path)
    {
        var session = new DrawingSession(); var arrow = Draw(session, DrawingTool.Arrow, new(10, 10), new(100, 100));
        var json = JsonNode.Parse(SimpleDrawDocument.Serialize(session.Manager))!;
        json["diagram"]!["components"]![0]!["propertyBag"]!["PathData"] = path;
        Assert.Throws<InvalidDataException>(() => session.OpenDocument(json.ToJsonString()));
        Assert.Same(arrow, Assert.Single(session.Manager.GetComponents()));
    }
    [Fact] public void EraserSweepDeletesOnceUndoRestoresPaintOrderAndCancelPreservesScene()
    {
        var session = new DrawingSession();
        var a = Draw(session, DrawingTool.Rectangle, new(100, 100), new(140, 140));
        var b = Draw(session, DrawingTool.Ellipse, new(200, 100), new(240, 140));
        var c = Draw(session, DrawingTool.Diamond, new(300, 100), new(340, 140));
        session.Tool = DrawingTool.Eraser; session.PointerDown(new(80, 120)); session.PointerMove(new(260, 120));
        Assert.Equal(2, session.ErasePreview.Count); Assert.Equal(3, session.Manager.GetComponents().Count);
        session.CancelGesture(); Assert.Empty(session.ErasePreview); Assert.Equal(3, session.Manager.HistoryManager.UndoCount);
        session.PointerDown(new(80, 120)); session.PointerUp(new(260, 120));
        Assert.Same(c, Assert.Single(session.Manager.GetComponents())); Assert.Equal(4, session.Manager.HistoryManager.UndoCount);
        session.Undo(); Assert.Equal(new[] { a, b, c }, session.Manager.GetComponents().Cast<SketchShape>());
        session.Redo(); Assert.Same(c, Assert.Single(session.Manager.GetComponents()));
    }
    [Fact] public void MultiselectionMovementAndStyleEditHaveOneUndoEach()
    {
        var session = new DrawingSession();
        var a = Draw(session, DrawingTool.Rectangle, new(100, 100), new(140, 140));
        var b = Draw(session, DrawingTool.Rectangle, new(200, 100), new(240, 140));
        session.Tool = DrawingTool.Select; session.PointerDown(new(120, 120)); session.PointerUp(new(120, 120));
        session.PointerDown(new(220, 120), additive: true); session.PointerUp(new(220, 120));
        Assert.Equal(2, session.Manager.SelectionManager.SelectedComponents.Count);
        session.PointerDown(new(120, 120)); session.PointerUp(new(130, 140));
        Assert.Equal(110, a.X); Assert.Equal(210, b.X); Assert.Equal(3, session.Manager.HistoryManager.UndoCount);
        session.Undo(); Assert.Equal(100, a.X); Assert.Equal(200, b.X);
        session.SetStyle(stroke: "#7c3aed", fill: "#ede9fe", width: 4);
        Assert.Equal(a.Stroke, b.Stroke); Assert.Equal(3, session.Manager.HistoryManager.UndoCount); Assert.False(session.Manager.CanRedo);
        session.Undo(); Assert.Equal("#334155", a.Stroke); Assert.Equal(2, b.StrokeWidth);
        session.Redo(); Assert.Equal("#ede9fe", b.Fill);
        Assert.Throws<ArgumentException>(() => session.SetStyle(stroke: "bad", width: 8));
        Assert.Equal(4, a.StrokeWidth); Assert.Equal("#7c3aed", b.Stroke);
    }
    [Fact] public void SharedRendererPaintsHorizontalLineAtGeometryRatherThanBox()
    {
        var session = new DrawingSession(); var line = Draw(session, DrawingTool.Line, new(200, 100), new(20, 100));
        line.Stroke = "#7c3aed"; line.StrokeWidth = 4;
        using var surface = SKSurface.Create(new SKImageInfo(240, 160)); surface.Canvas.Clear(SKColors.White);
        session.Manager.RenderingHelper.DrawForExport(surface.Canvas);
        using var bitmap = SKBitmap.FromImage(surface.Snapshot());
        Assert.Equal(SKColor.Parse("#7c3aed"), bitmap.GetPixel(80, 100)); Assert.Equal(SKColors.White, bitmap.GetPixel(80, 80));
    }
}
