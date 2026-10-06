using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Flowchart;
using Beep.SimpleDraw.Core.Library;
using SkiaSharp;

namespace Beep.SimpleDraw.Core;

/// <summary>One existing-scene history action; sketch snapshots reuse the shared Beep state contract.</summary>
public sealed class ComponentEditAction : DrawingAction
{
    public abstract record State
    {
        public abstract float X { get; }
        public abstract float Y { get; }
        public abstract float Width { get; }
        public abstract float Height { get; }
        public abstract float TextFontSize { get; }
        public abstract Guid GroupId { get; }
        public abstract void Apply(SkiaComponent component);
        public abstract State Resize(float x, float y, float width, float height, float fontSize);
        public static State Capture(SkiaComponent c) => c switch
        {
            SketchShape s => new SketchState(SketchEditAction.State.Capture(s)),
            FlowchartControl f when FlowchartDefinitions.Supports(f.GetType().FullName) => new FamilyState(FlowchartDefinitions.TypeKey(f), f.X, f.Y, f.Width, f.Height, f.Name, f.GroupId,
                FlowchartDefinitions.Label(f), f.CustomStrokeColor, f.CustomFillColor, f.CustomTextColor, f.NodeStrokeWidth, f.LabelFontSize, FlowchartDefinitions.TopBottomPorts(f)),
            _ when SkiaCatalog.Supports(c) => new CatalogState(System.Text.Json.JsonSerializer.Serialize(SkiaCatalog.Capture(c))),
            _ => throw new InvalidDataException("Unsupported editor component.")
        };
    }
    private sealed record SketchState(SketchEditAction.State Value) : State
    {
        public override float X => Value.X; public override float Y => Value.Y;
        public override float Width => Value.Width; public override float Height => Value.Height;
        public override float TextFontSize => Value.TextFontSize; public override Guid GroupId => Value.GroupId;
        public override void Apply(SkiaComponent c) => Value.Apply((SketchShape)c);
        public override State Resize(float x, float y, float width, float height, float fontSize) => new SketchState(Value with
        { X = x, Y = y, Width = width, Height = height, TextFontSize = fontSize });
    }
    private sealed record FamilyState(string Type, float Left, float Top, float W, float H, string Name, Guid Group,
        string Label, SKColor? Stroke, SKColor? Fill, SKColor? TextColor, float Border, float FontSize, bool TopBottom) : State
    {
        public override float X => Left; public override float Y => Top;
        public override float Width => W; public override float Height => H;
        public override float TextFontSize => FontSize; public override Guid GroupId => Group;
        public override void Apply(SkiaComponent c)
        {
            var node = (FlowchartControl)c;
            if (FlowchartDefinitions.TypeKey(node) != Type) throw new InvalidOperationException("History component type changed.");
            node.X = Left; node.Y = Top; node.Width = W; node.Height = H; node.Name = Name; node.GroupId = Group;
            node.CustomStrokeColor = Stroke; node.CustomFillColor = Fill; node.CustomTextColor = TextColor;
            node.NodeStrokeWidth = Border; node.LabelFontSize = FontSize;
            FlowchartDefinitions.SetLabel(node, Label); FlowchartDefinitions.SetTopBottomPorts(node, TopBottom);
        }
        public override State Resize(float x, float y, float width, float height, float fontSize) => this with { Left = x, Top = y, W = width, H = height, FontSize = fontSize };
    }
    private sealed record CatalogState(string Json) : State
    {
        private Beep.Skia.Serialization.ComponentDto Dto => System.Text.Json.JsonSerializer.Deserialize<Beep.Skia.Serialization.ComponentDto>(Json)!;
        public override float X => Dto.X; public override float Y => Dto.Y;
        public override float Width => Dto.Width; public override float Height => Dto.Height;
        public override float TextFontSize => 12;
        public override Guid GroupId => Dto.PropertyBag.TryGetValue("GroupId",out var group) ? Guid.Parse(group) : Guid.Empty;
        public override void Apply(SkiaComponent component) => SkiaCatalog.ApplyProperties(component,Dto);
        public override State Resize(float x,float y,float width,float height,float fontSize)
        {
            var dto = Dto; dto.X = x; dto.Y = y; dto.Width = width; dto.Height = height;
            return new CatalogState(System.Text.Json.JsonSerializer.Serialize(dto));
        }
    }
    private readonly SkiaComponent[] _components;
    private readonly State[] _before, _after;
    public ComponentEditAction(IEnumerable<SkiaComponent> components, IEnumerable<State> before)
    {
        _components = components.ToArray(); _before = before.ToArray();
        if (_components.Length != _before.Length) throw new ArgumentException("Each component requires its previous state.");
        _after = _components.Select(State.Capture).ToArray();
    }
    public override void Execute() { for (int i = 0; i < _components.Length; i++) _after[i].Apply(_components[i]); }
    public override void Undo() { for (int i = 0; i < _components.Length; i++) _before[i].Apply(_components[i]); }
}
