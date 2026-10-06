using System.Globalization;
using Beep.Skia;
using Beep.Skia.Flowchart;
using Beep.Skia.Serialization;
using SkiaSharp;

namespace Beep.SimpleDraw.Core.Library;

/// <summary>Reviewed fixed factories over existing Beep components/DTOs; never resolve file-supplied CLR types.</summary>
public static class FlowchartDefinitions
{
    public const int MaxPortsPerDirection = 8;
    public const int MaxLabelLength = 128;
    public const string ProcessType = "Beep.Skia.Flowchart.ProcessNode";
    public const string DecisionType = "Beep.Skia.Flowchart.DecisionNode";
    public const string TerminatorType = "Beep.Skia.Flowchart.StartEndNode";
    private static readonly string[] Keys = ["InPortCount", "OutPortCount", "CustomFillColor", "CustomStrokeColor", "CustomTextColor",
        "Label", "ShowTopBottomPorts", "NodeStrokeWidth", "LabelFontSize", "GroupId"];
    public static bool Supports(string? type) => type is ProcessType or DecisionType or TerminatorType;
    public static string TypeKey(FlowchartControl node) => node switch
    {
        ProcessNode p when p.GetType() == typeof(ProcessNode) => ProcessType,
        DecisionNode d when d.GetType() == typeof(DecisionNode) => DecisionType,
        StartEndNode s when s.GetType() == typeof(StartEndNode) => TerminatorType,
        _ => throw new InvalidDataException("This flowchart component has no reviewed factory.")
    };
    public static string Label(FlowchartControl node) => node switch
    { ProcessNode p => p.Label, DecisionNode d => d.Label, StartEndNode s => s.Label, _ => throw new InvalidDataException("Unsupported flowchart component.") };
    public static void SetLabel(FlowchartControl node, string label)
    {
        ValidateLabel(label);
        switch (node) { case ProcessNode p: p.Label = label; break; case DecisionNode d: d.Label = label; break;
            case StartEndNode s: s.Label = label; break; default: throw new InvalidDataException("Unsupported flowchart component."); }
    }
    public static bool TopBottomPorts(FlowchartControl node) => node switch
    { ProcessNode p => p.ShowTopBottomPorts, DecisionNode d => d.ShowTopBottomPorts, StartEndNode s => s.ShowTopBottomPorts,
        _ => throw new InvalidDataException("Unsupported flowchart component.") };
    public static void SetTopBottomPorts(FlowchartControl node, bool value)
    {
        switch (node) { case ProcessNode p: p.ShowTopBottomPorts = value; break; case DecisionNode d: d.ShowTopBottomPorts = value; break;
            case StartEndNode s: s.ShowTopBottomPorts = value; break; default: throw new InvalidDataException("Unsupported flowchart component."); }
    }

    public static ComponentDto Capture(FlowchartControl node)
    {
        if (node.ShowDisplayText || !string.IsNullOrEmpty(node.DisplayText) || node.Children.Count != 0 || node.IsStatic || !node.IsVisible
            || !node.IsEnabled || node.Opacity != 1 || node.RequiredLabelFontFamily != "Kalam" || !Keys.ToHashSet(StringComparer.Ordinal).SetEquals(node.NodeProperties.Keys))
            throw new InvalidDataException("This node has unsupported presentation or properties.");
        var dto = new ComponentDto { Id = node.Id, Type = TypeKey(node), Name = node.Name, X = node.X, Y = node.Y,
            Width = node.Width, Height = node.Height, InPointIds = node.InConnectionPoints.Select(p => p.Id).ToList(),
            OutPointIds = node.OutConnectionPoints.Select(p => p.Id).ToList(), PropertyBag = new(StringComparer.Ordinal)
            {
                ["InPortCount"] = node.InPortCount.ToString(CultureInfo.InvariantCulture), ["OutPortCount"] = node.OutPortCount.ToString(CultureInfo.InvariantCulture),
                ["CustomFillColor"] = Color(node.CustomFillColor), ["CustomStrokeColor"] = Color(node.CustomStrokeColor), ["CustomTextColor"] = Color(node.CustomTextColor),
                ["Label"] = Label(node), ["ShowTopBottomPorts"] = TopBottomPorts(node).ToString(), ["NodeStrokeWidth"] = node.NodeStrokeWidth.ToString("R", CultureInfo.InvariantCulture),
                ["LabelFontSize"] = node.LabelFontSize.ToString("R", CultureInfo.InvariantCulture), ["GroupId"] = node.GroupId.ToString("D")
            } };
        using var validated = Prepare(dto); return dto;
    }
    private static string Color(SKColor? value) => value?.ToString() ?? "";
    private static SKColor? Color(string value)
    {
        if (value == "") return null;
        if (value is null || value.Length is not (7 or 9) || value[0] != '#' || !SKColor.TryParse(value, out var color))
            throw new InvalidDataException("Invalid flowchart color.");
        return color;
    }
    private static float Number(string value) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.IsFinite(result)
        ? result : throw new InvalidDataException("Invalid flowchart numeric property.");
    private static int Count(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count <= MaxPortsPerDirection
        ? count : throw new InvalidDataException("A flowchart node permits zero through eight ports per direction.");
    private static bool Boolean(string value) => value switch { "True" => true, "False" => false, _ => throw new InvalidDataException("Invalid flowchart port policy.") };
    public static void ValidateLabel(string label)
    {
        if (label is null || string.IsNullOrWhiteSpace(label) || label.Length > MaxLabelLength || label.Any(char.IsControl))
            throw new InvalidDataException("A flowchart label needs 1–128 characters on one line.");
        SimpleDrawFonts.EnsureLoaded();
        using var font = new SKFont(TypefaceCache.GetRequired("Kalam"), 14);
        if (!font.ContainsGlyphs(label)) throw new InvalidDataException("The bundled Kalam font does not support one or more label characters.");
    }
    public static FlowchartControl Prepare(ComponentDto dto)
    {
        if (dto is null || !Supports(dto.Type) || dto.Id == Guid.Empty || dto.Name is null || dto.Name.Length > 120 || dto.Name.Any(char.IsControl)
            || !SimpleDrawDocument.Bounds(dto.X, dto.Y, dto.Width, dto.Height) || dto.Width < 20 || dto.Height < 20
            || dto.PropertyBag is null || !Keys.ToHashSet(StringComparer.Ordinal).SetEquals(dto.PropertyBag.Keys)
            || dto.PropertyBag.Any(pair => pair.Value is null || pair.Value.Length > (pair.Key == "Label" ? MaxLabelLength : 36))
            || dto.TypedPropertyBag is null || dto.TypedPropertyBag.Count != 0 || dto.InPointIds is null || dto.OutPointIds is null)
            throw new InvalidDataException("A flowchart node has unsupported identity, geometry or properties.");
        var values = dto.PropertyBag;
        var inputs = Count(values["InPortCount"]); var outputs = Count(values["OutPortCount"]);
        if (inputs != dto.InPointIds.Count || outputs != dto.OutPointIds.Count)
            throw new InvalidDataException("Port count and persisted port identities disagree.");
        var identities = dto.InPointIds.Concat(dto.OutPointIds).Prepend(dto.Id).ToArray();
        if (identities.Any(id => id == Guid.Empty) || identities.Distinct().Count() != identities.Length
            || !Guid.TryParseExact(values["GroupId"], "D", out var group) || identities.Contains(group))
            throw new InvalidDataException("Component, group and port identities must be distinct.");
        ValidateLabel(values["Label"]); var topBottom = Boolean(values["ShowTopBottomPorts"]);
        var fill = Color(values["CustomFillColor"]); var stroke = Color(values["CustomStrokeColor"]); var text = Color(values["CustomTextColor"]);
        var thickness = Number(values["NodeStrokeWidth"]); var size = Number(values["LabelFontSize"]);
        if (thickness is < 1 or > 12 || size is < 8 or > 120) throw new InvalidDataException("Invalid flowchart border or label size.");
        FlowchartControl node = dto.Type switch
        { ProcessType => new ProcessNode(dto.Id, dto.InPointIds, dto.OutPointIds), DecisionType => new DecisionNode(dto.Id, dto.InPointIds, dto.OutPointIds),
            TerminatorType => new StartEndNode(dto.Id, dto.InPointIds, dto.OutPointIds), _ => throw new InvalidDataException("Unknown flowchart type.") };
        try
        {
            node.X = dto.X; node.Y = dto.Y; node.Width = dto.Width; node.Height = dto.Height; node.Name = dto.Name; node.GroupId = group;
            node.ShowDisplayText = false; node.DisplayText = ""; node.RequiredLabelFontFamily = "Kalam"; node.CustomFillColor = fill; node.CustomStrokeColor = stroke;
            node.CustomTextColor = text; node.NodeStrokeWidth = thickness; node.LabelFontSize = size;
            SetLabel(node, values["Label"]); SetTopBottomPorts(node, topBottom); return node;
        }
        catch { node.Dispose(); throw; }
    }
}
