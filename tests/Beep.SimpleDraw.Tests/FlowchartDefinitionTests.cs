using System.Globalization;
using System.Text.Json;
using Beep.SimpleDraw.Core.Library;
using Beep.Skia.Flowchart;
using Beep.Skia.Serialization;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class FlowchartDefinitionTests
{
    private static FlowchartControl Node(int kind) => kind switch
    { 0 => new ProcessNode(), 1 => new DecisionNode(), _ => new StartEndNode() };
    private static ComponentDto Definition(int kind = 0)
    {
        using var node = Node(kind); node.ShowDisplayText = false; node.RequiredLabelFontFamily = "Kalam";
        node.X = 17.5f; node.Y = -24.25f; node.Width = 170.5f; node.Height = 100.5f; node.Name = "Reviewed node";
        node.InPortCount = 3; node.OutPortCount = 2; node.GroupId = Guid.NewGuid();
        node.CustomFillColor = SKColor.Parse("#efcfaa"); node.CustomStrokeColor = SKColor.Parse("#ff664488");
        node.CustomTextColor = SKColor.Parse("#111a22"); node.NodeStrokeWidth = 3.5f; node.LabelFontSize = 18.5f;
        FlowchartDefinitions.SetLabel(node, "Ready?"); FlowchartDefinitions.SetTopBottomPorts(node, true);
        return FlowchartDefinitions.Capture(node);
    }
    private static ComponentDto Clone(ComponentDto dto) => JsonSerializer.Deserialize<ComponentDto>(JsonSerializer.Serialize(dto))!;
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void FixedFactoryRoundtripPreservesEveryReviewedValue(int kind)
    {
        var definition = Definition(kind); using var restored = FlowchartDefinitions.Prepare(Clone(definition));
        Assert.Equal(definition.Id, restored.Id); Assert.Equal(definition.Name, restored.Name);
        Assert.Equal(definition.X, restored.X); Assert.Equal(definition.Y, restored.Y); Assert.Equal(definition.Width, restored.Width); Assert.Equal(definition.Height, restored.Height);
        Assert.Equal(definition.InPointIds, restored.InConnectionPoints.Select(p => p.Id)); Assert.Equal(definition.OutPointIds, restored.OutConnectionPoints.Select(p => p.Id));
        Assert.Equal("Ready?", FlowchartDefinitions.Label(restored)); Assert.True(FlowchartDefinitions.TopBottomPorts(restored));
        Assert.Equal("Kalam", restored.RequiredLabelFontFamily); Assert.False(restored.ShowDisplayText);
        Assert.Equal(JsonSerializer.Serialize(definition), JsonSerializer.Serialize(FlowchartDefinitions.Capture(restored)));
    }
    [Fact]
    public void CaptureUsesInvariantNumbersInsteadOfGeneralReflectionConversion()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); var definition = Definition();
            Assert.Equal("3.5", definition.PropertyBag["NodeStrokeWidth"]); Assert.Equal("18.5", definition.PropertyBag["LabelFontSize"]);
            using var restored = FlowchartDefinitions.Prepare(Clone(definition)); Assert.Equal(3.5f, restored.NodeStrokeWidth);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Fact]
    public void UnknownTypesAndPropertiesHaveNoFactory()
    {
        foreach (var type in new[] { "System.IO.File", "Beep.Skia.Flowchart.ProcessNode, Beep.Skia.FlowChart", "Beep.Skia.Flowchart.SourceNode", "beep.skia.flowchart.ProcessNode" })
        { var definition = Definition(); definition.Type = type; Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(definition)); }
        var extra = Definition(); extra.PropertyBag["Execute"] = "true"; Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(extra));
        var missing = Definition(); missing.PropertyBag.Remove("Label"); Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(missing));
        var typed = Definition(); typed.TypedPropertyBag["Label"] = JsonSerializer.SerializeToElement("Ready?"); Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(typed));
    }
    [Theory]
    [InlineData("Label", "")] [InlineData("Label", "two\nlines")] [InlineData("Label", "\u4e2d")]
    [InlineData("CustomFillColor", "red")] [InlineData("CustomTextColor", "#xyzxyz")]
    [InlineData("NodeStrokeWidth", "NaN")] [InlineData("NodeStrokeWidth", "0")] [InlineData("LabelFontSize", "121")]
    [InlineData("LabelFontSize", "18,5")] [InlineData("InPortCount", "9")] [InlineData("InPortCount", "-1")]
    [InlineData("OutPortCount", " 2")] [InlineData("ShowTopBottomPorts", "true")] [InlineData("GroupId", "")]
    public void InvalidPropertyValuesAreRefused(string property, string value)
    {
        var definition = Definition(); definition.PropertyBag[property] = value;
        Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(definition));
    }
    [Fact]
    public void IdentityBoundsAndPortCountFailuresAreRefusedBeforeConstruction()
    {
        var definitions = new List<ComponentDto>();
        var count = Definition(); count.OutPointIds.RemoveAt(0); definitions.Add(count);
        var duplicate = Definition(); duplicate.OutPointIds[0] = duplicate.InPointIds[0]; definitions.Add(duplicate);
        var empty = Definition(); empty.InPointIds[0] = Guid.Empty; definitions.Add(empty);
        var group = Definition(); group.PropertyBag["GroupId"] = group.InPointIds[0].ToString("D"); definitions.Add(group);
        var small = Definition(); small.Width = 19; definitions.Add(small);
        var far = Definition(); far.X = 100_000; definitions.Add(far);
        var name = Definition(); name.Name = "Bad\nname"; definitions.Add(name);
        foreach (var definition in definitions) Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Prepare(definition));
        using var unsupported = Node(0); Assert.Throws<InvalidDataException>(() => FlowchartDefinitions.Capture(unsupported));
    }
}
