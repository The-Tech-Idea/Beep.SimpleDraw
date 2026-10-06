using Beep.Skia;
using Beep.Skia.Flowchart;
using Beep.Skia.Model;
using Beep.Skia.Serialization;
using SkiaSharp;

namespace Beep.SimpleDraw.Core.Library;

/// <summary>Reviewed static connector profile over the existing line DTO and renderer.</summary>
public static class ConnectorDefinitions
{
    public const int MaxLines = 4_000;
    public static LineDto Create(Guid start, Guid end, uint color, string? label = null, LineRoutingMode routing = LineRoutingMode.Straight)
    {
        var dto = new LineDto { StartPointId = start, EndPointId = end, LineColor = color,
            Label3 = label, DataTypeLabel = "", RoutingMode = (int)routing, ShowStartArrow = false, ShowEndArrow = true,
            Label3Placement = (int)LabelPlacement.Over, DataLabelPlacement = (int)LabelPlacement.Below, ArrowSize = 10,
            StatusColor = (uint)SKColors.DimGray, DataFlowSpeed = 100, DataFlowParticleSize = 4, DataFlowColor = (uint)SKColors.Blue };
        Validate(dto); return dto;
    }
    public static void ValidateLabel(string? label)
    {
        if (label is null || label.Length == 0) return;
        if (label.Length > 128 || label.Any(char.IsControl)) throw new InvalidDataException("A connector label permits up to 128 characters on one line.");
        SimpleDrawFonts.EnsureLoaded();
        using var font = new SKFont(TypefaceCache.GetRequired("Kalam"), 14);
        if (!font.ContainsGlyphs(label)) throw new InvalidDataException("The bundled font does not support a connector label character.");
    }
    public static void Validate(LineDto line)
    {
        if (line is null || line.StartPointId == Guid.Empty || line.EndPointId == Guid.Empty || line.StartPointId == line.EndPointId
            || line.StartComponentIndex != -1 || line.EndComponentIndex != -1
            || !Enum.IsDefined((LineRoutingMode)line.RoutingMode) || !Enum.IsDefined((LabelPlacement)line.Label1Placement)
            || !Enum.IsDefined((LabelPlacement)line.Label2Placement) || !Enum.IsDefined((LabelPlacement)line.Label3Placement)
            || line.DataLabelPlacement != (int)LabelPlacement.Below || line.DataTypeLabel != ""
            || !float.IsFinite(line.ArrowSize) || line.ArrowSize is < 4 or > 32
            || line.FlowDirection != 0 || line.DashPattern is not null || line.ShowStatusIndicator || line.Status != 0
            || line.StatusColor != (uint)SKColors.DimGray || line.IsAnimated || line.IsDataFlowAnimated
            || line.DataFlowSpeed != 100 || line.DataFlowParticleSize != 4 || line.DataFlowColor != (uint)SKColors.Blue
            || line.StartMultiplicity != 0 || line.EndMultiplicity != 0 || line.SchemaJson is not null || line.ExpectedSchemaJson is not null
            || line.GuardCondition is not null || line.TransitionAction is not null || line.TriggerEvent is not null)
            throw new InvalidDataException("The connector contains unsupported endpoints, styles, animation, schema, or execution metadata.");
        ValidateLabel(line.Label1); ValidateLabel(line.Label2); ValidateLabel(line.Label3);
    }
    public static LineDto Capture(IConnectionLine value)
    {
        if (value is not ConnectionLine line || line.GetType() != typeof(ConnectionLine)
            || line.Start?.Component is not SkiaComponent source || line.End?.Component is not SkiaComponent target
            || source == target || !source.OutConnectionPoints.Contains(line.Start) || !target.InConnectionPoints.Contains(line.End)
            || !line.IsConnected || line.Start.Connection != line.End || line.End.Connection != line.Start
            || line.Start.IsAvailable || line.End.IsAvailable || !line.IsVisible || line.RequiredLabelFontFamily != "Kalam"
            || line.SourceRowId is not null || line.TargetRowId is not null || line.AnimationStyle != FlowAnimationStyle.Dots
            || line.CrowFootSpreadDegrees != 18 || line.CrowFootLength != 1 || line.Paint is null
            || line.Paint.StrokeWidth != 2 || line.Paint.Style != SKPaintStyle.Stroke || !line.Paint.IsAntialias
            || line.Paint.StrokeCap != SKStrokeCap.Round || line.Paint.StrokeJoin != SKStrokeJoin.Miter
            || line.Paint.PathEffect is not null || line.Paint.Shader is not null || line.Paint.ColorFilter is not null
            || line.Paint.ImageFilter is not null || line.Paint.MaskFilter is not null || line.Paint.BlendMode != SKBlendMode.SrcOver)
            throw new InvalidDataException("This board contains an unsupported connector rendering or port configuration.");
        if (!SkiaCatalog.Supports(source) || !SkiaCatalog.Supports(target)) throw new InvalidDataException("Connector component is not in the catalog.");
        var dto = new LineDto
        {
            StartPointId = line.Start.Id, EndPointId = line.End.Id,
            ShowStartArrow = line.ShowStartArrow, ShowEndArrow = line.ShowEndArrow,
            Label1 = line.Label1, Label2 = line.Label2, Label3 = line.Label3, DataTypeLabel = line.DataTypeLabel,
            LineColor = (uint)line.LineColor, RoutingMode = (int)line.RoutingMode,
            Label1Placement = (int)line.Label1Placement, Label2Placement = (int)line.Label2Placement, Label3Placement = (int)line.Label3Placement,
            DataLabelPlacement = (int)line.DataLabelPlacement, ArrowSize = line.ArrowSize, DashPattern = line.DashPattern,
            FlowDirection = (int)line.FlowDirection, ShowStatusIndicator = line.ShowStatusIndicator, Status = (int)line.Status,
            StatusColor = (uint)line.StatusColor, IsAnimated = line.IsAnimated, IsDataFlowAnimated = line.IsDataFlowAnimated,
            DataFlowSpeed = line.DataFlowSpeed, DataFlowParticleSize = line.DataFlowParticleSize, DataFlowColor = (uint)line.DataFlowColor,
            StartMultiplicity = (int)line.StartMultiplicity, EndMultiplicity = (int)line.EndMultiplicity,
            SchemaJson = line.SchemaJson, ExpectedSchemaJson = line.ExpectedSchemaJson,
            GuardCondition = line.GuardCondition, TransitionAction = line.TransitionAction, TriggerEvent = line.TriggerEvent
        };
        Validate(dto); return dto;
    }
    public static List<ConnectionLine> Prepare(IReadOnlyList<LineDto> lines, IReadOnlyList<SkiaComponent> components)
    {
        if (lines is null || lines.Count > MaxLines) throw new InvalidDataException("The board permits at most 4,000 connectors.");
        var ports = components.Where(SkiaCatalog.Supports).SelectMany(n => n.InConnectionPoints.Concat(n.OutConnectionPoints)).DistinctBy(p => p.Id).ToDictionary(p => p.Id);
        var occupied = new HashSet<Guid>(); var result = new List<ConnectionLine>();
        try
        {
            foreach (var dto in lines)
            {
                Validate(dto);
                if (!ports.TryGetValue(dto.StartPointId, out var start) || !ports.TryGetValue(dto.EndPointId, out var end)
                    || start.Component == end.Component || start.Component is not SkiaComponent source || end.Component is not SkiaComponent target
                    || !source.OutConnectionPoints.Contains(start) || !target.InConnectionPoints.Contains(end)
                    || !occupied.Add(start.Id) || !occupied.Add(end.Id))
                    throw new InvalidDataException("Connectors need distinct unused output and input ports on different reviewed workflow nodes.");
                source.RefreshPortLayout(); target.RefreshPortLayout();
                result.Add(new ConnectionLine(start, end, () => { })
                {
                    RequiredLabelFontFamily = "Kalam", DataTypeLabel = "", Label1 = dto.Label1, Label2 = dto.Label2, Label3 = dto.Label3,
                    LineColor = new SKColor(dto.LineColor), RoutingMode = (LineRoutingMode)dto.RoutingMode,
                    Label1Placement = (LabelPlacement)dto.Label1Placement, Label2Placement = (LabelPlacement)dto.Label2Placement,
                    Label3Placement = (LabelPlacement)dto.Label3Placement, ShowStartArrow = dto.ShowStartArrow,
                    ShowEndArrow = dto.ShowEndArrow, ArrowSize = dto.ArrowSize
                });
            }
            return result;
        }
        catch { foreach (var line in result) line.Dispose(); throw; }
    }
}
