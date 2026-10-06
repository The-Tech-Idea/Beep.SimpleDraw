using System.Text;
using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Documents;
using Beep.SimpleDraw.Core.Library;
using Beep.Skia;
using Beep.Skia.Flowchart;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class ConnectorIntegrationTests
{
    [Fact]
    public void ActualBrowserConnectedFilesPreserveBranchesCopiesAndPortableExportBounds()
    {
        var native = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "connected-board.json"));
        var board = new DrawingSession(); board.OpenDocument(native);
        Assert.Equal(native.Replace("\"version\": 6",$"\"version\": {SimpleDrawDocument.CurrentVersion}").Trim(), SimpleDrawDocument.Serialize(board.Manager).Trim());
        var nodes = board.Manager.GetComponents().Cast<FlowchartControl>().ToArray(); var lines = board.Manager.GetLines().ToArray();
        Assert.Equal(6, nodes.Length); Assert.Equal(6, lines.Length); Assert.Equal(2, nodes.Select(n => n.GroupId).Distinct().Count());
        Assert.Equal(new[] { "", "Yes", "No", "", "Yes", "No" }, lines.Select(l => l.Label3 ?? ""));
        Assert.Equal(LineRoutingMode.Curved, lines[2].RoutingMode); Assert.Equal(LineRoutingMode.Curved, lines[5].RoutingMode);
        Assert.Same(nodes[1].OutConnectionPoints[1], lines[2].Start); Assert.Same(nodes[0].InConnectionPoints[0], lines[2].End);
        Assert.Same(nodes[4].OutConnectionPoints[1], lines[5].Start); Assert.Same(nodes[3].InConnectionPoints[0], lines[5].End);
        var originalPorts = nodes.Take(3).SelectMany(n => n.InConnectionPoints.Concat(n.OutConnectionPoints)).Select(p => p.Id).ToHashSet();
        Assert.All(nodes.Skip(3).SelectMany(n => n.InConnectionPoints.Concat(n.OutConnectionPoints)), p => Assert.DoesNotContain(p.Id, originalPorts));
        for (int i = 0; i < 3; i++) { Assert.Equal(nodes[i].X + 10, nodes[i + 3].X); Assert.Equal(nodes[i].Y + 175, nodes[i + 3].Y); }
        var library = SketchLibrary.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "connected-library.json")));
        Assert.Equal(3, Assert.Single(library.Entries).ToDiagram().Lines.Count);
        using var png = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "connected.png")));
        var layout = DrawingExport.Layout(board.Manager); Assert.Equal(layout.Width, png.Width); Assert.Equal(layout.Height, png.Height);
        var svg = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "connected.svg"));
        Assert.Contains("<path", svg); Assert.DoesNotContain("<text", svg); Assert.DoesNotContain("font-family", svg);
    }
    [Fact]
    public void UnsupportedLiveConnectorAppearanceIsRejectedInsteadOfLostOnSave()
    {
        var board = new DrawingSession(); board.InsertConnectedTemplate();
        var line = (ConnectionLine)board.Manager.GetLines()[0]; var before = SimpleDrawDocument.Serialize(board.Manager);
        line.Paint.StrokeWidth = 5; Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Serialize(board.Manager));
        line.Paint.StrokeWidth = 2; line.SchemaJson = "[]"; Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Serialize(board.Manager));
        line.SchemaJson = null; Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager));
        using var bitmap = new SKBitmap(100, 100); using var canvas = new SKCanvas(bitmap); var saveCount = canvas.SaveCount;
        line.RequiredLabelFontFamily = "Unknown connector font"; Assert.Throws<InvalidOperationException>(() => line.Draw(canvas)); Assert.Equal(saveCount, canvas.SaveCount);
        line.RequiredLabelFontFamily = "Kalam"; line.Label3 = "\u4e2d"; Assert.Throws<ArgumentException>(() => line.Draw(canvas)); Assert.Equal(saveCount, canvas.SaveCount);
    }
    [Fact]
    public void ConnectedTemplateNativeLibraryAndDuplicationKeepExactEndpointsAsOneUndo()
    {
        var board = new DrawingSession(); board.InsertConnectedTemplate();
        Assert.Equal(3, board.Manager.GetComponents().Count); Assert.Equal(2, board.Manager.GetLines().Count);
        Assert.Equal(1, board.Manager.HistoryManager.UndoCount);
        var original = SimpleDrawDocument.Serialize(board.Manager);
        var restored = new DrawingSession(); restored.OpenDocument(original); Assert.Equal(original, SimpleDrawDocument.Serialize(restored.Manager));
        var library = new SketchLibrary(); var entry = library.SaveSelection(board.Manager, "Connected workflow");
        Assert.Equal(2, entry.ToDiagram().Lines.Count);
        var initial = board.Manager.GetLines().ToArray(); var oldPorts = initial.SelectMany(l => new[] { l.Start.Id, l.End.Id }).ToHashSet();
        board.InsertDiagram(entry.ToDiagram(), new(600, 400));
        Assert.Equal(6, board.Manager.GetComponents().Count); Assert.Equal(4, board.Manager.GetLines().Count);
        var copies = board.Manager.GetLines().Skip(2).ToArray();
        Assert.All(copies, l => { Assert.DoesNotContain(l.Start.Id, oldPorts); Assert.DoesNotContain(l.End.Id, oldPorts); Assert.True(l.IsConnected); });
        board.Undo(); Assert.Equal(original, SimpleDrawDocument.Serialize(board.Manager));
        board.Redo(); Assert.Equal(copies, board.Manager.GetLines().Skip(2));
        board.Manager.SelectAllComponents(); board.DuplicateSelection(); Assert.Equal(12, board.Manager.GetComponents().Count); Assert.Equal(8, board.Manager.GetLines().Count);
        var after = SimpleDrawDocument.Serialize(board.Manager); restored.OpenDocument(after); Assert.Equal(after, SimpleDrawDocument.Serialize(restored.Manager));
        board.Undo(); Assert.Equal(6, board.Manager.GetComponents().Count); Assert.Equal(4, board.Manager.GetLines().Count);
        board.Redo(); Assert.Equal(after, SimpleDrawDocument.Serialize(board.Manager));
    }
    [Fact]
    public void PartialSelectionRetainsOnlyInternalLinesAndDeletionUndoReattachesThem()
    {
        var board = new DrawingSession(); board.InsertConnectedTemplate(); board.GroupSelection(true);
        var nodes = board.Manager.GetComponents().ToArray(); board.Manager.SelectionManager.ClearSelection();
        board.Manager.SelectionManager.SelectComponent(nodes[0]); board.Manager.SelectionManager.SelectComponent(nodes[1], true);
        var entry = new SketchLibrary().SaveSelection(board.Manager, "Two nodes"); Assert.Single(entry.ToDiagram().Lines);
        board.DuplicateSelection(); Assert.Equal(5, board.Manager.GetComponents().Count); Assert.Equal(3, board.Manager.GetLines().Count);
        var before = SimpleDrawDocument.Serialize(board.Manager); board.DeleteSelected(); Assert.Equal(3, board.Manager.GetComponents().Count); Assert.Equal(2, board.Manager.GetLines().Count);
        board.Undo(); Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager));
        board.Redo(); Assert.Equal(2, board.Manager.GetLines().Count);
        var originalOnly = SimpleDrawDocument.Serialize(board.Manager);
        board.Manager.RemoveComponent(nodes[0]); Assert.Single(board.Manager.GetLines());
        board.Undo(); Assert.Equal(originalOnly, SimpleDrawDocument.Serialize(board.Manager));
    }
    [Theory]
    [InlineData("dangling")] [InlineData("direction")] [InlineData("self")] [InlineData("occupied")]
    [InlineData("guard")] [InlineData("schema")] [InlineData("routing")] [InlineData("glyph")]
    [InlineData("version5")] [InlineData("animation")] [InlineData("arrow")] [InlineData("index")]
    public void InvalidConnectorFilePreservesBoardDraftAndRedo(string failure)
    {
        var source = new DrawingSession(); source.InsertConnectedTemplate();
        var root = JsonNode.Parse(SimpleDrawDocument.Serialize(source.Manager))!; var line = root["diagram"]!["lines"]![0]!;
        var components = root["diagram"]!["components"]!;
        switch (failure)
        {
            case "dangling": line["endPointId"] = Guid.NewGuid(); break;
            case "direction": line["startPointId"] = components[0]!["inPointIds"]![0]!.DeepClone(); break;
            case "self": line["endPointId"] = components[0]!["inPointIds"]![0]!.DeepClone(); break;
            case "occupied": root["diagram"]!["lines"]!.AsArray().Add(line.DeepClone()); break;
            case "guard": line["guardCondition"] = "run()"; break;
            case "schema": line["schemaJson"] = "[]"; break;
            case "routing": line["routingMode"] = 999; break;
            case "glyph": line["label3"] = "\u4e2d"; break;
            case "version5": root["version"] = 5; break;
            case "animation": line["isAnimated"] = true; break;
            case "arrow": line["arrowSize"] = 1000; break;
            case "index": line["startComponentIndex"] = 0; break;
        }
        var board = new DrawingSession(); board.InsertTemplate(FlowchartDefinitions.ProcessType); board.InsertTemplate(FlowchartDefinitions.DecisionType); board.Undo();
        var before = SimpleDrawDocument.Serialize(board.Manager); board.Tool = DrawingTool.Line; board.PointerDown(new(20, 20)); board.PointerMove(new(90, 90));
        var draft = board.Draft; Assert.Throws<InvalidDataException>(() => board.OpenDocument(root.ToJsonString()));
        Assert.Equal(before, SimpleDrawDocument.Serialize(board.Manager)); Assert.Same(draft, board.Draft); Assert.True(board.HasGesture);
        Assert.Equal(1, board.Manager.HistoryManager.RedoCount);
    }
    [Theory]
    [InlineData(LineRoutingMode.Straight)] [InlineData(LineRoutingMode.Orthogonal)] [InlineData(LineRoutingMode.Curved)]
    public void LabelOutlinesAndBoundsSurviveNativeRoundtripAndBothExports(LineRoutingMode routing)
    {
        var board = new DrawingSession(); board.InsertConnectedTemplate();
        var line = (ConnectionLine)board.Manager.GetLines()[0]; line.Label1 = new string('W', 70); line.Label2 = "Target"; line.Label3 = "Middle";
        line.RoutingMode = routing; line.ShowStartArrow = true; line.ArrowSize = 24;
        var bounds = line.GetPathAndLabelBounds(); Assert.True(bounds.Left < line.Start.Position.X - 200);
        var layout = DrawingExport.Layout(board.Manager); Assert.True(layout.Bounds.Contains(bounds));
        var native = SimpleDrawDocument.Serialize(board.Manager); var restored = new DrawingSession(); restored.OpenDocument(native);
        Assert.Equal(native, SimpleDrawDocument.Serialize(restored.Manager));
        using var bitmap = SKBitmap.Decode(DrawingExport.Png(restored.Manager)); Assert.Equal(layout.Width, bitmap.Width);
        var svg = Encoding.UTF8.GetString(DrawingExport.Svg(restored.Manager)); Assert.Contains("<path", svg);
        Assert.DoesNotContain("<text", svg); Assert.DoesNotContain("font-family", svg);
        foreach (var forbidden in new[] { "<script", "<image", "foreignObject", "href=" }) Assert.DoesNotContain(forbidden, svg, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void SessionConnectionAndRemovalUseOneHistoryAndRejectBusyEditsWithoutDamage()
    {
        var board = new DrawingSession(); board.InsertConnectedTemplate(); var nodes = board.Manager.GetComponents().ToArray();
        var original = SimpleDrawDocument.Serialize(board.Manager); var history = board.Manager.HistoryManager.UndoCount;
        Assert.Throws<InvalidDataException>(() => board.Connect(nodes[0].Id, 0, nodes[2].Id, 0, "Bad", LineRoutingMode.Straight));
        Assert.Equal(history, board.Manager.HistoryManager.UndoCount); Assert.Equal(original, SimpleDrawDocument.Serialize(board.Manager));
        var removed = board.Manager.GetLines()[0]; board.RemoveConnection(removed); Assert.Single(board.Manager.GetLines());
        board.Undo(); Assert.Equal(original, SimpleDrawDocument.Serialize(board.Manager)); board.Redo(); Assert.Single(board.Manager.GetLines());
        board.Connect(nodes[0].Id, 0, nodes[1].Id, 0, "New", LineRoutingMode.Curved); Assert.Equal(2, board.Manager.GetLines().Count);
        board.Undo(); Assert.Single(board.Manager.GetLines()); board.Redo(); Assert.Equal(2, board.Manager.GetLines().Count);
        board.Tool = DrawingTool.Line; board.PointerDown(new(20, 20));
        Assert.Throws<InvalidOperationException>(() => board.RemoveConnection(removed)); Assert.True(board.HasGesture);
    }
}
