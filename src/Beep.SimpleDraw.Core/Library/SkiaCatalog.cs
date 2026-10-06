using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Beep.Skia;
using Beep.Skia.Serialization;
using SkiaSharp;

namespace Beep.SimpleDraw.Core.Library;

public sealed record CatalogEntry(string Family, string Label, string TypeKey, Func<SkiaComponent> Factory,
    IReadOnlyDictionary<string, PropertyInfo> Properties);

/// <summary>Fixed compiled Beep factories; document input never selects assemblies or CLR types.</summary>
public static partial class SkiaCatalog
{
    private static CatalogEntry Entry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string family, string label, Func<SkiaComponent> factory)
        where T : SkiaComponent => new(family, label, typeof(T).FullName!, factory,
            typeof(T).GetProperties().Where(p => p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
                .GroupBy(p => p.Name,StringComparer.Ordinal).ToDictionary(g => g.Key,g => g.First(),StringComparer.Ordinal));
    private static class Lookup
    {
        internal static readonly IReadOnlyDictionary<string, CatalogEntry> ByType =
            Entries.ToDictionary(entry => entry.TypeKey, StringComparer.Ordinal);
    }
    public static CatalogEntry? Find(string? key) => key is not null && Lookup.ByType.TryGetValue(key, out var entry) ? entry : null;
    public static bool Supports(SkiaComponent node) => Find(node.GetType().FullName) is not null;
    private static readonly HashSet<string> Excluded = ["Id", "Name", "X", "Y", "Width", "Height", "IsVisible", "IsEnabled", "IsStatic", "Opacity", "GroupId"];

    public static SkiaComponent Create(string type)
    {
        var entry = Find(type) ?? throw new InvalidDataException("Unknown Beep.Skia catalog component.");
        SimpleDrawFonts.EnsureLoaded();
        var node = entry.Factory();
        node.Name ??= entry.Label;
        if (node is Beep.Skia.UML.UMLControl) node.ShowDisplayText = false;
        return node;
    }
    public static ComponentDto Capture(SkiaComponent node)
    {
        var entry = Find(node.GetType().FullName) ?? throw new InvalidDataException("Component is not in the compiled Beep.Skia catalog.");
        if (!SimpleDrawDocument.Bounds(node.X,node.Y,node.Width,node.Height) || node.Children.Count != 0 || node.IsStatic || !node.IsVisible || !node.IsEnabled || node.Opacity != 1)
            throw new InvalidDataException("Unsupported catalog component geometry or presentation.");
        var dto = new ComponentDto { Id = node.Id, Type = entry.TypeKey, Name = node.Name ?? entry.Label,
            X = node.X, Y = node.Y, Width = node.Width, Height = node.Height,
            InPointIds = node.InConnectionPoints.Select(p => p.Id).ToList(), OutPointIds = node.OutConnectionPoints.Select(p => p.Id).ToList() };
        foreach (var pair in node.GetProperties())
        {
            if (Excluded.Contains(pair.Key)) continue;
            var value = entry.Properties.TryGetValue(pair.Key, out var property) ? property.GetValue(node) : pair.Value;
            if (value is null && property?.PropertyType == typeof(string)) value = "";
            if (value is string or bool or Enum or SKColor or IConvertible)
                dto.PropertyBag[pair.Key] = value is SKColor color ? color.ToString() : Convert.ToString(value,CultureInfo.InvariantCulture)!;
            else dto.TypedPropertyBag[pair.Key] = JsonSerializer.SerializeToElement(value,value?.GetType() ?? typeof(object));
        }
        dto.PropertyBag["GroupId"] = node.GroupId.ToString("D");
        return dto;
    }
    public static SkiaComponent Prepare(ComponentDto dto)
    {
        if (dto is null || dto.Id == Guid.Empty || dto.Name is null || dto.Name.Length > 120 || dto.Name.Any(char.IsControl)
            || !SimpleDrawDocument.Bounds(dto.X,dto.Y,dto.Width,dto.Height) || dto.PropertyBag is null || dto.TypedPropertyBag is null
            || dto.InPointIds is null || dto.OutPointIds is null || dto.InPointIds.Count > 64 || dto.OutPointIds.Count > 64)
            throw new InvalidDataException("Invalid catalog identity, properties or geometry.");
        var node = Create(dto.Type);
        try
        {
            var expected = Capture(node);
            if (!expected.PropertyBag.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(dto.PropertyBag.Keys)
                || !expected.TypedPropertyBag.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(dto.TypedPropertyBag.Keys)
                || dto.PropertyBag.Any(p => p.Value is null || p.Value.Length > 16384)
                || dto.TypedPropertyBag.Any(p => p.Value.GetRawText().Length > 120000))
                throw new InvalidDataException($"Unsupported Beep.Skia property keys or value size: {dto.Type}; strings added {string.Join(',',dto.PropertyBag.Keys.Except(expected.PropertyBag.Keys))}, missing {string.Join(',',expected.PropertyBag.Keys.Except(dto.PropertyBag.Keys))}; typed added {string.Join(',',dto.TypedPropertyBag.Keys.Except(expected.TypedPropertyBag.Keys))}, missing {string.Join(',',expected.TypedPropertyBag.Keys.Except(dto.TypedPropertyBag.Keys))}.");
            ApplyProperties(node,dto);
            node.RestorePersistentIdentity(dto.Id,dto.InPointIds,dto.OutPointIds);
            var captured = Capture(node);
            if (!dto.PropertyBag.All(p => captured.PropertyBag.TryGetValue(p.Key,out var value) && value == p.Value)
                || !dto.TypedPropertyBag.All(p => captured.TypedPropertyBag.TryGetValue(p.Key,out var value) && value.GetRawText() == p.Value.GetRawText()))
                throw new InvalidDataException($"A catalog property did not roundtrip exactly: {dto.Type}; {string.Join(';',dto.PropertyBag.Where(p => !captured.PropertyBag.TryGetValue(p.Key,out var value) || value != p.Value).Select(p => p.Key+"="+p.Value+" -> "+captured.PropertyBag.GetValueOrDefault(p.Key)))}; typed {string.Join(',',dto.TypedPropertyBag.Where(p => !captured.TypedPropertyBag.TryGetValue(p.Key,out var value) || value.GetRawText()!=p.Value.GetRawText()).Select(p => p.Key))}.");
            return node;
        }
        catch { node.Dispose(); throw; }
    }
    public static void ApplyProperties(SkiaComponent node, ComponentDto dto)
    {
        var entry = Find(dto.Type) ?? throw new InvalidDataException("Unknown catalog component.");
        foreach (var key in dto.PropertyBag.Keys.Concat(dto.TypedPropertyBag.Keys))
        {
            object? value;
            if (entry.Properties.TryGetValue(key,out var property))
            {
                if (dto.TypedPropertyBag.TryGetValue(key,out var json)) value = json.Deserialize(property.PropertyType);
                else value = ConvertValue(dto.PropertyBag[key],property.PropertyType);
                property.SetValue(node,value);
            }
            else
            {
                var info = node.NodeProperties[key];
                var target = info.ParameterType ?? info.ParameterCurrentValue?.GetType() ?? typeof(string);
                value = dto.TypedPropertyBag.TryGetValue(key,out var json) ? json.Deserialize(target) : ConvertValue(dto.PropertyBag[key],target);
                info.ParameterCurrentValue = value;
            }
        }
        node.Name = dto.Name; node.X = dto.X; node.Y = dto.Y; node.Width = dto.Width; node.Height = dto.Height;

    }
    private static object? ConvertValue(string text, Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) return text;
        if (type == typeof(SKColor)) return SKColor.Parse(text);
        if (type == typeof(Guid)) return Guid.ParseExact(text,"D");
        if (type == typeof(TimeSpan)) return TimeSpan.Parse(text,CultureInfo.InvariantCulture);
        if (type.IsEnum) { var value = Enum.Parse(type,text); if (!Enum.IsDefined(type,value)) throw new InvalidDataException("Undefined catalog enum."); return value; }
        var result = Convert.ChangeType(text,type,CultureInfo.InvariantCulture);
        if (result is float f && !float.IsFinite(f) || result is double d && !double.IsFinite(d)) throw new InvalidDataException("Non-finite catalog property.");
        return result;
    }
    public static string Label(SkiaComponent node) => node.Name ?? Find(node.GetType().FullName)?.Label ?? node.GetType().Name;
    public static string? VisualKey(SkiaComponent node, string kind)
    {
        var bag = Capture(node).PropertyBag;
        string[] candidates = kind switch
        {
            "stroke" => ["BorderColor","StrokeColor","CustomStrokeColor","Color"],
            "fill" => ["BackgroundColor","FillColor","CustomFillColor","NodeColor"],
            "width" => ["BorderThickness","StrokeWidth","NodeStrokeWidth","Thickness"],
            "font" => ["LabelFontSize","FontSize","TextFontSize"], _ => []
        };
        return candidates.FirstOrDefault(bag.ContainsKey);
    }
    public static ComponentDto NormalizeEdit(ComponentDto dto)
    {
        using var candidate = Create(dto.Type);
        ApplyProperties(candidate,dto);
        candidate.RestorePersistentIdentity(dto.Id,dto.InPointIds,dto.OutPointIds);
        var normalized = Capture(candidate);
        using var checkedNode = Prepare(normalized);
        return normalized;
    }
}
