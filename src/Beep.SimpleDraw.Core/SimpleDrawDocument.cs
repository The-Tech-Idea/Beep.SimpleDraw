using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Serialization;
using Beep.Skia.Flowchart;
using Beep.SimpleDraw.Core.Library;

namespace Beep.SimpleDraw.Core;

/// <summary>Strict browser document boundary over the existing Beep DiagramDto.</summary>
public static class SimpleDrawDocument
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public const int MaxShapes = 2_000;
    public const int CurrentVersion = 7;
    private const string ShapeType = "Beep.Skia.Components.SketchShape";
    private static readonly string[] V2Keys = ["Kind", "Stroke", "Fill", "StrokeWidth", "PathData"];
    private static readonly string[] CurrentKeys = [.. V2Keys, "DisplayText", "TextFontSize", "GroupId", "Roughness", "SketchSeed"];
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, WriteIndented = true, NewLine = "\n"
    };
    private sealed record Envelope(string Format, int Version, DiagramDto Diagram);

    public static string Serialize(DrawingManager manager)
    {
        // Reject invalid live geometry before the general Beep serializer sanitizes coordinates.
        foreach (var c in manager.GetComponents())
            if (!(c is SketchShape || SkiaCatalog.Supports(c)) || !Bounds(c.X, c.Y, c.Width, c.Height))
                throw new InvalidDataException("This board contains an unsupported component or invalid bounds.");
        var diagram = manager.ToDto();
        diagram.Lines = manager.GetLines().Select(ConnectorDefinitions.Capture).ToList();
        var live = manager.GetComponents();
        for (int i = 0; i < diagram.Components.Count; i++)
            if (live[i] is FlowchartControl node && FlowchartDefinitions.Supports(node.GetType().FullName)) diagram.Components[i] = FlowchartDefinitions.Capture(node);
            else if (live[i] is SketchShape) diagram.Components[i].Type = ShapeType;
            else diagram.Components[i] = SkiaCatalog.Capture(live[i]);
        return Serialize(diagram);
    }

    public static string Serialize(DiagramDto diagram)
    {
        Validate(diagram);
        var json = JsonSerializer.Serialize(new Envelope("beep.simpledraw", CurrentVersion, diagram), Options);
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Document exceeds the 2 MiB limit.");
        return json;
    }

    public static DiagramDto Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json) > MaxBytes)
            throw new InvalidDataException("Choose a non-empty SimpleDraw file smaller than 2 MiB.");
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Options) ?? throw new InvalidDataException("Document is empty.");
            if (envelope.Format != "beep.simpledraw" || envelope.Version is < 1 or > CurrentVersion)
                throw new InvalidDataException("Expected a SimpleDraw version 1 through 7 file.");
            if (envelope.Version < CurrentVersion) Migrate(envelope.Diagram, envelope.Version);
            Validate(envelope.Diagram); return envelope.Diagram;
        }
        catch (JsonException ex) { throw new InvalidDataException("The file is not a valid SimpleDraw document.", ex); }
    }

    private static bool Keys(ComponentDto c, IEnumerable<string> keys) =>
        c.PropertyBag is not null && new HashSet<string>(keys, StringComparer.Ordinal).SetEquals(c.PropertyBag.Keys);
    private static void Migrate(DiagramDto? diagram, int version)
    {
        if (version == 6) return; // Existing native graph schema is preserved.
        if (version == 5)
        {
            if (diagram?.Lines is null || diagram.Lines.Count != 0) throw new InvalidDataException("Version 5 files cannot contain connectors.");
            return;
        }
        if (diagram?.Components is null || diagram.Lines is null || diagram.Lines.Count != 0 || diagram.Components.Any(c=>c is null || c.Type != ShapeType))
            throw new InvalidDataException("Earlier SimpleDraw versions permit sketch components only.");
        if (version == 4) return; // Same sketch schema and portable metrics; preserve identities exactly.
        if (version == 3)
        {
            // Early text documents used host-rounded metrics. Preserve identity/content and normalize to portable metrics.
            foreach (var shape in Prepare(diagram, true)) shape.Dispose();
            return;
        }
        if (diagram?.SchemaVersion != 2 || diagram.Components is null) throw new InvalidDataException("Invalid earlier diagram schema.");
        foreach (var c in diagram.Components)
        {
            if (c is null || c.Id != Guid.Empty || !Keys(c, version == 1 ? V2Keys[..4] : V2Keys)
                || !c.PropertyBag.TryGetValue("Kind", out var kind)
                || (version == 1 ? kind is not ("Rectangle" or "Ellipse" or "Diamond")
                    : kind is not ("Rectangle" or "Ellipse" or "Diamond" or "Line" or "Arrow" or "Freehand")))
                throw new InvalidDataException("Invalid earlier SimpleDraw shape.");
            if (version == 1) c.PropertyBag.Add("PathData", "");
            c.Id = Guid.NewGuid();
            c.PropertyBag.Add("DisplayText", ""); c.PropertyBag.Add("TextFontSize", "24");
            c.PropertyBag.Add("GroupId", Guid.Empty.ToString("D")); c.PropertyBag.Add("Roughness", "0");
            c.PropertyBag.Add("SketchSeed", (c.Id.GetHashCode() & int.MaxValue).ToString(CultureInfo.InvariantCulture));
        }
        diagram.SchemaVersion = DiagramDto.CurrentSchemaVersion;
    }
    private static void Validate(DiagramDto? diagram) { foreach (var shape in Prepare(diagram)) shape.Dispose(); }

    public static void Apply(DrawingManager manager, DiagramDto diagram)
    {
        var lines = new List<ConnectionLine>(); var prepared = Prepare(diagram, preparedLines: lines);
        var discarded = manager.GetComponents().ToArray(); var discardedLines = manager.GetLines().OfType<ConnectionLine>().ToArray();
        manager.ClearComponents(); manager.HistoryManager.Clear();
        foreach (var shape in prepared) manager.AddComponent(shape);
        foreach (var line in lines)
            if (!manager.ConnectComponents(line)) throw new InvalidOperationException("Validated connector could not be attached.");
        manager.HistoryManager.Clear();
        foreach (var line in discardedLines) line.Dispose();
        foreach (var shape in discarded) shape.Dispose();
    }

    internal static DiagramDto SelectionSubset(DiagramDto diagram, IReadOnlySet<Guid> selected)
    {
        var components = diagram.Components.Where(c => selected.Contains(c.Id)).ToList();
        var ports = components.SelectMany(c => c.InPointIds.Concat(c.OutPointIds)).ToHashSet();
        return new DiagramDto { SchemaVersion = diagram.SchemaVersion, ThemeName = diagram.ThemeName, Components = components,
            Lines = diagram.Lines.Where(l => ports.Contains(l.StartPointId) && ports.Contains(l.EndPointId)).ToList() };
    }

    internal static List<SkiaComponent> Prepare(DiagramDto? diagram, bool migrateTextBounds = false, List<ConnectionLine>? preparedLines = null)
    {
        if (diagram is null || diagram.SchemaVersion != DiagramDto.CurrentSchemaVersion || diagram.Components is null
            || diagram.Lines is null || diagram.Lines.Count > ConnectorDefinitions.MaxLines || diagram.Components.Count > MaxShapes)
            throw new InvalidDataException("Unsupported diagram schema, connections, or shape count.");
        var result = new List<SkiaComponent>(); var ids = new HashSet<Guid>();
        try
        {
            foreach (var c in diagram.Components)
            {
                if (c is null || c.Id == Guid.Empty || !ids.Add(c.Id)) throw new InvalidDataException("Invalid or duplicate component identity.");
                if (FlowchartDefinitions.Supports(c.Type))
                {
                    var node = FlowchartDefinitions.Prepare(c);
                    try
                    {
                        foreach (var id in c.InPointIds.Concat(c.OutPointIds))
                            if (!ids.Add(id)) throw new InvalidDataException("Component and port identities must be globally distinct.");
                        result.Add(node); node = null!;
                    }
                    finally { node?.Dispose(); }
                    continue;
                }
                if (SkiaCatalog.Find(c.Type) is not null)
                {
                    var node = SkiaCatalog.Prepare(c);
                    try
                    {
                        foreach (var id in c.InPointIds.Concat(c.OutPointIds).Distinct())
                            if (!ids.Add(id)) throw new InvalidDataException("Component and physical port identities must be globally distinct.");
                        result.Add(node); node = null!;
                    }
                    finally { node?.Dispose(); }
                    continue;
                }
                if (c.Type != ShapeType || !Keys(c, CurrentKeys)
                    || c.TypedPropertyBag is null || c.TypedPropertyBag.Count != 0 || c.InPointIds is null || c.OutPointIds is null
                    || c.InPointIds.Count != 0 || c.OutPointIds.Count != 0 || c.Name is null || c.Name.Length > 120
                    || !Bounds(c.X, c.Y, c.Width, c.Height))
                    throw new InvalidDataException("A shape has unsupported identity, type, properties, name, or bounds.");
                var bag = c.PropertyBag;
                if (bag.Any(p => p.Value is null || p.Value.Length > (p.Key == "PathData" ? 120_000 : p.Key == "DisplayText" ? SketchShape.MaxTextLength : 36))
                    || !Enum.TryParse<SketchShapeKind>(bag["Kind"], out var kind) || !Enum.IsDefined(kind) || kind.ToString() != bag["Kind"]
                    || !Number(bag["StrokeWidth"], out var width) || !Number(bag["TextFontSize"], out var size)
                    || !Number(bag["Roughness"], out var roughness) || !int.TryParse(bag["SketchSeed"], NumberStyles.None, CultureInfo.InvariantCulture, out var seed)
                    || !Guid.TryParseExact(bag["GroupId"], "D", out var group))
                    throw new InvalidDataException("Invalid sketch geometry, style, or group properties.");
                SketchShape? shape = null;
                try
                {
                    shape = new SketchShape(c.Id) { Kind = kind, X = c.X, Y = c.Y, Width = c.Width, Height = c.Height, Name = c.Name,
                        Stroke = bag["Stroke"], Fill = bag["Fill"], StrokeWidth = width, PathData = bag["PathData"],
                        DisplayText = bag["DisplayText"], TextFontSize = size, GroupId = group, Roughness = roughness, SketchSeed = seed };
                    if (shape.IsPath ? shape.PathPoints.Distinct().Take(2).Count() < 2 || (kind != SketchShapeKind.Freehand && shape.PathPoints.Count != 2)
                        : shape.PathPoints.Count != 0)
                        throw new InvalidDataException("Geometry does not match the shape kind.");
                    if (kind == SketchShapeKind.Text)
                    {
                        if (string.IsNullOrWhiteSpace(shape.DisplayText)) throw new InvalidDataException("Text objects cannot be empty.");
                        SimpleDrawFonts.EnsureLoaded(); var measured = shape.MeasureText();
                        if (migrateTextBounds)
                        {
                            c.Width = shape.Width = measured.Width; c.Height = shape.Height = measured.Height;
                            if (!Bounds(c.X, c.Y, c.Width, c.Height)) throw new InvalidDataException("Migrated text would exceed the board bounds.");
                        }
                        if (Math.Abs(measured.Width - shape.Width) > .1f || Math.Abs(measured.Height - shape.Height) > .1f)
                            throw new InvalidDataException("Text bounds do not match the bundled font metrics.");
                    }
                    else if (shape.DisplayText.Length != 0) throw new InvalidDataException("Only text objects may contain text.");
                    result.Add(shape); shape = null;
                }
                catch (ArgumentException ex) { throw new InvalidDataException("The shape contains invalid geometry, text, color, or stroke width.", ex); }
                finally { shape?.Dispose(); }
            }
            foreach (var group in result.Where(s => s.GroupId != Guid.Empty).GroupBy(s => s.GroupId))
                if (group.Count() < 2 || ids.Contains(group.Key)) throw new InvalidDataException("A group needs at least two members and a distinct identity.");
            var lines = ConnectorDefinitions.Prepare(diagram.Lines, result);
            if (preparedLines is null) foreach (var line in lines) line.Dispose();
            else preparedLines.AddRange(lines);
            return result;
        }
        catch { foreach (var shape in result) shape.Dispose(); throw; }
    }
    private static bool Number(string value, out float number) => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && float.IsFinite(number);
    private static bool Coordinate(float value) => float.IsFinite(value) && Math.Abs(value) <= 100_000;
    public static bool Bounds(float x, float y, float width, float height) => Coordinate(x) && Coordinate(y) && Coordinate(width) && Coordinate(height)
        && width > 0 && height > 0 && Coordinate(x + width) && Coordinate(y + height);
}
