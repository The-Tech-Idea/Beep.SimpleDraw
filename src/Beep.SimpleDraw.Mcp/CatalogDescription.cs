using System.Reflection;
using System.Text.Json;
using Beep.Skia;
using Beep.SimpleDraw.Core.Library;
using SkiaSharp;

namespace Beep.SimpleDraw.Mcp;

public static class CatalogDescription
{
    public static object List(string? family, string? search, int offset, int limit)
    {
        if (offset < 0 || limit is < 1 or > 100) throw new InvalidDataException("Use offset >= 0 and limit 1 through 100.");
        var entries=SkiaCatalog.Entries.Where(e=>(family is null || e.Family.Equals(family,StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(search) || (e.Label+" "+e.TypeKey).Contains(search,StringComparison.OrdinalIgnoreCase))).ToArray();
        return new {total=entries.Length,offset,limit,components=entries.Skip(offset).Take(limit).Select(e=>new {e.Family,e.Label,type=e.TypeKey}),families=SkiaCatalog.Entries.Select(e=>e.Family).Distinct().ToArray(),sketchKinds=Enum.GetNames<Beep.Skia.Components.SketchShapeKind>()};
    }
    public static object Describe(string type)
    {
        var entry=SkiaCatalog.Find(type) ?? throw new InvalidDataException("Unknown component type. Use list_components to get exact type keys.");
        using var node=SkiaCatalog.Create(type);
        if(node is Beep.Skia.Flowchart.FlowchartControl flow && FlowchartDefinitions.Supports(type)) { flow.ShowDisplayText=false;flow.RequiredLabelFontFamily="Kalam"; }
        var dto=node is Beep.Skia.Flowchart.FlowchartControl f && FlowchartDefinitions.Supports(type) ? FlowchartDefinitions.Capture(f) : SkiaCatalog.Capture(node);
        var properties=dto.PropertyBag.Keys.Concat(dto.TypedPropertyBag.Keys).Where(k=>k!="GroupId").Select(key=>{
            var propType=entry.Properties.TryGetValue(key,out var p) ? p.PropertyType : node.NodeProperties.TryGetValue(key,out var info) ? info.ParameterType ?? info.ParameterCurrentValue?.GetType() ?? typeof(string) : typeof(string);
            object value = dto.PropertyBag.TryGetValue(key,out var scalar) ? scalar : dto.TypedPropertyBag[key];
            return new {name=key,encoding=dto.PropertyBag.ContainsKey(key)?"invariant-string":"json",clrType=propType.FullName,defaultValue=value,schema=Schema(propType,0,[],dto.TypedPropertyBag.ContainsKey(key)),description=node.NodeProperties.GetValueOrDefault(key)?.Description,choices=node.NodeProperties.GetValueOrDefault(key)?.Choices,jsonBacked=value is string text && (text.TrimStart().StartsWith("[") || text.TrimStart().StartsWith("{")),editable=!(FlowchartDefinitions.Supports(type)&&key is "InPortCount" or "OutPortCount")};
        }).ToArray();
        return new {entry.Family,entry.Label,type,defaultBounds=new {node.X,node.Y,node.Width,node.Height},properties,
            ports=new {inputs=node.InConnectionPoints.Select((p,i)=>new {index=i,p.DataType,p.IsAvailable,p.Type}),outputs=node.OutConnectionPoints.Select((p,i)=>new {index=i,p.DataType,p.IsAvailable,p.Type})},
            geometryProperties=new[]{"X","Y","Width","Height"},identityProperties=new[]{"Id","Name","GroupId","InPointIds","OutPointIds"},
            runtimeProperties=entry.Properties.Where(p=>!dto.PropertyBag.ContainsKey(p.Key)&&!dto.TypedPropertyBag.ContainsKey(p.Key)).Select(p=>new {name=p.Key,clrType=p.Value.PropertyType.FullName,editable=false,reason="Runtime property outside the current portable document contract."}),
            notes="Set serialized component properties with set_component_property. Use move_elements/resize_elements for bounds, group_elements for groups, and connect_nodes for port links. CLR runtime infrastructure is not editable document data. Server validation is authoritative."};
    }
    private static object Schema(Type type,int depth,HashSet<Type> seen,bool json=true)
    {
        var nullable=Nullable.GetUnderlyingType(type); if(nullable is not null) return new {anyOf=new object[]{Schema(nullable,depth,seen,json),new {type="null"}}};
        if(type==typeof(string)||type==typeof(char)) return new {type="string"};
        if(type==typeof(bool)) return new {type="boolean"};
        if(type==typeof(Guid)) return new {type="string",format="uuid"};
        if(type==typeof(SKColor)) return new {type="string",description="Skia color, #RRGGBB or #AARRGGBB."};
        if(type.IsEnum) return json ? (object)new {type="integer",@enum=Enum.GetValues(type).Cast<object>().Select(v=>Convert.ToInt64(v)).ToArray(),names=Enum.GetNames(type)} : new {type="string",@enum=Enum.GetNames(type)};
        if(type==typeof(float)||type==typeof(double)||type==typeof(decimal)) return new {type="number"};
        if(type.IsPrimitive) return new {type="integer"};
        if(depth>=6||seen.Contains(type)) return new {description="Nested native value; see defaultValue for its serialized structure.",clrType=type.FullName};
        var next=new HashSet<Type>(seen){type};
        if(type.IsArray) return new {type="array",items=Schema(type.GetElementType()!,depth+1,next)};
        var enumerable=type.GetInterfaces().Append(type).FirstOrDefault(i=>i.IsGenericType && i.GetGenericTypeDefinition()==typeof(IEnumerable<>));
        if(enumerable is not null) return new {type="array",items=Schema(enumerable.GetGenericArguments()[0],depth+1,next)};
        return new {type="object",properties=type.GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(p=>p.GetIndexParameters().Length==0&&p.GetMethod?.IsPublic==true).ToDictionary(p=>p.Name,p=>Schema(p.PropertyType,depth+1,next))};
    }
}
