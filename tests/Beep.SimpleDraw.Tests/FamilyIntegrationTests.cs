using System.Text;
using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;
using Beep.SimpleDraw.Core.Documents;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Flowchart;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class FamilyIntegrationTests
{
    [Fact]
    public void ActualBrowserFamilyLibraryAndBoardPreserveContentAndRemapAllIdentities()
    {
        var library = SketchLibrary.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","family-library.json")));
        var entry = Assert.Single(library.Entries); var source = entry.ToDiagram(); Assert.Equal(3,source.Components.Count);
        var board = new DrawingSession(); var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","family-board.json"));
        board.OpenDocument(json); Assert.Equal(json.Replace("\"version\": 5", $"\"version\": {SimpleDrawDocument.CurrentVersion}").Trim(),SimpleDrawDocument.Serialize(board.Manager).Trim());
        Assert.Equal(6,board.Manager.GetComponents().Count);
        var nodes = board.Manager.GetComponents().Cast<FlowchartControl>().ToArray();
        Assert.Equal(new[]{"Prepare idea","Ready?","Finish"},nodes.Take(3).Select(FlowchartDefinitions.Label));
        Assert.Equal(nodes.Take(3).Select(FlowchartDefinitions.Label),nodes.Skip(3).Select(FlowchartDefinitions.Label));
        Assert.Equal(180,nodes[0].Width); Assert.Equal(90,nodes[0].Height); Assert.Equal(24,nodes[0].LabelFontSize);
        Assert.Equal(SKColor.Parse("#dceee6"),nodes[0].CustomFillColor); Assert.Equal(SKColor.Parse("#2875bc"),nodes[0].CustomStrokeColor);
        Assert.Equal(2,nodes.Select(n=>n.GroupId).Distinct().Count());
        var identities = nodes.SelectMany(n=>n.InConnectionPoints.Concat(n.OutConnectionPoints).Select(p=>p.Id).Append(n.Id)).ToArray();
        Assert.Equal(24,identities.Length); Assert.Equal(24,identities.Distinct().Count());
        for (int i=0;i<3;i++)
        {
            Assert.Equal(source.Components[i].Id,nodes[i].Id);
            Assert.Equal(source.Components[i].InPointIds,nodes[i].InConnectionPoints.Select(p=>p.Id));
            Assert.Equal(nodes[i].Width,nodes[i+3].Width); Assert.Equal(nodes[i].Height,nodes[i+3].Height);
            Assert.Equal(nodes[i].X+5,nodes[i+3].X); Assert.Equal(nodes[i].Y+155.5f,nodes[i+3].Y);
        }
        Assert.DoesNotContain("<text",Encoding.UTF8.GetString(DrawingExport.Svg(board.Manager)));
        var actualSvg = File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","family.svg"));
        foreach (var forbidden in new[]{"<text","font-family","<script","<image","foreignObject","href="}) Assert.DoesNotContain(forbidden,actualSvg,StringComparison.OrdinalIgnoreCase);
        using var actualPng = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","family.png")));
        var layout = DrawingExport.Layout(board.Manager); Assert.Equal(layout.Width,actualPng.Width); Assert.Equal(layout.Height,actualPng.Height);
    }
    [Theory]
    [InlineData(FlowchartDefinitions.ProcessType)] [InlineData(FlowchartDefinitions.DecisionType)] [InlineData(FlowchartDefinitions.TerminatorType)]
    public void TemplateInsertionUsesRealFamilyAndNativeRoundtrip(string type)
    {
        var board = new DrawingSession(); board.InsertTemplate(type);
        var original = Assert.IsAssignableFrom<FlowchartControl>(Assert.Single(board.Manager.GetComponents()));
        Assert.Equal(type,FlowchartDefinitions.TypeKey(original)); Assert.True(original.IsSelected); Assert.Equal(1,board.Manager.HistoryManager.UndoCount);
        var json = SimpleDrawDocument.Serialize(board.Manager); Assert.Contains($"\"version\": {SimpleDrawDocument.CurrentVersion}",json);
        var restored = new DrawingSession(); restored.OpenDocument(json); Assert.Equal(json,SimpleDrawDocument.Serialize(restored.Manager));
        board.Undo(); Assert.Empty(board.Manager.GetComponents()); board.Redo(); Assert.Same(original,Assert.Single(board.Manager.GetComponents()));
        Assert.Equal(json,SimpleDrawDocument.Serialize(board.Manager));
    }
    private static DrawingSession MixedBoard()
    {
        var board = new DrawingSession(); board.InsertTemplate(FlowchartDefinitions.ProcessType);
        board.Tool = DrawingTool.Rectangle; board.PointerDown(new(50,80)); board.PointerUp(new(150,140));
        board.Tool = DrawingTool.Select; board.Manager.SelectAllComponents(); return board;
    }
    [Theory]
    [InlineData("ports")] [InlineData("component-port")] [InlineData("group-port")]
    public void CrossComponentIdentityCollisionsNeverMutateTheActiveBoard(string collision)
    {
        var source = new DrawingSession(); source.InsertTemplate(FlowchartDefinitions.ProcessType); source.InsertTemplate(FlowchartDefinitions.DecisionType);
        var diagram = SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(source.Manager)); var first = diagram.Components[0]; var second = diagram.Components[1];
        if (collision == "ports") second.InPointIds[0] = first.InPointIds[0];
        if (collision == "component-port") first.Id = second.InPointIds[0];
        if (collision == "group-port")
        { first.PropertyBag["GroupId"] = second.InPointIds[0].ToString("D"); second.PropertyBag["GroupId"] = first.PropertyBag["GroupId"]; }
        var target = MixedBoard(); target.Undo(); var before = SimpleDrawDocument.Serialize(target.Manager);
        Assert.Throws<InvalidDataException>(()=>target.InsertDiagram(diagram,new(100,200)));
        Assert.Equal(before,SimpleDrawDocument.Serialize(target.Manager)); Assert.True(target.Manager.CanRedo);
    }
    [Fact]
    public void MixedStylesGroupsAndResizeAreOneHistoryCommandAndRestoreSameInstances()
    {
        var board = MixedBoard(); var originals = board.Manager.GetComponents().ToArray(); var count = board.Manager.HistoryManager.UndoCount;
        var initial = SimpleDrawDocument.Serialize(board.Manager);
        board.SetStyle(stroke:"#2875bc",fill:"#dceee6",width:4,fontSize:36); Assert.Equal(count+1,board.Manager.HistoryManager.UndoCount);
        Assert.Equal(4,((FlowchartControl)originals[0]).NodeStrokeWidth); Assert.Equal(36,((FlowchartControl)originals[0]).LabelFontSize);
        board.Undo(); Assert.Equal(initial,SimpleDrawDocument.Serialize(board.Manager)); board.Redo();
        board.Manager.SelectAllComponents(); board.GroupSelection(); var group = originals[0].GroupId;
        Assert.NotEqual(Guid.Empty,group); Assert.All(originals,c=>Assert.Equal(group,c.GroupId));
        board.Undo(); Assert.All(originals,c=>Assert.Equal(Guid.Empty,c.GroupId)); board.Redo();
        board.Manager.SelectAllComponents(); var before = SimpleDrawDocument.Serialize(board.Manager); var bounds = board.SelectionBounds!.Value;
        board.ResizeSelection(bounds.Width*2,bounds.Height*2); Assert.Equal(bounds.Width*2,board.SelectionBounds!.Value.Width);
        board.Undo(); Assert.Equal(before,SimpleDrawDocument.Serialize(board.Manager)); Assert.Equal(originals,board.Manager.GetComponents());
        Assert.Throws<InvalidOperationException>(()=>board.SetStyle(roughness:1.5f)); Assert.Equal(before,SimpleDrawDocument.Serialize(board.Manager));
    }
    [Fact]
    public void PointerMoveCancelKeyboardMoveAndEraserUseFamilySelectionAndHistory()
    {
        var board = new DrawingSession(); board.InsertTemplate(FlowchartDefinitions.ProcessType);
        var node = (FlowchartControl)board.Manager.GetComponents()[0]; var initial = new SKPoint(node.X,node.Y);
        var point = new SKPoint(node.X+30,node.Y+30);
        board.PointerDown(point); board.PointerMove(point+new SKPoint(40,30)); board.CancelGesture(); Assert.Equal(initial,new SKPoint(node.X,node.Y));
        board.PointerDown(point); board.PointerUp(point+new SKPoint(40,30)); Assert.Equal(initial+new SKPoint(40,30),new SKPoint(node.X,node.Y));
        board.Undo(); Assert.Equal(initial,new SKPoint(node.X,node.Y)); board.SelectShape(node); board.MoveSelected(new(8,0));
        board.Undo(); Assert.Equal(initial,new SKPoint(node.X,node.Y));
        board.Tool = DrawingTool.Eraser; board.PointerDown(new(node.X-20,node.Y+node.Height/2)); board.PointerUp(new(node.X+node.Width+20,node.Y+node.Height/2));
        Assert.Empty(board.Manager.GetComponents()); board.Undo(); Assert.Same(node,Assert.Single(board.Manager.GetComponents()));
    }
    [Fact]
    public void LibraryAndDuplicateRemapEveryComponentPortAndMixedGroupIdentity()
    {
        var board = MixedBoard(); board.GroupSelection(); var originals = board.Manager.GetComponents().ToArray();
        var entry = new SketchLibrary().SaveSelection(board.Manager,"Mixed workflow"); var captured = entry.ToDiagram();
        var originalIds = originals.SelectMany(c=>c.InConnectionPoints.Concat(c.OutConnectionPoints).Select(p=>p.Id).Append(c.Id)).ToHashSet();
        var target = new DrawingSession(); target.InsertDiagram(captured,new(100,200)); var inserted = target.Manager.GetComponents().ToArray();
        Assert.Empty(inserted.SelectMany(c=>c.InConnectionPoints.Concat(c.OutConnectionPoints).Select(p=>p.Id).Append(c.Id)).Intersect(originalIds));
        Assert.NotEqual(originals[0].GroupId,inserted[0].GroupId); Assert.Equal(inserted[0].GroupId,inserted[1].GroupId);
        var originalNode = (FlowchartControl)originals[0]; var insertedNode = (FlowchartControl)inserted[0];
        Assert.Equal(FlowchartDefinitions.Label(originalNode),FlowchartDefinitions.Label(insertedNode));
        target.DuplicateSelection(); var copies = target.SelectedComponents.ToArray(); Assert.Equal(4,target.Manager.GetComponents().Count);
        Assert.Equal(inserted[0].X+24,copies[0].X); Assert.EndsWith(" copy",copies[0].Name);
        Assert.Empty(copies.SelectMany(c=>c.InConnectionPoints.Concat(c.OutConnectionPoints).Select(p=>p.Id).Append(c.Id))
            .Intersect(inserted.SelectMany(c=>c.InConnectionPoints.Concat(c.OutConnectionPoints).Select(p=>p.Id).Append(c.Id))));
        target.Undo(); Assert.Equal(inserted,target.Manager.GetComponents()); target.Redo(); Assert.Equal(4,target.Manager.GetComponents().Count);
    }
    [Fact]
    public void LabelValidationAndHistoryPreserveBoardAndPorts()
    {
        var board = new DrawingSession(); board.InsertTemplate(FlowchartDefinitions.DecisionType); var node = (FlowchartControl)board.Manager.GetComponents()[0];
        var ports = node.InConnectionPoints.Concat(node.OutConnectionPoints).Select(p=>p.Id).ToArray(); var before = SimpleDrawDocument.Serialize(board.Manager);
        board.SetFamilyLabel("Ready to ship?"); Assert.Equal("Ready to ship?",FlowchartDefinitions.Label(node));
        board.Undo(); Assert.Equal(before,SimpleDrawDocument.Serialize(board.Manager)); board.Redo();
        Assert.Equal(ports,node.InConnectionPoints.Concat(node.OutConnectionPoints).Select(p=>p.Id));
        var after = SimpleDrawDocument.Serialize(board.Manager); Assert.Throws<InvalidDataException>(()=>board.SetFamilyLabel("\u4e2d")); Assert.Equal(after,SimpleDrawDocument.Serialize(board.Manager));
        Assert.Throws<ArgumentException>(()=>board.ResizeSelection(19,40)); Assert.Equal(after,SimpleDrawDocument.Serialize(board.Manager));
    }
    [Fact]
    public void InvalidFamilyOrOldVersionMasqueradePreservesDraftSceneAndRedo()
    {
        var board = MixedBoard(); board.Undo(); board.Tool = DrawingTool.Rectangle; board.PointerDown(new(10,10)); board.PointerMove(new(40,40));
        var draft = board.Draft; var json = SimpleDrawDocument.Serialize(board.Manager); var root = JsonNode.Parse(json)!;
        root["diagram"]!["components"]![0]!["propertyBag"]!["Execute"] = "true";
        Assert.Throws<InvalidDataException>(()=>board.OpenDocument(root.ToJsonString())); Assert.Same(draft,board.Draft); Assert.True(board.Manager.CanRedo);
        root = JsonNode.Parse(json)!; root["version"] = 4;
        Assert.Throws<InvalidDataException>(()=>board.OpenDocument(root.ToJsonString())); Assert.Same(draft,board.Draft); Assert.Equal(json,SimpleDrawDocument.Serialize(board.Manager));
    }
    [Theory]
    [InlineData(FlowchartDefinitions.ProcessType)] [InlineData(FlowchartDefinitions.DecisionType)] [InlineData(FlowchartDefinitions.TerminatorType)]
    public void FamilyExportsUseSamePortableRendererAndIncludePorts(string type)
    {
        var board = new DrawingSession(); board.InsertTemplate(type); board.SetStyle(fill:"#dceee6",stroke:"#2875bc",width:4);
        board.SetFamilyLabel("Ready?"); var before = SimpleDrawDocument.Serialize(board.Manager);
        var svg = Encoding.UTF8.GetString(DrawingExport.Svg(board.Manager)); Assert.Contains("<path",svg); Assert.DoesNotContain("<text",svg); Assert.DoesNotContain("font-family",svg);
        var bytes = DrawingExport.Png(board.Manager); using var bitmap = SKBitmap.Decode(bytes); Assert.NotNull(bitmap);
        var layout = DrawingExport.Layout(board.Manager); var node = board.Manager.GetComponents()[0];
        Assert.True(layout.Bounds.Left < node.X-9 && layout.Bounds.Right > node.X+node.Width+9);
        Assert.Equal(before,SimpleDrawDocument.Serialize(board.Manager));
    }
}
