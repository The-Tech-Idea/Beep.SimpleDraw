using Beep.Skia;
using Beep.Skia.Components;
using SkiaSharp;
using Beep.Skia.Flowchart;
using Beep.SimpleDraw.Core.Library;

namespace Beep.SimpleDraw.Core;

public enum DrawingTool { Select, Rectangle, Ellipse, Diamond, Hand, Line, Arrow, Freehand, Eraser, Text }

/// <summary>Tool gestures on one Beep.Skia scene. History remains owned by DrawingManager.</summary>
public sealed class DrawingSession
{
    private DrawingTool _tool;
    private SKPoint _startScreen, _startWorld, _startPan, _lastWorld;
    private readonly Dictionary<SkiaComponent, SKPoint> _moving = new();
    private readonly List<SKPoint> _path = new();
    private readonly HashSet<SkiaComponent> _erasing = new();
    private readonly Dictionary<SkiaComponent, ComponentEditAction.State> _resizing = new();
    private SKRect _resizeBounds;
    private int _endpoint = -1;
    private bool _resizeActive, _marqueeAdd;
    private bool _active, _panning;
    private bool _keyboardGesture, _pinching;
    private SKPoint _pinchWorld;
    private float _pinchZoom;
    public SKSize ViewportSize { get; private set; } = new(800, 600);
    public SKPoint KeyboardCursor { get; private set; } = new(400, 300);
    public bool HasKeyboardGesture => _keyboardGesture;
    public DrawingManager Manager { get; } = new() { ShowGrid = false, SnapToGrid = false, ShowSelectionHandles = false };
    public SketchShape? Draft { get; private set; }
    public DrawingTool Tool
    {
        get => _tool;
        set { CancelGesture(); _tool = value; }
    }
    public string Stroke { get; set; } = "#334155";
    public string Fill { get; set; } = "#00000000";
    public float StrokeWidth { get; set; } = 2;
    public float FontSize { get; set; } = 24;
    public float Roughness { get; set; }
    public SKPoint? TextAnchor { get; private set; }
    public SketchShape? TextTarget { get; private set; }
    public bool HasTextEdit => TextAnchor.HasValue;
    public SKRect? Marquee { get; private set; }
    public IEnumerable<SketchShape> SelectedShapes => Manager.SelectionManager.SelectedComponents.OfType<SketchShape>();
    public IEnumerable<SkiaComponent> SelectedComponents => Manager.SelectionManager.SelectedComponents;
    public SKRect? SelectionBounds => SelectedComponents.Any() ? new(SelectedComponents.Min(s => s.X), SelectedComponents.Min(s => s.Y),
        SelectedComponents.Max(s => s.X + s.Width), SelectedComponents.Max(s => s.Y + s.Height)) : null;
    public event Action? DocumentChanged;
    public bool HasGesture => _active || _pinching;
    public string? GestureNotice { get; private set; }
    public IReadOnlyCollection<SkiaComponent> ErasePreview => _erasing;
    public bool EditingSelection => Tool == DrawingTool.Select && Manager.SelectionManager.SelectedComponents.Any();
    public SKPoint ScreenToWorld(SKPoint point) => new(
        (point.X - Manager.PanOffset.X) / Manager.Zoom,
        (point.Y - Manager.PanOffset.Y) / Manager.Zoom);

    public void PointerDown(SKPoint screen, bool pan = false, bool additive = false)
    {
        CancelGesture();
        GestureNotice = null;
        if (!Finite(screen)) return;
        _startScreen = screen; _startWorld = ScreenToWorld(screen); _startPan = Manager.PanOffset;
        if (!WithinBoard(_startWorld)) return;
        _active = true; _panning = pan || Tool == DrawingTool.Hand;
        _lastWorld = _startWorld;
        if (_panning) return;
        if (Tool == DrawingTool.Text) { _active = false; BeginText(screen); return; }
        if (Tool == DrawingTool.Eraser) { CollectErase(_startWorld, _startWorld); return; }
        if (Tool == DrawingTool.Select)
        {
            if (!additive && StartResize(_startWorld)) return;
            var hit = Manager.GetComponents().Reverse().FirstOrDefault(s => s is SketchShape sketch ? sketch.ContainsPoint(_startWorld, 5 / Manager.Zoom) : s.ContainsPoint(_startWorld));
            if (additive && hit is not null && hit.IsSelected) { SelectGroup(hit, true); return; }
            if (hit is null)
            {
                if (!additive) Manager.SelectionManager.ClearSelection();
                _marqueeAdd = additive; Marquee = new(_startWorld.X, _startWorld.Y, _startWorld.X, _startWorld.Y); return;
            }
            if (!hit.IsSelected || additive) SelectGroup(hit, additive);
            if (hit is not null)
                foreach (var shape in SelectedComponents)
                    _moving.Add(shape, new(shape.X, shape.Y));
            return;
        }
        if (Manager.GetComponents().Count >= SimpleDrawDocument.MaxShapes)
        { _active = false; GestureNotice = $"This drawing has reached the {SimpleDrawDocument.MaxShapes} object limit."; return; }
        Draft = new SketchShape { X = _startWorld.X, Y = _startWorld.Y, Width = 0, Height = 0,
            Kind = Tool switch { DrawingTool.Ellipse => SketchShapeKind.Ellipse,
                DrawingTool.Diamond => SketchShapeKind.Diamond, DrawingTool.Line => SketchShapeKind.Line,
                DrawingTool.Arrow => SketchShapeKind.Arrow, DrawingTool.Freehand => SketchShapeKind.Freehand,
                _ => SketchShapeKind.Rectangle },
            Stroke = Stroke, Fill = Fill, StrokeWidth = StrokeWidth, Roughness = Roughness };
        if (Draft.IsPath) { _path.Add(_startWorld); Draft.SetWorldPath([_startWorld, _startWorld]); }
    }

    public void PointerMove(SKPoint screen)
    {
        if (!_active || !Finite(screen)) return;
        if (_panning) { Manager.PanOffset = _startPan + screen - _startScreen; return; }
        var world = ScreenToWorld(screen);
        if (!WithinBoard(world)) return;
        if (_resizeActive) { PreviewResize(world); return; }
        if (Marquee.HasValue)
        { Marquee = new(Math.Min(_startWorld.X, world.X), Math.Min(_startWorld.Y, world.Y), Math.Max(_startWorld.X, world.X), Math.Max(_startWorld.Y, world.Y)); return; }
        if (Tool == DrawingTool.Eraser) { CollectErase(_lastWorld, world); _lastWorld = world; return; }
        if (Draft is not null)
        {
            if (Draft.IsPath)
            {
                if (Draft.Kind == SketchShapeKind.Freehand)
                {
                    if ((world - _path[^1]).Length * Manager.Zoom >= 1)
                    {
                        if (_path.Count < SketchShape.MaxPathPoints) _path.Add(world);
                        else GestureNotice = "Stroke reached 4096 points. Start another stroke to continue.";
                    }
                    if (_path.Count >= 2) Draft.SetWorldPath(_path);
                }
                else Draft.SetWorldPath([_startWorld, world]);
            }
            else
            {
                Draft.X = Math.Min(world.X, _startWorld.X); Draft.Y = Math.Min(world.Y, _startWorld.Y);
                Draft.Width = Math.Abs(world.X - _startWorld.X); Draft.Height = Math.Abs(world.Y - _startWorld.Y);
            }
        }
        if (_moving.Count > 0)
        {
            var delta = world - _startWorld;
            if (_moving.All(p => WithinBoard(p.Value + delta) && WithinBoard(p.Value + delta + new SKPoint(p.Key.Width, p.Key.Height))))
                foreach (var (shape, origin) in _moving) { shape.X = origin.X + delta.X; shape.Y = origin.Y + delta.Y; }
        }
    }

    public void PointerUp(SKPoint screen)
    {
        if (!_active) return;
        PointerMove(screen);
        bool committed = false;
        if (_resizeActive)
        {
            var shapes = _resizing.Keys.ToArray(); var before = _resizing.Values.ToArray();
            if (shapes.Where((s, i) => ComponentEditAction.State.Capture(s) != before[i]).Any())
            { Manager.HistoryManager.ExecuteAction(new ComponentEditAction(shapes, before)); committed = true; }
        }
        if (Marquee.HasValue)
        {
            var rect = Marquee.Value;
            if (rect.Width * Manager.Zoom >= 4 || rect.Height * Manager.Zoom >= 4)
            {
                Manager.SelectionManager.SelectComponentsInRect(rect, Manager.GetComponents(), _marqueeAdd);
                foreach (var shape in SelectedComponents.ToArray()) SelectGroup(shape, true, false);
            }
        }
        if (Draft is not null)
        {
            if (Manager.GetComponents().Count < SimpleDrawDocument.MaxShapes &&
                (Draft.IsPath ? Math.Max(Draft.Width, Draft.Height) * Manager.Zoom >= 4 : Draft.Width * Manager.Zoom >= 4 && Draft.Height * Manager.Zoom >= 4))
            {
                Draft.Name = $"{Draft.Kind} {Manager.GetComponents().Count + 1}";
                Manager.AddComponent(Draft);
                Manager.SelectionManager.SelectComponent(Draft);
                committed = true;
            }
            else Draft.Dispose();
            Draft = null;
        }
        if (_moving.Count > 0)
        {
            var first = _moving.First(); var delta = new SKPoint(first.Key.X, first.Key.Y) - first.Value;
            if (delta != SKPoint.Empty)
            {
                Manager.HistoryManager.ExecuteAction(new MoveComponentsAction(Manager, _moving.Keys.Cast<Beep.Skia.SkiaComponent>().ToList(), delta));
                committed = true;
            }
        }
        if (_erasing.Count > 0)
        {
            Manager.SelectionManager.ClearSelection();
            foreach (var shape in _erasing) Manager.SelectionManager.SelectComponent(shape, true);
            Manager.DeleteSelectedComponents(); committed = true;
        }
        _moving.Clear(); _path.Clear(); _erasing.Clear(); _resizing.Clear(); Marquee = null;
        _resizeActive = false; _endpoint = -1; _active = false; _panning = false;
        if (committed) DocumentChanged?.Invoke();
    }

    public void CancelGesture()
    {
        Draft?.Dispose(); Draft = null;
        foreach (var (shape, state) in _resizing) state.Apply(shape);
        foreach (var (shape, origin) in _moving) { shape.X = origin.X; shape.Y = origin.Y; }
        _moving.Clear(); _path.Clear(); _erasing.Clear(); _resizing.Clear(); Marquee = null;
        TextAnchor = null; TextTarget = null;
        _resizeActive = false; _endpoint = -1; _active = false; _panning = false;
        _keyboardGesture = false; _pinching = false;
    }

    private void CollectErase(SKPoint from, SKPoint to)
    {
        foreach (var shape in Manager.GetComponents())
            if (shape is SketchShape sketch ? sketch.IntersectsSegment(from, to, 5 / Manager.Zoom) : SegmentHitsBounds(from,to,shape.Bounds,5/Manager.Zoom))
                foreach (var member in Members(shape)) _erasing.Add(member);
    }
    private static bool SegmentHitsBounds(SKPoint from, SKPoint to, SKRect bounds, float tolerance)
    {
        bounds.Inflate(tolerance,tolerance); float enter = 0, exit = 1; var delta = to-from;
        bool Clip(float p, float q)
        {
            if (p == 0) return q >= 0;
            var t = q/p;
            if (p < 0) { if (t > exit) return false; enter = Math.Max(enter,t); }
            else { if (t < enter) return false; exit = Math.Min(exit,t); }
            return true;
        }
        return Clip(-delta.X,from.X-bounds.Left) && Clip(delta.X,bounds.Right-from.X)
            && Clip(-delta.Y,from.Y-bounds.Top) && Clip(delta.Y,bounds.Bottom-from.Y);
    }
    public void SetStyle(string? stroke = null, string? fill = null, float? width = null, float? roughness = null, float? fontSize = null)
    {
        CancelGesture();
        // Validate before applying anything to the scene or defaults.
        using var validation = new SketchShape { Stroke = stroke ?? Stroke, Fill = fill ?? Fill, StrokeWidth = width ?? StrokeWidth,
            Roughness = roughness ?? Roughness, TextFontSize = fontSize ?? FontSize };
        var selected = EditingSelection ? SelectedComponents.ToArray() : [];
        if (roughness.HasValue && selected.Any(s=>s is not SketchShape))
            throw new InvalidOperationException("Sketch appearance is available for sketch objects only.");
        var before = selected.Select(ComponentEditAction.State.Capture).ToArray();
        if (fontSize.HasValue)
            foreach (var s in selected.OfType<SketchShape>().Where(s => s.Kind == SketchShapeKind.Text))
            {
                using var test = new SketchShape(); SketchEditAction.State.Capture(s).Apply(test);
                test.TextFontSize = fontSize.Value; var measured = test.MeasureText();
                if (!WithinBoard(new(s.X + measured.Width, s.Y + measured.Height))) throw new ArgumentOutOfRangeException(nameof(fontSize), "Text would exceed the board bounds.");
            }
        for (int i = 0; i < selected.Length; i++)
        {
            if (selected[i] is SketchShape sketch)
            {
                if (stroke is not null) sketch.Stroke = stroke;
                if (fill is not null) sketch.Fill = fill;
                if (width.HasValue) sketch.StrokeWidth = width.Value;
                if (roughness.HasValue) sketch.Roughness = roughness.Value;
                if (fontSize.HasValue && sketch.Kind == SketchShapeKind.Text)
                { sketch.TextFontSize = fontSize.Value; var size = sketch.MeasureText(); sketch.Width = size.Width; sketch.Height = size.Height; }
            }
            else if (selected[i] is FlowchartControl node)
            {
                if (stroke is not null) node.CustomStrokeColor = SKColor.Parse(stroke);
                if (fill is not null) node.CustomFillColor = SKColor.Parse(fill);
                if (width.HasValue) node.NodeStrokeWidth = width.Value;
                if (fontSize.HasValue) node.LabelFontSize = fontSize.Value;
            }
        }
        Stroke = validation.Stroke; Fill = validation.Fill; StrokeWidth = validation.StrokeWidth; Roughness = validation.Roughness; FontSize = validation.TextFontSize;
        if (selected.Where((s, i) => ComponentEditAction.State.Capture(s) != before[i]).Any())
        { Manager.HistoryManager.ExecuteAction(new ComponentEditAction(selected, before)); DocumentChanged?.Invoke(); }
    }

    private IEnumerable<SkiaComponent> Members(SkiaComponent shape) => shape.GroupId == Guid.Empty ? [shape] :
        Manager.GetComponents().Where(s => s.GroupId == shape.GroupId);
    private void SelectGroup(SkiaComponent shape, bool additive, bool toggle = true)
    {
        var members = Members(shape).ToArray(); var remove = additive && toggle && members.All(s => s.IsSelected);
        if (!additive) Manager.SelectionManager.ClearSelection();
        foreach (var member in members)
            if (remove) Manager.SelectionManager.RemoveFromSelection(member); else Manager.SelectionManager.SelectComponent(member, true);
    }
    public void SelectShape(SkiaComponent shape, bool additive = false)
    {
        if (!Manager.GetComponents().Contains(shape)) throw new ArgumentException("Shape is not in this board.");
        CancelGesture(); _tool = DrawingTool.Select; SelectGroup(shape, additive);
    }
    public void GroupSelection(bool ungroup = false)
    {
        CancelGesture(); var shapes = SelectedComponents.ToArray();
        if (!ungroup && shapes.Length < 2) return;
        if (!ungroup && shapes[0].GroupId != Guid.Empty && shapes.All(s => s.GroupId == shapes[0].GroupId)) return;
        var before = shapes.Select(ComponentEditAction.State.Capture).ToArray();
        var group = ungroup ? Guid.Empty : Guid.NewGuid();
        foreach (var shape in shapes) shape.GroupId = group;
        if (shapes.Where((s, i) => s.GroupId != before[i].GroupId).Any())
        { Manager.HistoryManager.ExecuteAction(new ComponentEditAction(shapes, before)); DocumentChanged?.Invoke(); }
    }
    public void DuplicateSelection()
    {
        CancelGesture(); var originals = SelectedComponents.ToArray();
        if (originals.Length == 0) return;
        if (Manager.GetComponents().Count + originals.Length > SimpleDrawDocument.MaxShapes) throw new InvalidOperationException("Duplicating would exceed the object limit.");
        if (originals.Any(s => !WithinBoard(new(s.X + s.Width + 24, s.Y + s.Height + 24)))) throw new InvalidOperationException("Duplicating would exceed the board bounds.");
        var selected = originals.Select(s=>s.Id).ToHashSet();
        var diagram = SimpleDrawDocument.SelectionSubset(SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(Manager)), selected);
        foreach (var c in diagram.Components) { c.Name = c.Name[..Math.Min(c.Name.Length,110)] + " copy"; }
        InsertDiagram(diagram,new(24,24));
    }
    public void ReorderSelection(bool front)
    {
        CancelGesture(); var selected = SelectedComponents.ToHashSet();
        if (selected.Count == 0) return;
        var all = Manager.GetComponents().ToArray(); var others = all.Where(s => !selected.Contains(s)); var moving = all.Where(selected.Contains);
        var order = (front ? others.Concat(moving) : moving.Concat(others)).ToArray();
        if (all.SequenceEqual(order)) return;
        Manager.SetComponentOrder(order); DocumentChanged?.Invoke();
    }
    public void InsertDiagram(Beep.Skia.Serialization.DiagramDto diagram, SKPoint location)
    {
        if (HasGesture || HasTextEdit) throw new InvalidOperationException("Finish or cancel the current edit before inserting a library entry.");
        if (!Finite(location)) throw new ArgumentException("Choose a finite insertion location.");
        var copy = SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(diagram));
        if (copy.Components.Count == 0) throw new InvalidDataException("The library entry is empty.");
        var groups = copy.Components.Select(c=>Guid.Parse(c.PropertyBag["GroupId"])).Where(id=>id!=Guid.Empty)
            .Distinct().ToDictionary(id=>id,_=>Guid.NewGuid());
        var ports = copy.Components.SelectMany(c => c.InPointIds.Concat(c.OutPointIds)).ToDictionary(id => id, _ => Guid.NewGuid());
        foreach (var c in copy.Components)
        {
            c.Id = Guid.NewGuid(); c.X+=location.X; c.Y+=location.Y;
            c.InPointIds = c.InPointIds.Select(id=>ports[id]).ToList();
            c.OutPointIds = c.OutPointIds.Select(id=>ports[id]).ToList();
            var group = Guid.Parse(c.PropertyBag["GroupId"]);
            c.PropertyBag["GroupId"] = (group==Guid.Empty ? Guid.Empty : groups[group]).ToString("D");
        }
        foreach (var line in copy.Lines) { line.StartPointId = ports[line.StartPointId]; line.EndPointId = ports[line.EndPointId]; }
        var combined = SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(Manager));
        combined.Components.AddRange(copy.Components); combined.Lines.AddRange(copy.Lines); SimpleDrawDocument.Serialize(combined);
        var lines = new List<ConnectionLine>();
        var prepared = SimpleDrawDocument.Prepare(copy, preparedLines: lines); // All validation happens before gesture/scene/history mutation.
        CancelGesture();
        try
        {
            using var batch = Manager.HistoryManager.BeginBatch();
            foreach (var shape in prepared) Manager.AddComponent(shape);
            foreach (var line in lines)
                if (!Manager.ConnectComponents(line)) throw new InvalidOperationException("Validated connector could not be inserted.");
            batch.Commit();
        }
        catch { foreach (var line in lines) line.Dispose(); foreach (var shape in prepared) shape.Dispose(); throw; }
        Tool = DrawingTool.Select; Manager.SelectionManager.ClearSelection();
        foreach (var shape in prepared) Manager.SelectionManager.SelectComponent(shape,true);
        DocumentChanged?.Invoke();
    }
    public void BeginText(SKPoint screen, bool create = true)
    {
        CancelGesture(); var world = ScreenToWorld(screen);
        if (!WithinBoard(world)) return;
        var target = Manager.GetComponents().OfType<SketchShape>().Reverse().FirstOrDefault(s => s.ContainsPoint(world, 5 / Manager.Zoom));
        if (target?.Kind != SketchShapeKind.Text) target = null;
        if (target is null && !create) return;
        if (target is null && Manager.GetComponents().Count >= SimpleDrawDocument.MaxShapes) throw new InvalidOperationException("This drawing has reached the object limit.");
        SimpleDrawFonts.EnsureLoaded(); TextTarget = target; TextAnchor = target is null ? world : new(target.X, target.Y);
    }
    public bool CommitText(string text, float? fontSize = null)
    {
        if (!TextAnchor.HasValue) return false;
        if (string.IsNullOrWhiteSpace(text)) { CancelGesture(); return false; }
        var target = TextTarget; var anchor = TextAnchor.Value; var shape = new SketchShape { Kind = SketchShapeKind.Text };
        try
        {
            if (target is not null) SketchEditAction.State.Capture(target).Apply(shape);
            else { shape.Stroke = Stroke; shape.TextFontSize = FontSize; }
            shape.DisplayText = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
            if (fontSize.HasValue) shape.TextFontSize = fontSize.Value;
            var size = shape.MeasureText();
            if (!WithinBoard(anchor + new SKPoint(size.Width, size.Height))) throw new ArgumentException("Text would exceed the board bounds.");
            shape.X = anchor.X; shape.Y = anchor.Y; shape.Width = size.Width; shape.Height = size.Height;
            if (target is null)
            {
                shape.Name = $"Text {Manager.GetComponents().Count + 1}"; Manager.AddComponent(shape);
                Manager.SelectionManager.SelectComponent(shape); shape = null!;
            }
            else
            {
                var before = SketchEditAction.State.Capture(target); var after = SketchEditAction.State.Capture(shape);
                if (before == after) { CancelGesture(); return false; }
                after.Apply(target); Manager.HistoryManager.ExecuteAction(new SketchEditAction([target], [before]));
            }
            CancelGesture(); DocumentChanged?.Invoke(); return true;
        }
        finally { shape?.Dispose(); }
    }
    private bool StartResize(SKPoint world)
    {
        var selected = SelectedComponents.ToArray(); if (selected.Length == 0 || !SelectionBounds.HasValue) return false;
        if (selected.Length == 1 && selected[0] is SketchShape line && line.Kind is SketchShapeKind.Line or SketchShapeKind.Arrow)
        {
            for (int i = 0; i < 2; i++) if ((world - line.WorldPoint(i)).Length * Manager.Zoom <= 8) { _endpoint = i; break; }
            if (_endpoint < 0) return false;
        }
        else if ((world - new SKPoint(SelectionBounds.Value.Right, SelectionBounds.Value.Bottom)).Length * Manager.Zoom > 8) return false;
        _resizeBounds = SelectionBounds.Value;
        foreach (var s in selected) _resizing.Add(s, ComponentEditAction.State.Capture(s));
        _resizeActive = true; return true;
    }
    public void ResizeSelection(float width, float height)
    {
        CancelGesture();
        if (!EditingSelection || !SelectionBounds.HasValue) return;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width < 1 || height < 1)
            throw new ArgumentException("Selection dimensions must be at least one drawing unit.");
        _resizeBounds = SelectionBounds.Value; var corner = new SKPoint(_resizeBounds.Left+width,_resizeBounds.Top+height);
        if (!WithinBoard(corner)) throw new ArgumentException("The resized selection would exceed the board bounds.");
        if (SelectedComponents.OfType<FlowchartControl>().Any(s=>s.Width*width/_resizeBounds.Width < 20 || s.Height*height/_resizeBounds.Height < 20))
            throw new ArgumentException("Workflow nodes need at least 20 drawing units per side.");
        foreach (var s in SelectedComponents) _resizing.Add(s,ComponentEditAction.State.Capture(s));
        _resizeActive = _active = true;
        PointerUp(new(corner.X*Manager.Zoom+Manager.PanOffset.X,corner.Y*Manager.Zoom+Manager.PanOffset.Y));
    }
    private void PreviewResize(SKPoint world)
    {
        if (_endpoint >= 0)
        {
            var shape = (SketchShape)_resizing.Keys.Single(); var before = _resizing[shape];
            using var original = new SketchShape(); before.Apply(original);
            var points = new[] { original.WorldPoint(0), original.WorldPoint(1) }; points[_endpoint] = world;
            if ((points[0] - points[1]).Length * Manager.Zoom >= 4) shape.SetWorldPath(points);
            return;
        }
        var sx = Math.Max(1, world.X - _resizeBounds.Left) / _resizeBounds.Width;
        var sy = Math.Max(1, world.Y - _resizeBounds.Top) / _resizeBounds.Height;
        var next = new List<ComponentEditAction.State>();
        foreach (var (shape, before) in _resizing)
        {
            var x = _resizeBounds.Left + (before.X - _resizeBounds.Left) * sx;
            var y = _resizeBounds.Top + (before.Y - _resizeBounds.Top) * sy;
            var width = before.Width * sx; var height = before.Height * sy; var size = before.TextFontSize;
            if (shape is SketchShape { Kind: SketchShapeKind.Text })
            {
                size = Math.Clamp(MathF.Round(before.TextFontSize * Math.Min(sx, sy), 1), 8, 120);
                using var text = new SketchShape(); before.Apply(text); text.TextFontSize = size;
                var measured = text.MeasureText(); width = measured.Width; height = measured.Height;
            }
            if (!WithinBoard(new(x, y)) || !WithinBoard(new(x + width, y + height)) || width <= 0 || height <= 0) return;
            if (shape is FlowchartControl && (width < 20 || height < 20))
            { GestureNotice = "Workflow nodes need at least 20 drawing units per side."; return; }
            next.Add(before.Resize(x,y,width,height,size));
        }
        var index = 0; foreach (var shape in _resizing.Keys) next[index++].Apply(shape);
    }
    public void SetFamilyLabel(string label)
    {
        if (HasGesture || HasTextEdit) throw new InvalidOperationException("Finish the current edit before changing a workflow label.");
        if (SelectedComponents.SingleOrDefault() is not FlowchartControl node) return;
        FlowchartDefinitions.ValidateLabel(label); var before = ComponentEditAction.State.Capture(node);
        FlowchartDefinitions.SetLabel(node,label);
        if (ComponentEditAction.State.Capture(node) != before)
        { Manager.HistoryManager.ExecuteAction(new ComponentEditAction([node],[before])); DocumentChanged?.Invoke(); }
    }
    public void InsertTemplate(string type)
    {
        FlowchartControl node = type switch
        {
            FlowchartDefinitions.ProcessType => new ProcessNode(), FlowchartDefinitions.DecisionType => new DecisionNode(),
            FlowchartDefinitions.TerminatorType => new StartEndNode(), _ => throw new InvalidDataException("Unknown workflow template.")
        };
        using (node)
        {
            node.ShowDisplayText = false; node.RequiredLabelFontFamily = "Kalam";
            var diagram = new Beep.Skia.Serialization.DiagramDto { Components = [FlowchartDefinitions.Capture(node)] };
            var center = ScreenToWorld(new(ViewportSize.Width/2,ViewportSize.Height/2));
            InsertDiagram(diagram,center-new SKPoint(node.Width/2,node.Height/2));
        }
    }

    public void Connect(Guid sourceId, int output, Guid targetId, int input, string? label, Beep.Skia.Model.LineRoutingMode routing)
    {
        if (HasGesture || HasTextEdit) throw new InvalidOperationException("Finish or cancel the current edit before connecting nodes.");
        if (Manager.GetLines().Count >= ConnectorDefinitions.MaxLines) throw new InvalidDataException("This board has reached the connector limit.");
        var source = Manager.GetComponents().OfType<FlowchartControl>().SingleOrDefault(n => n.Id == sourceId);
        var target = Manager.GetComponents().OfType<FlowchartControl>().SingleOrDefault(n => n.Id == targetId);
        if (source is null || target is null || source == target || output < 0 || input < 0
            || output >= source.OutConnectionPoints.Count || input >= target.InConnectionPoints.Count)
            throw new InvalidDataException("Choose output and input ports on two different workflow nodes.");
        var dto = ConnectorDefinitions.Create(source.OutConnectionPoints[output].Id, target.InConnectionPoints[input].Id,
            (uint)SKColor.Parse(Stroke), label, routing);
        var line = ConnectorDefinitions.Prepare([dto], Manager.GetComponents())[0];
        if (!Manager.ConnectComponents(line)) { line.Dispose(); throw new InvalidDataException("One of these ports is already connected."); }
        DocumentChanged?.Invoke();
    }
    public void RemoveConnection(Beep.Skia.Model.IConnectionLine line)
    {
        if (HasGesture || HasTextEdit) throw new InvalidOperationException("Finish or cancel the current edit before removing a connector.");
        if (!Manager.GetLines().Contains(line)) return;
        Manager.DisconnectComponents(line); DocumentChanged?.Invoke();
    }
    public void InsertConnectedTemplate()
    {
        var group = Guid.NewGuid();
        using var process = new ProcessNode { X = 0, Y = 10, Label = "Prepare", ShowDisplayText = false, RequiredLabelFontFamily = "Kalam", GroupId = group };
        using var decision = new DecisionNode { X = 250, Y = 0, Label = "Ready?", ShowDisplayText = false, RequiredLabelFontFamily = "Kalam", GroupId = group };
        using var finish = new StartEndNode { X = 500, Y = 10, Label = "Finish", ShowDisplayText = false, RequiredLabelFontFamily = "Kalam", GroupId = group };
        var diagram = new Beep.Skia.Serialization.DiagramDto { Components = [FlowchartDefinitions.Capture(process), FlowchartDefinitions.Capture(decision), FlowchartDefinitions.Capture(finish)],
            Lines = [ConnectorDefinitions.Create(process.OutConnectionPoints[0].Id, decision.InConnectionPoints[0].Id, (uint)SKColor.Parse(Stroke)),
                ConnectorDefinitions.Create(decision.OutConnectionPoints[0].Id, finish.InConnectionPoints[0].Id, (uint)SKColor.Parse(Stroke), "Yes")] };
        var center = ScreenToWorld(new(ViewportSize.Width / 2, ViewportSize.Height / 2));
        InsertDiagram(diagram, center - new SKPoint(330, 45));
    }

    public void Undo() { CancelGesture(); if (Manager.CanUndo) { Manager.Undo(); DocumentChanged?.Invoke(); } }
    public void Redo() { CancelGesture(); if (Manager.CanRedo) { Manager.Redo(); DocumentChanged?.Invoke(); } }
    public void DeleteSelected()
    {
        CancelGesture();
        if (Manager.SelectionManager.SelectedComponents.Count == 0) return;
        Manager.DeleteSelectedComponents(); DocumentChanged?.Invoke();
    }
    public void MoveSelected(SKPoint delta)
    {
        CancelGesture();
        if (Manager.SelectionManager.SelectedComponents.Count == 0 || !Finite(delta)) return;
        if (Manager.SelectionManager.SelectedComponents.Any(s => !WithinBoard(new SKPoint(s.X, s.Y) + delta)
            || !WithinBoard(new SKPoint(s.X + s.Width, s.Y + s.Height) + delta))) return;
        Manager.MoveSelectedComponents(delta); DocumentChanged?.Invoke();
    }
    public void ZoomAt(SKPoint screen, float factor)
    {
        CancelGesture();
        if (!Finite(screen) || !float.IsFinite(factor) || factor <= 0) return;
        var world = ScreenToWorld(screen);
        Manager.Zoom = Math.Clamp(Manager.Zoom * factor, 0.1f, 5f);
        Manager.PanOffset = screen - new SKPoint(world.X * Manager.Zoom, world.Y * Manager.Zoom);
    }
    public void ResizeViewport(float width, float height)
    {
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0) return;
        // Orientation changes cancel pointer previews, but preserve the user's HTML text draft.
        if (!HasTextEdit) CancelGesture();
        ViewportSize = new(width,height); KeyboardCursor = new(width / 2, height / 2);
    }
    public void BeginViewportGesture(SKPoint center)
    {
        if (!Finite(center)) return;
        CancelGesture(); _pinchWorld = ScreenToWorld(center); _pinchZoom = Manager.Zoom; _pinching = true;
    }
    public void UpdateViewportGesture(SKPoint center, float scale)
    {
        if (!_pinching || !Finite(center) || !float.IsFinite(scale) || scale <= 0) return;
        Manager.Zoom = Math.Clamp(_pinchZoom * scale,.1f,5f);
        Manager.PanOffset = center - new SKPoint(_pinchWorld.X * Manager.Zoom,_pinchWorld.Y * Manager.Zoom);
    }
    public void EndViewportGesture() => _pinching = false;
    public void FitDrawing()
    {
        CancelGesture(); var shapes = Manager.GetComponents(); if (shapes.Count == 0) return;
        var bounds = new SKRect(shapes.Min(s=>s.X),shapes.Min(s=>s.Y),shapes.Max(s=>s.X+s.Width),shapes.Max(s=>s.Y+s.Height));
        Manager.Zoom = Math.Clamp(Math.Min(Math.Max(20,ViewportSize.Width-48)/Math.Max(1,bounds.Width),
            Math.Max(20,ViewportSize.Height-260)/Math.Max(1,bounds.Height)),.1f,5f);
        Manager.PanOffset = new(ViewportSize.Width/2 - bounds.MidX*Manager.Zoom,(ViewportSize.Height+100)/2 - bounds.MidY*Manager.Zoom);
    }
    public void KeyboardEnter()
    {
        if (HasTextEdit) return;
        if (_keyboardGesture) { PointerUp(KeyboardCursor); _keyboardGesture = false; return; }
        if (Tool is DrawingTool.Select or DrawingTool.Hand or DrawingTool.Eraser) return;
        PointerDown(KeyboardCursor); _keyboardGesture = _active;
    }
    public void MoveKeyboardCursor(SKPoint delta)
    {
        if (!Finite(delta)) return;
        KeyboardCursor = new(Math.Clamp(KeyboardCursor.X+delta.X,0,ViewportSize.Width),Math.Clamp(KeyboardCursor.Y+delta.Y,0,ViewportSize.Height));
        if (_keyboardGesture) PointerMove(KeyboardCursor);
    }
    public void NewDocument()
    {
        CancelGesture(); var discarded = Manager.GetComponents().ToArray(); Manager.ClearComponents(); Manager.HistoryManager.Clear();
        foreach (var shape in discarded) shape.Dispose();
        Manager.Zoom = 1; Manager.PanOffset = SKPoint.Empty; DocumentChanged?.Invoke();
    }
    public void OpenDocument(string json)
    {
        // Parse and prepare everything before cancelling the gesture or replacing the board.
        var diagram = SimpleDrawDocument.Parse(json);
        CancelGesture();
        SimpleDrawDocument.Apply(Manager, diagram);
        Manager.Zoom = 1; Manager.PanOffset = SKPoint.Empty; DocumentChanged?.Invoke();
    }
    /// <summary>Known CSS-space ink for the session overlay before the host device transform; null means unknown.</summary>
    public SKRect? GetOverlayInkBounds()
    {
        var result = SKRect.Empty;
        var zoom = Manager.Zoom; var pan = Manager.PanOffset;
        void Include(SKRect world, float cssGuard)
        {
            var css = new SKRect(world.Left * zoom + pan.X, world.Top * zoom + pan.Y,
                world.Right * zoom + pan.X, world.Bottom * zoom + pan.Y);
            css.Inflate(cssGuard, cssGuard); result = result.IsEmpty ? css : SKRect.Union(result, css);
        }
        if (Draft is { } draft)
        { if (!draft.TryGetInkBounds(out var ink)) return null; if (!ink.IsEmpty) Include(ink, 2); }
        foreach (var shape in ErasePreview) Include(shape.Bounds, 3);
        if (Marquee is { } marquee) Include(marquee, 3);
        if (EditingSelection && SelectionBounds is { } selected) Include(selected, 7);
        if (HasKeyboardGesture)
        {
            var p = KeyboardCursor; var cursor = new SKRect(p.X - 10, p.Y - 10, p.X + 10, p.Y + 10);
            result = result.IsEmpty ? cursor : SKRect.Union(result, cursor);
        }
        return result;
    }
    /// <summary>Paints the existing draft, selection, eraser, marquee and keyboard cursor overlay.</summary>
    public void DrawOverlay(SKCanvas canvas)
    {
        canvas.Save(); canvas.Translate(Manager.PanOffset.X, Manager.PanOffset.Y);
        canvas.Scale(Manager.Zoom); Draft?.Draw(canvas);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#e06b56"), Style = SKPaintStyle.Stroke, StrokeWidth = 2 / Manager.Zoom };
        foreach (var shape in ErasePreview) canvas.DrawRect(shape.Bounds, paint);
        if (Marquee.HasValue) { paint.Color = SKColor.Parse("#7c3aed"); canvas.DrawRect(Marquee.Value, paint); }
        if (EditingSelection && SelectionBounds is { } bounds)
        {
            paint.Color = SKColor.Parse("#7c3aed"); paint.Style = SKPaintStyle.Fill;
            if (SelectedComponents.Count() == 1 && SelectedShapes.SingleOrDefault()?.Kind is SketchShapeKind.Line or SketchShapeKind.Arrow)
            { var line = SelectedShapes.Single(); for (int i = 0; i < 2; i++) canvas.DrawCircle(line.WorldPoint(i), 5 / Manager.Zoom, paint); }
            else
            {
                paint.Style = SKPaintStyle.Stroke; canvas.DrawRect(bounds, paint);
                paint.Style = SKPaintStyle.Fill; canvas.DrawCircle(bounds.Right, bounds.Bottom, 5 / Manager.Zoom, paint);
            }
        }
        canvas.Restore();
        if (HasKeyboardGesture)
        {
            paint.Style = SKPaintStyle.Stroke; paint.Color = SKColor.Parse("#7c3aed"); paint.StrokeWidth = 2;
            var p = KeyboardCursor; canvas.DrawLine(p.X-7,p.Y,p.X+7,p.Y,paint); canvas.DrawLine(p.X,p.Y-7,p.X,p.Y+7,paint);
        }
    }
    private static bool Finite(SKPoint p) => float.IsFinite(p.X) && float.IsFinite(p.Y);
    private static bool WithinBoard(SKPoint p) => Finite(p) && Math.Abs(p.X) <= 100_000 && Math.Abs(p.Y) <= 100_000;
}
