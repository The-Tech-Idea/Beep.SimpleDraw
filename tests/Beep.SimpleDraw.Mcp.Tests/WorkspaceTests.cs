using System.Text.Json;
using Beep.SimpleDraw.Mcp;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;
using Beep.Skia.Components;
using ModelContextProtocol;
using Xunit;

namespace Beep.SimpleDraw.Mcp.Tests;

public sealed class WorkspaceTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"simpledraw-mcp-tests",Guid.NewGuid().ToString("N"));
    private readonly DrawingWorkspace _workspace;
    private readonly SimpleDrawTools _tools;
    public WorkspaceTests() {_workspace=new(_root);_tools=new(_workspace);}
    private static JsonElement Json(string text)=>JsonDocument.Parse(text).RootElement.Clone();
    private (Guid Id,long Revision) Create()=>Identity(Json(_tools.CreateDrawing("Test board")));
    private static (Guid Id,long Revision) Identity(JsonElement result)=>(result.GetProperty("id").GetGuid(),result.GetProperty("revision").GetInt64());
    private static Guid Selected(JsonElement result)=>result.GetProperty("selection")[0].GetGuid();
    public static IEnumerable<object[]> Components()=>SkiaCatalog.Entries.Select(e=>new object[]{e.TypeKey});
    [Theory][MemberData(nameof(Components))]
    public void DiscoveryIncludesEverySerializedPropertyWithItsDefault(string type)
    {
        using var node=SkiaCatalog.Create(type);
        if(node is Beep.Skia.Flowchart.FlowchartControl flow && FlowchartDefinitions.Supports(type)) { flow.ShowDisplayText=false;flow.RequiredLabelFontFamily="Kalam"; }
        var dto=node is Beep.Skia.Flowchart.FlowchartControl f && FlowchartDefinitions.Supports(type) ? FlowchartDefinitions.Capture(f) : SkiaCatalog.Capture(node);var description=Json(_tools.DescribeComponent(type));
        Assert.Equal(type,description.GetProperty("type").GetString());
        var actual=description.GetProperty("properties").EnumerateArray().ToDictionary(p=>p.GetProperty("name").GetString()!);
        Assert.Equal(dto.PropertyBag.Keys.Concat(dto.TypedPropertyBag.Keys).Where(k=>k!="GroupId").Order(),actual.Keys.Order());
        foreach(var (key,value) in dto.PropertyBag.Where(p=>p.Key!="GroupId")) {
            if(type=="Beep.Skia.ERD.ERDEntity" && key is "Columns" or "RowIdsCsv") Assert.NotEmpty(actual[key].GetProperty("defaultValue").GetString()!);
            else Assert.Equal(value,actual[key].GetProperty("defaultValue").GetString());
        }
        foreach(var (key,value) in dto.TypedPropertyBag) Assert.Equal(value.ValueKind,actual[key].GetProperty("defaultValue").ValueKind); // ERD factories generate fresh attribute IDs.
        var (boardId,revision)=Create();var inserted=Json(_tools.AddComponent(boardId,revision,type,80,60));
        var component=inserted.GetProperty("diagram").GetProperty("components")[0];
        Assert.Equal(type,component.GetProperty("type").GetString());Assert.Equal(80,component.GetProperty("x").GetSingle());Assert.Equal(60,component.GetProperty("y").GetSingle());
        Assert.Equal(dto.InPointIds.Count,component.GetProperty("inPointIds").GetArrayLength());Assert.Equal(dto.OutPointIds.Count,component.GetProperty("outPointIds").GetArrayLength());
        var native=Json(_tools.ExportDrawing(boardId)).GetProperty("nativeJson").GetString()!;
        Assert.Single(SimpleDrawDocument.Parse(native).Components);
    }
    [Fact] public void RevisionsProtectClaudeAndCodexAndExternalWritesResetOnlyStaleHistory()
    {
        var (id,r)=Create();using var other=new DrawingWorkspace(_root);var otherTools=new SimpleDrawTools(other);
        var changed=Json(_tools.AddSketch(id,r,"Rectangle",10,20));r=changed.GetProperty("revision").GetInt64();
        Assert.Throws<McpException>(()=>otherTools.AddSketch(id,1,"Ellipse"));
        Assert.Single(Json(otherTools.GetDrawing(id)).GetProperty("diagram").GetProperty("components").EnumerateArray());
        var next=Json(otherTools.AddSketch(id,r,"Ellipse",300,20));r=next.GetProperty("revision").GetInt64();
        var read=Json(_tools.GetDrawing(id));Assert.Equal(2,read.GetProperty("diagram").GetProperty("components").GetArrayLength());Assert.False(read.GetProperty("canUndo").GetBoolean());
        Assert.Throws<McpException>(()=>_tools.Undo(id,r));
    }
    [Fact] public void StructuredPropertiesRoundtripWithHistoryAndInvalidEditsDoNotAdvanceRevision()
    {
        var(id,r)=Create();var created=Json(_tools.AddComponent(id,r,"Beep.Skia.ERD.ERDEntity",10,20));r=created.GetProperty("revision").GetInt64();var element=Selected(created);
        var dto=created.GetProperty("diagram").GetProperty("components")[0];var property=dto.GetProperty("propertyBag").EnumerateObject().Single(p=>p.Name=="Indexes");
        var result=Json(_tools.SetComponentProperty(id,r,element,property.Name,property.Value.GetString()!));r=result.GetProperty("revision").GetInt64();
        var before=_tools.GetDrawing(id);Assert.Throws<McpException>(()=>_tools.SetComponentProperty(id,r,element,property.Name,"{invalid"));Assert.Equal(before,_tools.GetDrawing(id));
        Assert.Throws<McpException>(()=>_tools.SetComponentProperty(id,r,element,"UnknownProperty","bad"));Assert.Equal(before,_tools.GetDrawing(id));
        _tools.Undo(id,r);
    }
    [Fact] public void ConnectLibraryInsertAndNativeRestartPreserveFullGraph()
    {
        var(id,r)=Create();var p=Json(_tools.AddComponent(id,r,FlowchartDefinitions.ProcessType,0,0));r=p.GetProperty("revision").GetInt64();var source=Selected(p);
        var q=Json(_tools.AddComponent(id,r,FlowchartDefinitions.DecisionType,300,0));r=q.GetProperty("revision").GetInt64();var target=Selected(q);
        var linked=Json(_tools.ConnectNodes(id,r,source,0,target,0,"Yes","Orthogonal"));r=linked.GetProperty("revision").GetInt64();Assert.Single(linked.GetProperty("diagram").GetProperty("lines").EnumerateArray());
        var grouped=Json(_tools.GroupElements(id,r,[source,target]));r=grouped.GetProperty("revision").GetInt64();
        var saved=Json(_tools.SaveLibrarySelection(id,r,[source,target],"Workflow"));r=saved.GetProperty("revision").GetInt64();var entry=saved.GetProperty("library")[0].GetProperty("id").GetGuid();
        var copy=Json(_tools.InsertLibraryEntry(id,r,entry,0,300));Assert.Equal(4,copy.GetProperty("diagram").GetProperty("components").GetArrayLength());Assert.Equal(2,copy.GetProperty("diagram").GetProperty("lines").GetArrayLength());
        var exported=Json(_tools.ExportDrawing(id));var native=exported.GetProperty("nativeJson").GetString()!;
        Assert.Equal(native,File.ReadAllText(exported.GetProperty("path").GetString()!));
        using var restarted=new DrawingWorkspace(_root);var tools=new SimpleDrawTools(restarted);Assert.Equal(native,Json(tools.ExportDrawing(id)).GetProperty("nativeJson").GetString());
        Assert.True(File.ReadAllBytes(Json(_tools.ExportDrawing(id,"png")).GetProperty("path").GetString()!)[0]==137);
        Assert.Contains("<svg",File.ReadAllText(Json(_tools.ExportDrawing(id,"svg")).GetProperty("path").GetString()!));
        Assert.False(_tools.RenderDrawing(id).IsError??false);
    }
    [Fact] public void PathAndTextEditsTargetIdentityAndUndo()
    {
        var(id,r)=Create();var text=Json(_tools.AddSketch(id,r,"Text",0,0,text:"First"));r=text.GetProperty("revision").GetInt64();var first=Selected(text);
        var overlay=Json(_tools.AddSketch(id,r,"Text",0,0,text:"Top"));r=overlay.GetProperty("revision").GetInt64();
        var edited=Json(_tools.EditText(id,r,first,"Changed"));r=edited.GetProperty("revision").GetInt64();
        Assert.Equal("Changed",edited.GetProperty("diagram").GetProperty("components")[0].GetProperty("propertyBag").GetProperty("DisplayText").GetString());
        Assert.Equal("Top",edited.GetProperty("diagram").GetProperty("components")[1].GetProperty("propertyBag").GetProperty("DisplayText").GetString());
        var line=Json(_tools.AddSketch(id,r,"Arrow",points:[new(10,100),new(200,100)]));r=line.GetProperty("revision").GetInt64();var arrow=Selected(line);
        var changed=Json(_tools.SetPath(id,r,arrow,[new(20,110),new(300,160)]));r=changed.GetProperty("revision").GetInt64();
        var undo=Json(_tools.Undo(id,r));Assert.Equal(10,undo.GetProperty("diagram").GetProperty("components")[2].GetProperty("x").GetSingle());
    }
    [Fact] public void ViewportAndInvalidMotionKeepNativeGeometryAndHistory()
    {
        var(id,r)=Create();var added=Json(_tools.AddSketch(id,r,"Rectangle",10,10));r=added.GetProperty("revision").GetInt64();var node=Selected(added);var before=Json(_tools.ExportDrawing(id)).GetProperty("nativeJson").GetString();
        var viewport=Json(_tools.SetViewport(id,r,zoom:2,panX:-100));r=viewport.GetProperty("revision").GetInt64();Assert.Equal(before,Json(_tools.ExportDrawing(id)).GetProperty("nativeJson").GetString());
        Assert.Throws<McpException>(()=>_tools.MoveElements(id,r,[node],float.NaN,0));Assert.Throws<McpException>(()=>_tools.MoveElements(id,r,[node],1000000,0));
        var undo=Json(_tools.Undo(id,r));Assert.Empty(undo.GetProperty("diagram").GetProperty("components").EnumerateArray());
    }
    [Fact] public void ImportRejectsUnknownTypesAndDiscoveryPaginatesExactly249()
    {
        var first=Json(_tools.ListComponents());Assert.Equal(249,first.GetProperty("total").GetInt32());Assert.Equal(16,first.GetProperty("families").GetArrayLength());Assert.Equal(7,first.GetProperty("sketchKinds").GetArrayLength());
        Assert.Equal(49,Json(_tools.ListComponents(offset:200)).GetProperty("components").GetArrayLength());
        Assert.Throws<McpException>(()=>_tools.DescribeComponent("System.IO.File"));Assert.Throws<McpException>(()=>_tools.ImportDrawing("Bad","{}"));Assert.Empty(_workspace.List());
    }
    public void Dispose() { _workspace.Dispose(); if(Directory.Exists(_root)) Directory.Delete(_root,true); }
}
