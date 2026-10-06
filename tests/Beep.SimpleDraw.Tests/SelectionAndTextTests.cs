using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class SelectionAndTextTests
{
    private static SketchShape Rectangle(DrawingSession board, float x = 100)
    { board.Tool = DrawingTool.Rectangle; board.PointerDown(new(x, 100)); board.PointerUp(new(x + 100, 180)); return (SketchShape)board.Manager.GetComponents()[^1]; }
    private static SketchShape Text(DrawingSession board, string text = "Hello\nworld")
    { board.BeginText(new(100, 260)); Assert.True(board.CommitText(text)); return (SketchShape)board.Manager.GetComponents()[^1]; }

    [Fact] public void MultilineTextUsesBundledMetricsPaintsAndRoundtripsIdentity()
    {
        var board = new DrawingSession(); var text = Text(board); var measured = text.MeasureText();
        Assert.Equal(measured.Width, text.Width); Assert.Equal(measured.Height, text.Height);
        Assert.Equal("Kalam", TypefaceCache.GetRequired(SketchShape.TextFontFamily).FamilyName);
        var json = SimpleDrawDocument.Serialize(board.Manager); var restored = new DrawingSession(); restored.OpenDocument(json);
        var copy = (SketchShape)Assert.Single(restored.Manager.GetComponents());
        Assert.Equal(text.Id, copy.Id); Assert.Equal(text.DisplayText, copy.DisplayText); Assert.Equal(json, SimpleDrawDocument.Serialize(restored.Manager));
        using var surface = SKSurface.Create(new SKImageInfo(320, 420)); surface.Canvas.Clear(SKColors.White);
        board.Manager.RenderingHelper.DrawForExport(surface.Canvas); using var bitmap = SKBitmap.FromImage(surface.Snapshot());
        Assert.Contains(Enumerable.Range(265, 60), y => Enumerable.Range(105, (int)text.Width).Any(x => bitmap.GetPixel(x, y) != SKColors.White));
    }
    [Fact] public void TextEditCancelEmptyAndUndoPreserveTheExistingObject()
    {
        var board = new DrawingSession(); var text = Text(board); var before = SketchEditAction.State.Capture(text);
        board.BeginText(new(text.X + 5, text.Y + 5)); board.CancelGesture();
        Assert.Equal(before, SketchEditAction.State.Capture(text)); Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        board.BeginText(new(text.X + 5, text.Y + 5)); Assert.False(board.CommitText("   "));
        Assert.Equal(before, SketchEditAction.State.Capture(text));
        board.BeginText(new(text.X + 5, text.Y + 5)); Assert.True(board.CommitText("A longer idea\nwith another line", 36));
        Assert.Same(text, Assert.Single(board.Manager.GetComponents())); Assert.Equal(2, board.Manager.HistoryManager.UndoCount);
        board.Undo(); Assert.Equal(before, SketchEditAction.State.Capture(text)); board.Redo(); Assert.Equal(36, text.TextFontSize);
    }
    [Fact] public void UnsupportedTextAndOversizedTextDoNotMutateSceneOrLoseEditor()
    {
        var board = new DrawingSession(); var text = Text(board); var before = SketchEditAction.State.Capture(text);
        board.BeginText(new(text.X + 5, text.Y + 5));
        Assert.Throws<ArgumentException>(() => board.CommitText("😀")); Assert.True(board.HasTextEdit);
        Assert.Throws<ArgumentException>(() => board.CommitText(new string('x', 4097)));
        Assert.Equal(before, SketchEditAction.State.Capture(text)); Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        board.CancelGesture(); board.BeginText(new(99_999, 0)); Assert.Throws<ArgumentException>(() => board.CommitText("too wide"));
        Assert.Same(text, Assert.Single(board.Manager.GetComponents()));
    }
    [Fact] public void FontSizeStyleChangesRecomputeBoundsAndUndoTogether()
    {
        var board = new DrawingSession(); var text = Text(board); board.SelectShape(text); var before = SketchEditAction.State.Capture(text);
        board.SetStyle(fontSize: 48, stroke: "#7c3aed"); Assert.Equal(text.MeasureText().Width, text.Width);
        Assert.Equal(2, board.Manager.HistoryManager.UndoCount); board.Undo(); Assert.Equal(before, SketchEditAction.State.Capture(text));
        board.Redo(); Assert.Equal(48, text.TextFontSize); Assert.Contains($"\"version\": {SimpleDrawDocument.CurrentVersion}", SimpleDrawDocument.Serialize(board.Manager));
    }
    [Fact] public void ResizeCommitAndCancelUseOneHistoryEntryAndStableIdentity()
    {
        var board = new DrawingSession(); var shape = Rectangle(board); var id = shape.Id; board.Tool = DrawingTool.Select;
        board.PointerDown(new(200, 180)); board.PointerMove(new(300, 260)); Assert.Equal(200, shape.Width);
        board.CancelGesture(); Assert.Equal(100, shape.Width); Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        board.PointerDown(new(200, 180)); board.PointerUp(new(300, 260)); Assert.Equal(200, shape.Width);
        Assert.Equal(2, board.Manager.HistoryManager.UndoCount); board.Undo(); Assert.Equal(100, shape.Width);
        board.Redo(); Assert.Equal(200, shape.Width); Assert.Equal(id, shape.Id);
    }
    [Fact] public void ArrowEndpointResizePreservesEndpointOrderAndCanBeCancelled()
    {
        var board = new DrawingSession { Tool = DrawingTool.Arrow }; board.PointerDown(new(300, 200)); board.PointerUp(new(100, 100));
        var arrow = (SketchShape)Assert.Single(board.Manager.GetComponents()); board.Tool = DrawingTool.Select;
        board.PointerDown(new(100, 100)); board.PointerMove(new(350, 250)); board.CancelGesture(); Assert.Equal(new SKPoint(100, 100), arrow.WorldPoint(1));
        board.PointerDown(new(100, 100)); board.PointerUp(new(350, 250));
        Assert.Equal(new SKPoint(300, 200), arrow.WorldPoint(0)); Assert.Equal(new SKPoint(350, 250), arrow.WorldPoint(1));
        board.Undo(); Assert.Equal(new SKPoint(100, 100), arrow.WorldPoint(1)); board.Redo(); Assert.Equal(new SKPoint(350, 250), arrow.WorldPoint(1));
    }
    [Fact] public void TextResizeKeepsPersistedBoundsConsistentWithFont()
    {
        var board = new DrawingSession(); var text = Text(board); board.SelectShape(text);
        board.PointerDown(new(text.X + text.Width, text.Y + text.Height)); board.PointerUp(new(text.X + text.Width * 2, text.Y + text.Height * 2));
        Assert.Equal(48, text.TextFontSize); Assert.Equal(text.MeasureText().Width, text.Width);
        Assert.NotNull(SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(board.Manager)));
    }
    [Fact] public void GroupSelectionMoveDuplicateRoundtripAndUngroupKeepMembershipExplicit()
    {
        var board = new DrawingSession(); var a = Rectangle(board); var b = Rectangle(board, 300);
        board.SelectShape(a); board.SelectShape(b, true); board.GroupSelection(); var group = a.GroupId;
        Assert.NotEqual(Guid.Empty, group); Assert.Equal(group, b.GroupId);
        board.Manager.SelectionManager.ClearSelection(); board.SelectShape(a); Assert.Equal(2, board.SelectedShapes.Count());
        board.MoveSelected(new(10, 20)); Assert.Equal(110, a.X); Assert.Equal(310, b.X);
        var history = board.Manager.HistoryManager.UndoCount; board.DuplicateSelection();
        Assert.Equal(history + 1, board.Manager.HistoryManager.UndoCount); var copies = board.SelectedShapes.ToArray();
        Assert.Equal(2, copies.Length); Assert.Equal(copies[0].GroupId, copies[1].GroupId); Assert.NotEqual(group, copies[0].GroupId);
        Assert.Equal(4, board.Manager.GetComponents().Select(s => s.Id).Distinct().Count());
        board.Undo(); Assert.Equal(2, board.Manager.GetComponents().Count); board.Redo(); Assert.Equal(4, board.Manager.GetComponents().Count);
        var json = SimpleDrawDocument.Serialize(board.Manager); var restored = new DrawingSession(); restored.OpenDocument(json);
        Assert.Equal(json, SimpleDrawDocument.Serialize(restored.Manager));
        board.SelectShape(a); board.GroupSelection(true); Assert.Equal(Guid.Empty, b.GroupId); board.Undo(); Assert.Equal(group, b.GroupId);
    }
    [Fact] public void MarqueeExpandsGroupsAndEraserRemovesWholeGroupOnce()
    {
        var board = new DrawingSession(); var a = Rectangle(board); var b = Rectangle(board, 300);
        board.SelectShape(a); board.SelectShape(b, true); board.GroupSelection(); board.Manager.SelectionManager.ClearSelection();
        board.Tool = DrawingTool.Select; board.PointerDown(new(90, 90)); board.PointerUp(new(210, 190));
        Assert.Equal(2, board.SelectedShapes.Count()); var count = board.Manager.HistoryManager.UndoCount;
        board.Tool = DrawingTool.Eraser; board.PointerDown(new(110, 110)); board.PointerUp(new(110, 110));
        Assert.Empty(board.Manager.GetComponents()); Assert.Equal(count + 1, board.Manager.HistoryManager.UndoCount);
        board.Undo(); Assert.Equal(new[] { a.Id, b.Id }, board.Manager.GetComponents().Select(s => s.Id));
    }
    [Fact] public void ReorderIsUndoableAndPreservesMembershipAndIdentity()
    {
        var board = new DrawingSession(); var a = Rectangle(board); var b = Rectangle(board, 130);
        board.SelectShape(b); board.ReorderSelection(false); Assert.Same(b, board.Manager.GetComponents()[0]);
        board.Undo(); Assert.Same(a, board.Manager.GetComponents()[0]); board.Redo(); Assert.Same(b, board.Manager.GetComponents()[0]);
        var count = board.Manager.HistoryManager.UndoCount; board.ReorderSelection(false); Assert.Equal(count, board.Manager.HistoryManager.UndoCount);
        Assert.Throws<ArgumentException>(() => board.Manager.SetComponentOrder([a, a])); Assert.Equal(2, board.Manager.GetComponents().Count);
    }
    [Fact] public void UncommittedHistoryBatchRollsBackWithoutLosingRedoOrPaintOrder()
    {
        var board = new DrawingSession(); var a = Rectangle(board); var b = Rectangle(board, 300); board.Undo();
        using (board.Manager.HistoryManager.BeginBatch()) { board.Manager.RemoveComponent(a); board.Manager.AddComponent(new SketchShape()); }
        Assert.Same(a, Assert.Single(board.Manager.GetComponents())); Assert.True(board.Manager.CanRedo); board.Redo(); Assert.Same(b, board.Manager.GetComponents()[1]);
    }
    [Fact] public void SketchGeometryAndPixelsRemainDeterministicAcrossPaintsAndRoundtrip()
    {
        var board = new DrawingSession(); var shape = Rectangle(board); board.SelectShape(shape); board.SetStyle(roughness: 1.5f);
        var points = shape.SketchOutline().ToArray(); Assert.Equal(points, shape.SketchOutline());
        var restored = new DrawingSession(); restored.OpenDocument(SimpleDrawDocument.Serialize(board.Manager));
        var copy = (SketchShape)Assert.Single(restored.Manager.GetComponents()); Assert.Equal(points, copy.SketchOutline());
        static byte[] Paint(DrawingSession session)
        { using var surface = SKSurface.Create(new SKImageInfo(300, 240)); surface.Canvas.Clear(SKColors.White); session.Manager.RenderingHelper.DrawForExport(surface.Canvas); using var image = surface.Snapshot(); using var data = image.Encode(SKEncodedImageFormat.Png, 100); return data.ToArray(); }
        Assert.Equal(Paint(board), Paint(board)); Assert.Equal(Paint(board), Paint(restored));
        shape.Width += 50; Assert.NotEqual(points[12], shape.SketchOutline()[12]);
    }
    [Theory] [InlineData(1, 3)] [InlineData(2, 4)]
    public void ActualEarlierBrowserFilesMigrateToCurrentSchema(int version, int count)
    {
        var board = new DrawingSession(); board.OpenDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"v{version}.json")));
        Assert.Equal(count, board.Manager.GetComponents().Count); Assert.All(board.Manager.GetComponents(), s => Assert.NotEqual(Guid.Empty, s.Id));
        Assert.Contains("\"schemaVersion\": 3", SimpleDrawDocument.Serialize(board.Manager));
    }
    [Theory] [InlineData("duplicate-id")] [InlineData("orphan-group")] [InlineData("text-bounds")]
    public void InvalidIdentityMembershipOrTextBoundsPreserveActiveDocument(string invalid)
    {
        var board = new DrawingSession(); Rectangle(board); var text = Text(board); var node = JsonNode.Parse(SimpleDrawDocument.Serialize(board.Manager))!;
        if (invalid == "duplicate-id") node["diagram"]!["components"]![1]!["id"] = node["diagram"]!["components"]![0]!["id"]!.GetValue<string>();
        if (invalid == "orphan-group") node["diagram"]!["components"]![1]!["propertyBag"]!["GroupId"] = Guid.NewGuid().ToString();
        if (invalid == "text-bounds") node["diagram"]!["components"]![1]!["width"] = text.Width + 10;
        Assert.Throws<InvalidDataException>(() => board.OpenDocument(node.ToJsonString())); Assert.Same(text, board.Manager.GetComponents()[1]);
    }
    [Fact] public void ExportRejectsInvalidLiveCoordinatesBeforeGeneralSerializerSanitization()
    { var board = new DrawingSession(); var shape = Rectangle(board); shape.X = float.NaN; Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Serialize(board.Manager)); }
    [Theory] [InlineData(3)] [InlineData(4)] public void ActualBrowserTextGroupsAndSeedsRoundtripThroughDesktopNativeRenderer(int version)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", $"v{version}.json"));
        var board = new DrawingSession(); board.OpenDocument(json);
        Assert.Equal(7, board.Manager.GetComponents().Count); var text = board.Manager.GetComponents().Cast<SketchShape>().Single(s => s.Kind == SketchShapeKind.Text);
        Assert.Equal("Sketch a thought.\nMake it yours.", text.DisplayText);
        Assert.Equal(2, board.Manager.GetComponents().Cast<SketchShape>().Count(s => s.GroupId == text.GroupId));
        var original = JsonNode.Parse(json)!;
        Assert.Equal(original["diagram"]!["components"]!.AsArray().Select(c => c!["id"]!.GetValue<Guid>()), board.Manager.GetComponents().Select(s => s.Id));
        Assert.InRange(Math.Abs(text.MeasureText().Width - text.Width), 0, .001f);
        var current = SimpleDrawDocument.Serialize(board.Manager); var again = new DrawingSession(); again.OpenDocument(current);
        Assert.Equal(current, SimpleDrawDocument.Serialize(again.Manager));
        if (version == 4) Assert.Equal(json.Replace("\"version\": 4", $"\"version\": {SimpleDrawDocument.CurrentVersion}").Trim(), current.Trim());
    }
}
