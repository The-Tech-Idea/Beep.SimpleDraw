using System.Text;
using System.Xml.Linq;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Documents;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;
public sealed class DocumentReliabilityTests
{
    [Fact] public void ActualWasmExportsDecodeToExpectedDimensionsAndSelfContainedXml()
    {
        var board = Board(); var layout = DrawingExport.Layout(board.Manager);
        using var png = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","browser.png")));
        Assert.Equal(layout.Width,png.Width); Assert.Equal(layout.Height,png.Height);
        var svg = XDocument.Load(Path.Combine(AppContext.BaseDirectory,"Fixtures","browser.svg"));
        Assert.Equal("svg",svg.Root!.Name.LocalName); Assert.NotEmpty(svg.Descendants().Where(e=>e.Name.LocalName=="path"));
        Assert.DoesNotContain(svg.Descendants(),e=>e.Name.LocalName is "text" or "script" or "foreignObject");
        Assert.DoesNotContain(svg.Descendants().Attributes(),a=>a.Name.LocalName=="font-family");
        Assert.DoesNotContain(svg.Descendants().Attributes(),a=>a.Name.LocalName is "href" || a.Name.LocalName.StartsWith("on",StringComparison.Ordinal));
    }
    [Fact] public void LateAcknowledgmentDoesNotClearNewerRevisionAndFailedSaveRemainsPending()
    {
        var save = new AutosaveState(); save.Restore(18); save.Changed(); var captured = save.Revision;
        save.Changed(); save.Acknowledge(captured); Assert.True(save.Pending); Assert.Equal(19, save.SavedRevision);
        save.Failed("quota"); Assert.True(save.Pending); Assert.Equal("quota", save.Error);
        save.Acknowledge(20); Assert.False(save.Pending); Assert.Null(save.Error);
        Assert.Throws<ArgumentOutOfRangeException>(() => save.Acknowledge(21));
    }
    private static DrawingSession Board()
    {
        var session = new DrawingSession(); session.OpenDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","v4.json"))); return session;
    }
    [Fact] public void PngHasBoundedRealDimensionsBackgroundAndNoSelectionOrViewportAdorners()
    {
        var board = Board(); var layout = DrawingExport.Layout(board.Manager, 2); var plain = DrawingExport.Png(board.Manager, 2);
        board.Manager.SelectAllComponents(); board.Manager.ShowGrid = true; board.Manager.PanOffset = new(700, 500); board.Manager.Zoom = 2;
        Assert.Equal(plain, DrawingExport.Png(board.Manager,2));
        using var bitmap = SKBitmap.Decode(plain); Assert.Equal(layout.Width, bitmap.Width); Assert.Equal(layout.Height,bitmap.Height);
        Assert.Equal(SKColor.Parse("#fcfcf9"),bitmap.GetPixel(0,0));
        using var transparent = SKBitmap.Decode(DrawingExport.Png(board.Manager,1,false)); Assert.Equal(0,transparent.GetPixel(0,0).Alpha);
    }
    [Fact] public void SvgIsSelfContainedXmlWithDrawnPathsAndNoExecutableOrExternalReferences()
    {
        var board = Board(); var svg = Encoding.UTF8.GetString(DrawingExport.Svg(board.Manager));
        var xml = XDocument.Parse(svg); Assert.Equal("svg",xml.Root!.Name.LocalName);
        Assert.Contains(xml.Descendants(),e => e.Name.LocalName == "path");
        Assert.DoesNotContain(xml.Descendants(),e => e.Name.LocalName is "script" or "foreignObject" or "text");
        Assert.DoesNotContain(xml.Descendants().Attributes(),a => a.Name.LocalName is "href" || a.Name.LocalName.StartsWith("on",StringComparison.Ordinal));
        var plain = DrawingExport.Svg(board.Manager); board.Manager.SelectAllComponents(); board.Manager.Zoom = .5f; board.Manager.PanOffset = new(200,100);
        Assert.Equal(plain,DrawingExport.Svg(board.Manager));
    }
    [Fact] public void LargeSparseBoardAndInvalidScaleAreRejectedBeforeAllocation()
    {
        var board = Board(); board.Manager.GetComponents()[0].X = -80_000;
        Assert.Throws<InvalidDataException>(() => DrawingExport.Png(board.Manager));
        Assert.Throws<InvalidDataException>(() => DrawingExport.Svg(board.Manager));
        Assert.Throws<InvalidDataException>(() => DrawingExport.Layout(Board().Manager,float.NaN));
        Assert.Throws<InvalidDataException>(() => DrawingExport.Png(new DrawingSession().Manager));
    }
    [Fact] public void WideArrowHeadIsIncludedInCrop()
    {
        var session = new DrawingSession{Tool=DrawingTool.Arrow,StrokeWidth=20}; session.PointerDown(new(100,100)); session.PointerUp(new(600,100));
        var arrow = (SketchShape)Assert.Single(session.Manager.GetComponents()); var layout = DrawingExport.Layout(session.Manager);
        Assert.True(layout.Bounds.Contains(arrow.ArrowHead().Left)); Assert.True(layout.Bounds.Contains(arrow.ArrowHead().Right));
    }
}
