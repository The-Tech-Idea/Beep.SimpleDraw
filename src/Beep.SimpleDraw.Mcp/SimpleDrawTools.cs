using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Documents;
using Beep.SimpleDraw.Core.Library;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SkiaSharp;

namespace Beep.SimpleDraw.Mcp;

public sealed record DrawingPoint(float X, float Y);
[McpServerToolType]
public sealed class SimpleDrawTools(DrawingWorkspace workspace)
{
    private static string Result(Func<object> action)
    {
        try { return JsonSerializer.Serialize(action(),DrawingWorkspace.Json); }
        catch (Exception ex) when(ex is InvalidDataException or ArgumentException or InvalidOperationException or FormatException or OverflowException or JsonException) { throw new McpException(ex.Message); }
    }
    private string Change(Guid drawingId,long expectedRevision,Action<BoardState> action) => Result(()=>workspace.Change(drawingId,expectedRevision,action));
    private static void Select(BoardState board,Guid[] elementIds)
    {
        var nodes=board.Session.Manager.GetComponents().Where(n=>elementIds.Contains(n.Id)).ToArray();
        if(elementIds.Length==0||elementIds.Distinct().Count()!=elementIds.Length||nodes.Length!=elementIds.Length) throw new InvalidDataException("Provide distinct existing element IDs from get_drawing.");
        board.Session.Tool=DrawingTool.Select; board.Session.Manager.SelectionManager.ClearSelection();
        foreach(var node in nodes) if(!node.IsSelected) board.Session.SelectShape(node,true);
    }
    private static T EnumValue<T>(string name) where T:struct,Enum => Enum.TryParse<T>(name,true,out var value)&&Enum.IsDefined(value) ? value : throw new InvalidDataException($"Use one of: {string.Join(", ",Enum.GetNames<T>())}.");

    [McpServerTool(Name="list_components",ReadOnly=true),Description("Discover all 249 embedded Beep.Skia components across 16 families, plus seven sketch kinds. Returns exact type keys; paginate before selecting a component.")]
    public string ListComponents(string? family=null,string? search=null,int offset=0,int limit=100) => Result(()=>CatalogDescription.List(family,search,offset,limit));
    [McpServerTool(Name="describe_component",ReadOnly=true),Description("Get every serialized editable property of a Beep.Skia component, defaults, CLR types, JSON structure, enums and default port topology. Use the exact type returned by list_components.")]
    public string DescribeComponent(string type) => Result(()=>CatalogDescription.Describe(type));
    [McpServerTool(Name="list_drawings",ReadOnly=true),Description("List persistent drawings in this workspace with IDs, revisions and object/connector counts. Claude and Codex share this directory.")]
    public string ListDrawings() => Result(()=>workspace.List());
    [McpServerTool(Name="create_drawing"),Description("Create a named empty drawing. Returns ID and revision. All mutations require the current expectedRevision returned by a previous call.")]
    public string CreateDrawing(string name) => Result(()=>{var f=workspace.Create(name); return new {f.Id,f.Name,f.Revision};});
    [McpServerTool(Name="import_drawing"),Description("Create a drawing from native SimpleDraw JSON (versions 1-7), preserving identities, all typed properties and connections. Validates before saving; does not overwrite other drawings.")]
    public string ImportDrawing(string name,string nativeJson) => Result(()=>{var f=workspace.Create(name,nativeJson); return new {f.Id,f.Name,f.Revision};});
    [McpServerTool(Name="get_drawing",ReadOnly=true),Description("Read full native diagram: all element IDs, bounds, scalar and structured properties, stable input/output port IDs, connectors, revision, selection and current history/viewport.")]
    public string GetDrawing(Guid drawingId) => Result(()=>workspace.Read(drawingId,DrawingWorkspace.Snapshot));
    [McpServerTool(Name="add_component"),Description("Insert any embedded Beep.Skia type at world coordinates using its default bounds or supplied width/height. Returns full drawing and selected new element ID. Use describe_component then set_component_property to configure it.")]
    public string AddComponent(Guid drawingId,long expectedRevision,string type,float x,float y,float? width=null,float? height=null) => Change(drawingId,expectedRevision,b=>{
        using var node=SkiaCatalog.Create(type); if(node is Beep.Skia.Flowchart.FlowchartControl flow && FlowchartDefinitions.Supports(type)) { flow.ShowDisplayText=false;flow.RequiredLabelFontFamily="Kalam"; }
        var dto=node is Beep.Skia.Flowchart.FlowchartControl f && FlowchartDefinitions.Supports(type) ? FlowchartDefinitions.Capture(f) : SkiaCatalog.Capture(node); dto.X=0;dto.Y=0;dto.Width=width??node.Width;dto.Height=height??node.Height;
        b.Session.InsertDiagram(new DiagramDto{Components=[dto]},new(x,y));
    });
    [McpServerTool(Name="add_sketch"),Description("Create Rectangle, Ellipse, Diamond, Line, Arrow, Freehand or Text. Bounds and path points use world coordinates. Line/Arrow/Freehand require 2-4096 points. Text uses bundled font measurements. Colors accept #RRGGBB or #AARRGGBB.")]
    public string AddSketch(Guid drawingId,long expectedRevision,string kind,float x=0,float y=0,float width=120,float height=80,string text="",string stroke="#334155",string fill="#00000000",float strokeWidth=2,float fontSize=24,float roughness=0,DrawingPoint[]? points=null) => Change(drawingId,expectedRevision,b=>{
        SimpleDrawFonts.EnsureLoaded();
        using var shape=new SketchShape{Kind=EnumValue<SketchShapeKind>(kind),Name=kind,X=x,Y=y,Width=width,Height=height,Stroke=stroke,Fill=fill,StrokeWidth=strokeWidth,TextFontSize=fontSize,DisplayText=text,Roughness=roughness};
        if(shape.IsPath) shape.SetWorldPath(points?.Select(p=>new SKPoint(p.X,p.Y)).ToArray()??throw new InvalidDataException("Path points are required."));
        if(shape.Kind==SketchShapeKind.Text) { var size=shape.MeasureText();shape.Width=size.Width;shape.Height=size.Height; }
        var temp=new DrawingSession();temp.Manager.AddComponent(shape);
        var diagram=SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(temp.Manager));
        b.Session.InsertDiagram(diagram,new(0,0)); temp.NewDocument();
    });
    [McpServerTool(Name="set_component_property"),Description("Edit any serialized Beep.Skia property with undo support. value uses invariant text for scalar properties (including enums/colors) or a JSON string for structured properties. Supports all properties returned by describe_component, including JSON-backed domain data.")]
    public string SetComponentProperty(Guid drawingId,long expectedRevision,Guid elementId,string property,string value) => Change(drawingId,expectedRevision,b=>{
        if(property=="GroupId") throw new InvalidDataException("Use group_elements for grouping.");
        b.Session.SetCatalogProperty(elementId,property,value);
    });
    [McpServerTool(Name="set_style"),Description("Apply stroke/fill/stroke width/sketch roughness/font size to selected sketch or catalog objects using the same style logic as the UI. Unsupported component style mappings are explicit in describe_component.")]
    public string SetStyle(Guid drawingId,long expectedRevision,Guid[] elementIds,string? stroke=null,string? fill=null,float? strokeWidth=null,float? roughness=null,float? fontSize=null) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.SetStyle(stroke,fill,strokeWidth,roughness,fontSize);});
    [McpServerTool(Name="edit_text"),Description("Edit an existing sketch Text element with font validation, portable measured bounds and undo. Beep.Skia component labels use their serialized properties through set_component_property.")]
    public string EditText(Guid drawingId,long expectedRevision,Guid elementId,string text,float? fontSize=null) => Change(drawingId,expectedRevision,b=>{
        var node=b.Session.Manager.GetComponents().SingleOrDefault(c=>c.Id==elementId) as SketchShape;
        if(node?.Kind!=SketchShapeKind.Text) throw new InvalidDataException("Choose an existing sketch Text element.");
        Select(b,[elementId]); b.Session.BeginTextById(elementId);try { if(!b.Session.CommitText(text,fontSize)) throw new InvalidDataException("Text edit was rejected."); } finally {b.Session.CancelGesture();}
    });
    [McpServerTool(Name="set_path"),Description("Replace world-coordinate points on an existing sketch Line, Arrow or Freehand, preserving its identity/style and supporting undo. All native path/bounds limits apply.")]
    public string SetPath(Guid drawingId,long expectedRevision,Guid elementId,DrawingPoint[] points) => Change(drawingId,expectedRevision,b=>{
        var node=b.Session.Manager.GetComponents().SingleOrDefault(c=>c.Id==elementId) as SketchShape;
        if(node is null||!node.IsPath) throw new InvalidDataException("Choose an existing path element.");
        var before=ComponentEditAction.State.Capture(node);using var candidate=new SketchShape();before.Apply(candidate);candidate.SetWorldPath(points.Select(p=>new SKPoint(p.X,p.Y)).ToArray());
        if(!SimpleDrawDocument.Bounds(candidate.X,candidate.Y,candidate.Width,candidate.Height)) throw new InvalidDataException("Path exceeds native bounds.");
        ComponentEditAction.State.Capture(candidate).Apply(node);b.Session.Manager.HistoryManager.ExecuteAction(new ComponentEditAction([node],[before]));
    });
    [McpServerTool(Name="select_elements"),Description("Select explicit element IDs, including grouped objects, using SimpleDraw selection semantics.")]
    public string SelectElements(Guid drawingId,long expectedRevision,Guid[] elementIds) => Change(drawingId,expectedRevision,b=>Select(b,elementIds));
    [McpServerTool(Name="move_elements"),Description("Move selected element IDs by world-coordinate dx/dy, preserving ports/connections/groups. Rejects non-finite or out-of-board motion; supports undo.")]
    public string MoveElements(Guid drawingId,long expectedRevision,Guid[] elementIds,float dx,float dy) => Change(drawingId,expectedRevision,b=>{
        Select(b,elementIds);if(!float.IsFinite(dx)||!float.IsFinite(dy)||b.Session.SelectedComponents.Any(n=>!SimpleDrawDocument.Bounds(n.X+dx,n.Y+dy,n.Width,n.Height))) throw new InvalidDataException("Move exceeds native board bounds.");b.Session.MoveSelected(new(dx,dy));
    });
    [McpServerTool(Name="resize_elements"),Description("Resize the selection bounding box to world-coordinate width/height, proportionally scaling its members. Preserves text font measurements and existing catalog resize rules; supports undo.")]
    public string ResizeElements(Guid drawingId,long expectedRevision,Guid[] elementIds,float width,float height) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.ResizeSelection(width,height);});
    [McpServerTool(Name="delete_elements",Destructive=true),Description("Delete element IDs and associated connectors with undo support. This edits the selected drawing only.")]
    public string DeleteElements(Guid drawingId,long expectedRevision,Guid[] elementIds) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.DeleteSelected();});
    [McpServerTool(Name="duplicate_elements"),Description("Duplicate selected objects with new element/group/port identities using the UI duplication contract. Supports undo.")]
    public string DuplicateElements(Guid drawingId,long expectedRevision,Guid[] elementIds) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.DuplicateSelection();});
    [McpServerTool(Name="group_elements"),Description("Group selected objects, or ungroup them when ungroup=true, using shared selection and undo contracts.")]
    public string GroupElements(Guid drawingId,long expectedRevision,Guid[] elementIds,bool ungroup=false) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.GroupSelection(ungroup);});
    [McpServerTool(Name="reorder_elements"),Description("Bring selected elements to the front or send them to the back with undo support.")]
    public string ReorderElements(Guid drawingId,long expectedRevision,Guid[] elementIds,bool front=true) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Session.ReorderSelection(front);});
    [McpServerTool(Name="connect_nodes"),Description("Connect two Beep.Skia catalog elements by zero-based output/input port indices, with label and Straight/Orthogonal/Curved routing. Port identity/availability validation matches the UI. Read ports using get_ports.")]
    public string ConnectNodes(Guid drawingId,long expectedRevision,Guid sourceId,int output,Guid targetId,int input,string? label=null,string routing="Straight") => Change(drawingId,expectedRevision,b=>b.Session.Connect(sourceId,output,targetId,input,label,EnumValue<LineRoutingMode>(routing)));
    [McpServerTool(Name="get_ports",ReadOnly=true),Description("Read actual input/output ports on a drawing element: stable IDs, index, datatype, availability and world coordinates. Default topology is also in describe_component.")]
    public string GetPorts(Guid drawingId,Guid elementId) => Result(()=>workspace.Read(drawingId,b=>{
        var node=b.Session.Manager.GetComponents().SingleOrDefault(c=>c.Id==elementId)??throw new InvalidDataException("Element not found.");
        return (object)new {elementId,inputs=node.InConnectionPoints.Select((p,i)=>new {index=i,p.Id,p.DataType,p.IsAvailable,x=p.Position.X,y=p.Position.Y}),outputs=node.OutConnectionPoints.Select((p,i)=>new {index=i,p.Id,p.DataType,p.IsAvailable,x=p.Position.X,y=p.Position.Y})};
    }));
    [McpServerTool(Name="remove_connection",Destructive=true),Description("Remove the connector identified by its stable start/end port IDs. Supports undo through the shared drawing manager.")]
    public string RemoveConnection(Guid drawingId,long expectedRevision,Guid startPortId,Guid endPortId) => Change(drawingId,expectedRevision,b=>{
        var line=b.Session.Manager.GetLines().SingleOrDefault(l=>ConnectorDefinitions.Capture(l).StartPointId==startPortId&&ConnectorDefinitions.Capture(l).EndPointId==endPortId)??throw new InvalidDataException("Connection not found.");b.Session.RemoveConnection(line);
    });
    [McpServerTool(Name="undo"),Description("Undo the latest drawing edit in this server process. History resets when another process changes this drawing or the server restarts; native files persist.")]
    public string Undo(Guid drawingId,long expectedRevision) => Change(drawingId,expectedRevision,b=>{if(!b.Session.Manager.CanUndo) throw new InvalidDataException("No undo history in this process.");b.Session.Undo();});
    [McpServerTool(Name="redo"),Description("Redo the last undone drawing edit in this server process.")]
    public string Redo(Guid drawingId,long expectedRevision) => Change(drawingId,expectedRevision,b=>{if(!b.Session.Manager.CanRedo) throw new InvalidDataException("No redo history in this process.");b.Session.Redo();});
    [McpServerTool(Name="set_viewport"),Description("Set viewport size, world pan and zoom, or fit=true to fit the drawing. Viewport-only changes do not modify native geometry or undo history.")]
    public string SetViewport(Guid drawingId,long expectedRevision,float width=800,float height=600,float zoom=1,float panX=0,float panY=0,bool fit=false) => Change(drawingId,expectedRevision,b=>{
        if(!float.IsFinite(width)||!float.IsFinite(height)||width<1||height<1||width>8192||height>8192||!float.IsFinite(zoom)||zoom<.1f||zoom>8||!float.IsFinite(panX)||!float.IsFinite(panY)) throw new InvalidDataException("Invalid viewport dimensions, zoom (0.1-8), or pan.");
        b.Session.ResizeViewport(width,height);b.Session.Manager.Zoom=zoom;b.Session.Manager.PanOffset=new(panX,panY);if(fit)b.Session.FitDrawing();
    });
    [McpServerTool(Name="save_library_selection"),Description("Save selected objects and their internal connectors as a reusable personal-library entry in this drawing. Uses native library size/group limits.")]
    public string SaveLibrarySelection(Guid drawingId,long expectedRevision,Guid[] elementIds,string name) => Change(drawingId,expectedRevision,b=>{Select(b,elementIds);b.Library.SaveSelection(b.Session.Manager,name);});
    [McpServerTool(Name="insert_library_entry"),Description("Insert a reusable library entry at world coordinates, generating fresh object/group/port IDs and preserving internal connectors.")]
    public string InsertLibraryEntry(Guid drawingId,long expectedRevision,Guid entryId,float x,float y) => Change(drawingId,expectedRevision,b=>b.Session.InsertDiagram(b.Library.Find(entryId).ToDiagram(),new(x,y)));
    [McpServerTool(Name="import_library"),Description("Merge native SimpleDraw library JSON into the drawing's personal library after complete validation.")]
    public string ImportLibrary(Guid drawingId,long expectedRevision,string libraryJson) => Change(drawingId,expectedRevision,b=>b.Library.Import(libraryJson));
    [McpServerTool(Name="remove_library_entry",Destructive=true),Description("Remove a personal-library entry from this drawing. Existing canvas objects remain. Library removal is persisted and does not use drawing undo.")]
    public string RemoveLibraryEntry(Guid drawingId,long expectedRevision,Guid entryId) => Change(drawingId,expectedRevision,b=>b.Library.Remove(entryId));
    [McpServerTool(Name="export_drawing",ReadOnly=true),Description("Export native SimpleDraw JSON, PNG, SVG or personal-library JSON to a generated file under this workspace/artifacts. Returns absolute path and native JSON for native/library export. Open native files in SimpleDraw. Image exports use all actual Beep.Skia renderers.")]
    public string ExportDrawing(Guid drawingId,string format="native",float scale=1,bool background=true) => Result(()=>workspace.Read(drawingId,b=>{
        var (extension,mime,bytes)=format.ToLowerInvariant() switch {
            "native"=>("simpledraw.json","application/json",Encoding.UTF8.GetBytes(SimpleDrawDocument.Serialize(b.Session.Manager))),
            "library"=>("library.json","application/json",Encoding.UTF8.GetBytes(b.Library.Serialize())),
            "png"=>("png","image/png",DrawingExport.Png(b.Session.Manager,scale,background)),
            "svg"=>("svg","image/svg+xml",DrawingExport.Svg(b.Session.Manager,scale,background)),
            _=>throw new InvalidDataException("Use native, library, png or svg.")};
        return (object)new {drawingId,b.File.Revision,format,mime,bytes=bytes.Length,path=workspace.WriteArtifact(drawingId,b.File.Revision,extension,bytes),nativeJson=format is "native" or "library"?Encoding.UTF8.GetString(bytes):null};
    }));
    [McpServerTool(Name="render_drawing",ReadOnly=true),Description("Return an actual PNG image of the drawing inline to Claude/Codex for visual inspection, plus a saved artifact path. Uses real Beep.Skia rendering with strict export pixel limits.")]
    public CallToolResult RenderDrawing(Guid drawingId,float scale=1,bool background=true)
    {
        try {return workspace.Read(drawingId,b=>{var png=DrawingExport.Png(b.Session.Manager,scale,background);var path=workspace.WriteArtifact(drawingId,b.File.Revision,"png",png);return new CallToolResult{Content=[new TextContentBlock{Text=JsonSerializer.Serialize(new {drawingId,b.File.Revision,path})},ImageContentBlock.FromBytes(png,"image/png")]};});}
        catch(Exception ex) when(ex is InvalidDataException or ArgumentException) {throw new McpException(ex.Message);}
    }
}
