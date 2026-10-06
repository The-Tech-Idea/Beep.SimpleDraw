using System.Text;
using Beep.SimpleDraw.Core;
using Beep.Skia;
using Beep.Skia.Flowchart;
using Beep.Skia.Model;
using ConnectionPoint = Beep.Skia.ConnectionPoint;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class FlowchartPortableTests
{
    private static FlowchartControl Create(int kind, Guid id, Guid[] inputs, Guid[] outputs) => kind switch
    { 0 => new ProcessNode(id, inputs, outputs), 1 => new DecisionNode(id, inputs, outputs), _ => new StartEndNode(id, inputs, outputs) };
    private static void Label(FlowchartControl node, string text)
    {
        switch (node) { case ProcessNode p: p.Label = text; break; case DecisionNode d: d.Label = text; break; case StartEndNode s: s.Label = text; break; }
    }
    private static DrawingContext Context() => new() { Bounds = new(0, 0, 300, 200), Zoom = 1 };

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void ConstructorsRestoreOrderedIdentitiesWithoutReflection(int kind)
    {
        var id = Guid.NewGuid(); var inputs = new[] { Guid.NewGuid(), Guid.NewGuid() }; var outputs = new[] { Guid.NewGuid() };
        using var node = Create(kind, id, inputs, outputs);
        Assert.Equal(id, node.Id);
        Assert.Equal(inputs, node.InConnectionPoints.Select(p => p.Id)); Assert.Equal(outputs, node.OutConnectionPoints.Select(p => p.Id));
        Assert.Equal(2, node.NodeProperties["InPortCount"].ParameterCurrentValue); Assert.Equal(1, node.NodeProperties["OutPortCount"].ParameterCurrentValue);
        Assert.All(node.InConnectionPoints.Concat(node.OutConnectionPoints), p => Assert.Same(node, p.Component));
        Assert.Throws<ArgumentException>(() => Create(kind, Guid.Empty, inputs, outputs));
        Assert.Throws<ArgumentException>(() => Create(kind, id, [id], []));
        Assert.Throws<ArgumentException>(() => Create(kind, id, [inputs[0]], [inputs[0]]));
        Assert.Throws<ArgumentException>(() => Create(kind, id, [Guid.Empty], []));
        Assert.Throws<ArgumentException>(() => Create(kind, id, Enumerable.Range(0, 65).Select(_ => Guid.NewGuid()).ToArray(), []));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void SelectedNodesHonorColorsAndPortableGlyphOutlines(int kind)
    {
        SimpleDrawFonts.EnsureLoaded();
        using var node = Create(kind, Guid.NewGuid(), [], []);
        node.X = 30; node.Y = 40; node.Width = 160; node.Height = 100; node.ShowDisplayText = false;
        node.CustomFillColor = SKColor.Parse("#31abcf"); node.CustomStrokeColor = SKColor.Parse("#4512ab");
        node.CustomTextColor = SKColor.Parse("#e02e63"); node.NodeStrokeWidth = 6; node.LabelFontSize = 22;
        node.RequiredLabelFontFamily = "Kalam"; Label(node, "");
        using var bitmap = new SKBitmap(300, 200); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.White);
        node.Draw(canvas, Context()); Assert.Equal(SKColor.Parse("#31abcf"), bitmap.GetPixel(110, 90));
        Label(node, "Plan");
        using var stream = new SKDynamicMemoryWStream();
        using (var svg = SKSvgCanvas.Create(new SKRect(0, 0, 300, 200), stream)) node.Draw(svg, Context());
        using var data = stream.DetachAsData(); var text = Encoding.UTF8.GetString(data.ToArray());
        Assert.Contains("#31ABCF", text, StringComparison.OrdinalIgnoreCase); Assert.Contains("#4512AB", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#E02E63", text, StringComparison.OrdinalIgnoreCase); Assert.Contains("stroke-width=\"6\"", text);
        Assert.Contains("<path", text); Assert.DoesNotContain("<text", text); Assert.DoesNotContain("font-family", text);
    }

    [Fact]
    public void RequiredFontAndUnsupportedGlyphFailuresRestoreCanvasState()
    {
        SimpleDrawFonts.EnsureLoaded();
        using var node = new ProcessNode(Guid.NewGuid(), [], []) { X = 20, Y = 20, ShowDisplayText = false, RequiredLabelFontFamily = "Missing required font" };
        using var bitmap = new SKBitmap(300, 200); using var canvas = new SKCanvas(bitmap); var saved = canvas.SaveCount;
        Assert.Throws<InvalidOperationException>(() => node.Draw(canvas, Context())); Assert.Equal(saved, canvas.SaveCount);
        node.RequiredLabelFontFamily = "Kalam"; node.Label = "\u4e2d";
        Assert.Throws<ArgumentException>(() => node.Draw(canvas, Context())); Assert.Equal(saved, canvas.SaveCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => node.NodeStrokeWidth = float.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => node.LabelFontSize = 0);
        Assert.Throws<ArgumentException>(() => new ConnectionPoint(Guid.Empty));
    }

    [Fact]
    public void RelayoutKeepsConnectedPortsUnavailableAndUpdatesGeometry()
    {
        SimpleDrawFonts.EnsureLoaded();
        using var node = new ProcessNode { ShowDisplayText = false, RequiredLabelFontFamily = "Kalam", X = 20, Y = 20 };
        var port = node.OutConnectionPoints[0]; port.Connection = new ConnectionPoint(); port.IsAvailable = false;
        using var bitmap = new SKBitmap(300, 200); using var canvas = new SKCanvas(bitmap);
        node.Draw(canvas, Context()); var before = port.Center; node.X += 40; node.Draw(canvas, Context());
        Assert.False(port.IsAvailable); Assert.Equal(before.X + 40, port.Center.X);
    }

    [Fact]
    public void SharedRendererUsesWorldClipForTranslatedExportAndViewport()
    {
        SimpleDrawFonts.EnsureLoaded();
        var manager = new DrawingManager { ShowGrid = false, ShowSelectionHandles = false };
        using var node = new ProcessNode(Guid.NewGuid(), [], []) { X = -500, Y = -400, Width = 160, Height = 100,
            ShowDisplayText = false, RequiredLabelFontFamily = "Kalam", CustomFillColor = SKColors.Red, Label = "" };
        manager.AddComponent(node);
        using var bitmap = new SKBitmap(300, 200); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.White);
        canvas.Translate(530, 440); manager.RenderingHelper.DrawForExport(canvas);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(110, 90));
        canvas.ResetMatrix(); canvas.Clear(SKColors.White); manager.Zoom = 2; manager.PanOffset = new(1030, 840);
        manager.RenderingHelper.DrawAll(canvas); Assert.Equal(SKColors.Red, bitmap.GetPixel(190, 140));
    }
}
