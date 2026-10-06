using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class LibraryTests
{
    [Fact] public void ActualBrowserLibraryAndInsertedDrawingPreserveContentAndRemapEveryIdentity()
    {
        var library=SketchLibrary.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","library.json")));
        var entry=Assert.Single(library.Entries); Assert.Equal("All tools",entry.Name); Assert.Equal(7,entry.ToDiagram().Components.Count);
        var board=new DrawingSession(); board.OpenDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","insertion.json")));
        var shapes=board.Manager.GetComponents().Cast<SketchShape>().ToArray(); Assert.Equal(14,shapes.Length);
        Assert.Empty(shapes.Take(7).Select(s=>s.Id).Intersect(shapes.Skip(7).Select(s=>s.Id)));
        Assert.Empty(shapes.Take(7).Where(s=>s.GroupId!=Guid.Empty).Select(s=>s.GroupId).Intersect(shapes.Skip(7).Select(s=>s.GroupId)));
        Assert.Equal(shapes.Take(7).Select(s=>(s.Kind,s.Width,s.Height,s.DisplayText,s.SketchSeed)),shapes.Skip(7).Select(s=>(s.Kind,s.Width,s.Height,s.DisplayText,s.SketchSeed)));
    }
    private static DrawingSession Board()
    {
        var board = new DrawingSession {Tool=DrawingTool.Rectangle};
        board.PointerDown(new(-100,100)); board.PointerUp(new(0,180));
        board.Tool=DrawingTool.Arrow; board.PointerDown(new(0,140)); board.PointerUp(new(100,200));
        board.Tool=DrawingTool.Select; board.Manager.SelectAllComponents(); board.GroupSelection(); return board;
    }
    [Fact] public void CaptureNormalizesSubsetAndKeepsBoardHistoryAndSelectionIntact()
    {
        var board=Board(); var json=SimpleDrawDocument.Serialize(board.Manager); var count=board.Manager.HistoryManager.UndoCount;
        var library=new SketchLibrary(); var entry=library.SaveSelection(board.Manager,"  Arrow group  ");
        Assert.Equal("Arrow group",entry.Name); var diagram=entry.ToDiagram();
        Assert.Equal(0,diagram.Components.Min(c=>c.X)); Assert.Equal(0,diagram.Components.Min(c=>c.Y));
        Assert.Equal(json,SimpleDrawDocument.Serialize(board.Manager)); Assert.Equal(count,board.Manager.HistoryManager.UndoCount);
        Assert.Equal(2,board.SelectedShapes.Count());
        Assert.Equal(library.Serialize(),SketchLibrary.Parse(library.Serialize()).Serialize());
    }
    [Fact] public void RepeatedInsertionRemapsComponentAndGroupIdentityAndIsOneUndoPerCopy()
    {
        var source=Board(); var entry=new SketchLibrary().SaveSelection(source.Manager,"Workflow");
        var target=new DrawingSession(); target.InsertDiagram(entry.ToDiagram(),new(100,200));
        var first=target.SelectedShapes.ToArray(); Assert.Equal(1,target.Manager.HistoryManager.UndoCount);
        target.InsertDiagram(entry.ToDiagram(),new(400,200)); var second=target.SelectedShapes.ToArray();
        Assert.Equal(4,target.Manager.GetComponents().Count); Assert.Equal(2,target.Manager.HistoryManager.UndoCount);
        Assert.Empty(first.Select(s=>s.Id).Intersect(second.Select(s=>s.Id)));
        Assert.NotEqual(first[0].GroupId,second[0].GroupId); Assert.NotEqual(source.SelectedShapes.First().GroupId,first[0].GroupId);
        Assert.Equal(first[0].GroupId,first[1].GroupId);
        var arrow=(SketchShape)first.Single(s=>s.Kind==SketchShapeKind.Arrow); Assert.Equal(new SKPoint(200,240),arrow.WorldPoint(0));
        Assert.Equal(new SKPoint(300,300),arrow.WorldPoint(1));
        target.Undo(); Assert.Equal(first.Select(s=>s.Id),target.Manager.GetComponents().Select(s=>s.Id));
        target.Redo(); Assert.Equal(4,target.Manager.GetComponents().Count);
        var restored=new DrawingSession(); restored.OpenDocument(SimpleDrawDocument.Serialize(target.Manager));
        Assert.Equal(SimpleDrawDocument.Serialize(target.Manager),SimpleDrawDocument.Serialize(restored.Manager));
    }
    [Fact] public void InvalidOrOversizedInsertionPreservesSceneSelectionGestureAndRedo()
    {
        var board=Board(); var entry=new SketchLibrary().SaveSelection(board.Manager,"Pair"); board.Undo();
        var before=SimpleDrawDocument.Serialize(board.Manager); var ids=board.SelectedShapes.Select(s=>s.Id).ToArray();
        Assert.Throws<InvalidDataException>(()=>board.InsertDiagram(entry.ToDiagram(),new(100_000,0)));
        var bad=entry.ToDiagram(); bad.Components[0].Type="System.IO.File";
        Assert.Throws<InvalidDataException>(()=>board.InsertDiagram(bad,new(0,0)));
        Assert.Equal(before,SimpleDrawDocument.Serialize(board.Manager)); Assert.True(board.Manager.CanRedo);
        Assert.Equal(ids,board.SelectedShapes.Select(s=>s.Id));
        board.Tool=DrawingTool.Rectangle; board.KeyboardEnter(); board.MoveKeyboardCursor(new(50,30));
        Assert.Throws<InvalidOperationException>(()=>board.InsertDiagram(entry.ToDiagram(),new(0,0)));
        Assert.NotNull(board.Draft); Assert.True(board.HasKeyboardGesture); board.CancelGesture();
        board.BeginText(new(0,0)); Assert.Throws<InvalidOperationException>(()=>board.InsertDiagram(entry.ToDiagram(),new(0,0)));
        Assert.True(board.HasTextEdit);
    }
    [Fact] public void LibraryImportMergesAtomicallyAndRemapsEntryIdsWithoutChangingContent()
    {
        var source=new SketchLibrary(); var entry=source.SaveSelection(Board().Manager,"Example");
        var target=SketchLibrary.Parse(source.Serialize()); target.Import(source.Serialize());
        Assert.Equal(2,target.Entries.Count); Assert.NotEqual(entry.Id,target.Entries[1].Id);
        Assert.Equal(entry.Document,target.Entries[1].Document);
        var before=target.Serialize(); var bad=JsonNode.Parse(source.Serialize())!;
        bad["entries"]![0]!["document"]="{}";
        Assert.Throws<InvalidDataException>(()=>target.Import(bad.ToJsonString())); Assert.Equal(before,target.Serialize());
        var removed=target.Remove(entry.Id); Assert.Single(target.Entries); target.Add(removed); Assert.Equal(2,target.Entries.Count);
    }
    [Theory]
    [InlineData("unknown")]
    [InlineData("future")]
    [InlineData("duplicate")]
    [InlineData("name")]
    [InlineData("coordinate")]
    public void UntrustedLibraryMetadataAndPayloadAreRejected(string fault)
    {
        var library=new SketchLibrary(); library.SaveSelection(Board().Manager,"Pair"); var root=JsonNode.Parse(library.Serialize())!;
        if(fault=="unknown") root["runCode"]="malicious";
        if(fault=="future") root["version"]=2;
        if(fault=="duplicate") root["entries"]!.AsArray().Add(root["entries"]![0]!.DeepClone());
        if(fault=="name") root["entries"]![0]!["name"]="bad\u0000name";
        if(fault=="coordinate")
        { var doc=JsonNode.Parse(root["entries"]![0]!["document"]!.GetValue<string>())!; doc["diagram"]!["components"]![0]!["x"]=1; root["entries"]![0]!["document"]=doc.ToJsonString(); }
        Assert.Throws<InvalidDataException>(()=>SketchLibrary.Parse(root.ToJsonString()));
    }
    [Fact] public void PartialGroupsEmptySelectionsAndCapacityErrorsDoNotChangeLibrary()
    {
        var board=Board(); var library=new SketchLibrary(); var entry=library.SaveSelection(board.Manager,"Pair"); var before=library.Serialize();
        board.Manager.SelectionManager.ClearSelection(); board.Manager.SelectionManager.SelectComponent(board.Manager.GetComponents()[0]);
        Assert.Throws<InvalidDataException>(()=>library.SaveSelection(board.Manager,"Partial"));
        board.Manager.SelectionManager.ClearSelection(); Assert.Throws<InvalidDataException>(()=>library.SaveSelection(board.Manager,"Empty"));
        Assert.Equal(before,library.Serialize());
        while(library.Entries.Count<SketchLibrary.MaxEntries) library.Add(entry with {Id=Guid.NewGuid()});
        before=library.Serialize(); Assert.Throws<InvalidDataException>(()=>library.Import(new SketchLibrary().Serialize().Replace("[]", "[{}]")));
        Assert.Throws<InvalidDataException>(()=>library.Add(entry with {Id=Guid.NewGuid()})); Assert.Equal(before,library.Serialize());
    }
    [Fact] public void EveryCommittedToolRoundtripsThroughLibraryWithGeometryTextAndSeeds()
    {
        var board=new DrawingSession(); board.OpenDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","v4.json")));
        board.Manager.SelectAllComponents(); var entry=new SketchLibrary().SaveSelection(board.Manager,"All tools");
        var target=new DrawingSession(); target.InsertDiagram(entry.ToDiagram(),new(100,200));
        Assert.Equal(7,target.Manager.GetComponents().Count); Assert.Equal(1,target.Manager.HistoryManager.UndoCount);
        Assert.Equal(board.Manager.GetComponents().Cast<SketchShape>().Select(s=>(s.Kind,s.Width,s.Height,s.DisplayText,s.SketchSeed,s.Roughness)),
            target.Manager.GetComponents().Cast<SketchShape>().Select(s=>(s.Kind,s.Width,s.Height,s.DisplayText,s.SketchSeed,s.Roughness)));
    }
}
