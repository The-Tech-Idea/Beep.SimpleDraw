using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;
using Beep.SimpleDraw.Core.Documents;
using Beep.Skia;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class SkiaCatalogTests
{
    public static IEnumerable<object[]> Components => SkiaCatalog.Entries.Select(e => new object[] { e.TypeKey });
    [Fact]
    public void AllDiagramFamiliesAreBuiltInWithUniqueFactories()
    {
        Assert.Equal(16,SkiaCatalog.Entries.Select(e => e.Family).Distinct().Count());
        Assert.Equal(SkiaCatalog.Entries.Count,SkiaCatalog.Entries.Select(e => e.TypeKey).Distinct().Count());
    }
    [Theory, MemberData(nameof(Components))]
    public void EveryCatalogComponentSurvivesNativeLibraryHistoryAndRaster(string type)
    {
        var session = new DrawingSession(); session.ResizeViewport(1000,700);
        session.InsertTemplate(type);
        var original = Assert.Single(session.Manager.GetComponents());
        Assert.Equal(type,original.GetType().FullName);
        var json = SimpleDrawDocument.Serialize(session.Manager);
        session.MoveSelected(new SKPoint(17,23)); session.Undo();
        Assert.Equal(json,SimpleDrawDocument.Serialize(session.Manager));
        session.ResizeSelection(original.Width+10,original.Height+10); session.Undo();
        Assert.Equal(json,SimpleDrawDocument.Serialize(session.Manager));
        var restored = new DrawingSession(); SimpleDrawDocument.Apply(restored.Manager,SimpleDrawDocument.Parse(json));
        Assert.Equal(json,SimpleDrawDocument.Serialize(restored.Manager));
        var png = DrawingExport.Png(session.Manager);
        Assert.True(png.Length > 100);
        Assert.Equal(json,SimpleDrawDocument.Serialize(session.Manager));
        Assert.Equal(png,DrawingExport.Png(restored.Manager));
        Assert.True(DrawingExport.Svg(session.Manager).Length > 100);
        Assert.Equal(json,SimpleDrawDocument.Serialize(session.Manager));
        session.Manager.SelectionManager.SelectComponent(original);
        var library = new SketchLibrary(); var entry = library.SaveSelection(session.Manager,"Catalog item");
        session.InsertDiagram(entry.ToDiagram(),new SKPoint(50,50));
        var copy = session.Manager.GetComponents().Last();
        Assert.NotEqual(original.Id,copy.Id);
        Assert.Empty(original.InConnectionPoints.Concat(original.OutConnectionPoints).Select(p => p.Id)
            .Intersect(copy.InConnectionPoints.Concat(copy.OutConnectionPoints).Select(p => p.Id)));
        session.Undo(); Assert.Single(session.Manager.GetComponents());
        session.Redo(); Assert.Equal(2,session.Manager.GetComponents().Count);
    }
    public static IEnumerable<object[]> BusinessComponents => SkiaCatalog.Entries
        .Where(e => e.Family == "Business").Select(e => new object[] { e.TypeKey });
    [Theory, MemberData(nameof(BusinessComponents))]
    public void BusinessLayoutPreservesEditedPortTopologyAndNativeIdentity(string type)
    {
        var session = new DrawingSession(); session.ResizeViewport(1000,700); session.InsertTemplate(type);
        var node = Assert.IsAssignableFrom<Beep.Skia.Business.BusinessControl>(Assert.Single(session.Manager.GetComponents()));
        node.InPortCount = 3;
        node.OutPortCount = 4;
        var before = SimpleDrawDocument.Serialize(session.Manager);
        using var surface = SKSurface.Create(new SKImageInfo(1000,700));
        session.Manager.Draw(surface.Canvas);
        Assert.Equal(before,SimpleDrawDocument.Serialize(session.Manager));
        DrawingExport.Png(session.Manager);
        DrawingExport.Svg(session.Manager);
        Assert.Equal(before,SimpleDrawDocument.Serialize(session.Manager));
        Assert.Equal(3,node.InConnectionPoints.Count); Assert.Equal(4,node.OutConnectionPoints.Count);
    }
    [Fact]
    public void UnknownTypesAndPropertiesCannotSelectRuntimeCode()
    {
        Assert.Throws<InvalidDataException>(() => SkiaCatalog.Create("System.IO.File"));
        using var node = SkiaCatalog.Create("Beep.Skia.UML.UMLClass");
        var dto = SkiaCatalog.Capture(node); dto.PropertyBag["FilePath"] = "unknown";
        Assert.Throws<InvalidDataException>(() => SkiaCatalog.Prepare(dto));
    }
    [Fact]
    public void CatalogPropertyEditsUseTheExistingUndoAndNativeBoundary()
    {
        var session = new DrawingSession(); session.InsertTemplate("Beep.Skia.UML.UMLClass");
        var node = Assert.Single(session.Manager.GetComponents()); var before = SimpleDrawDocument.Serialize(session.Manager);
        var beforePixels = DrawingExport.Png(session.Manager);
        session.SetCatalogProperty(node.Id,"ClassName","Customer");
        var after = SimpleDrawDocument.Serialize(session.Manager); Assert.NotEqual(before,after);
        Assert.NotEqual(beforePixels,DrawingExport.Png(session.Manager));
        Assert.Equal("Customer",SkiaCatalog.Capture(node).PropertyBag["DisplayText"]);
        session.Undo(); Assert.Equal(before,SimpleDrawDocument.Serialize(session.Manager));
        session.Redo(); Assert.Equal(after,SimpleDrawDocument.Serialize(session.Manager));
        Assert.Throws<InvalidDataException>(() => session.SetCatalogProperty(node.Id,"TextFontSize","NaN"));
        Assert.Equal(after,SimpleDrawDocument.Serialize(session.Manager));
        session.SetStyle(stroke:"#e06b56",fill:"#ede9fe",width:4);
        Assert.Equal("#ffe06b56",SkiaCatalog.Capture(node).PropertyBag["BorderColor"]);
        session.Undo(); Assert.Equal(after,SimpleDrawDocument.Serialize(session.Manager));
    }
    [Fact]
    public void CrossFamilyConnectionsKeepPhysicalPortIdentityAndHistory()
    {
        var session = new DrawingSession(); session.InsertTemplate("Beep.Skia.UML.UMLClass");
        session.InsertTemplate("Beep.Skia.ERD.ERDEntity");
        var nodes = session.Manager.GetComponents().ToArray();
        session.Connect(nodes[0].Id,0,nodes[1].Id,0,"Owns",Beep.Skia.Model.LineRoutingMode.Straight);
        var json = SimpleDrawDocument.Serialize(session.Manager);
        var restored = new DrawingSession(); restored.OpenDocument(json);
        Assert.Single(restored.Manager.GetLines()); Assert.Equal(json,SimpleDrawDocument.Serialize(restored.Manager));
        session.Undo(); Assert.Empty(session.Manager.GetLines()); session.Redo(); Assert.Single(session.Manager.GetLines());
        Assert.Equal(json,SimpleDrawDocument.Serialize(session.Manager));
    }
}
