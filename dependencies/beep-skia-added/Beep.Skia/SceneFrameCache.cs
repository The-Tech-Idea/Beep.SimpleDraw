using System;
using System.Collections.Generic;
using Beep.Skia.Components;
using SkiaSharp;

namespace Beep.Skia;

/// <summary>Host-owned disposable pixels and visual keys for the existing renderer.
/// It owns no document, history, component visibility or component lifetime.</summary>
public sealed class SceneFrameCache : IDisposable
{
    private readonly record struct Entry(SketchVisualState State, SKRect Ink);
    private readonly Dictionary<SkiaComponent, Entry> _entries = new();
    private readonly List<SkiaComponent> _order = new();
    private DrawingManager _manager;
    private SKImageInfo _info;
    private SKMatrix _matrix;
    private SKColor _background, _gridColor;
    private float _zoom, _spacing;
    private SKPoint _pan;
    private bool _grid, _valid, _rejectFrame;
    private SKRect _overlay, _damage;
    private SKBitmap _scratch, _retained;
    internal SKBitmap Scratch => _scratch;
    internal bool Full { get; private set; }
    public SKRectI LastDamage { get; private set; }
    public int LastDrawnComponents { get; internal set; }
    public int LastSkippedComponents { get; internal set; }
    public long FullFrameCount { get; private set; }
    public long PartialFrameCount { get; private set; }
    public long ReusedFrameCount { get; private set; }

    public void Reset() { _valid = false; _entries.Clear(); _order.Clear(); _overlay = SKRect.Empty; }
    internal void Begin(DrawingManager manager, SKImageInfo info, SKMatrix matrix, SKColor background, SKRect overlay)
    {
        Full = !_valid || !ReferenceEquals(_manager, manager) || !_info.Equals(info) || !_matrix.Equals(matrix)
            || _background != background || _zoom != manager.Zoom || _pan != manager.PanOffset
            || _grid != manager.ShowGrid || _spacing != manager.GridSpacing || _gridColor != ThemeManager.Current.GridColor;
        if (_scratch == null || !_info.Equals(info))
        {
            _scratch?.Dispose(); _retained?.Dispose();
            _scratch = new SKBitmap(info); _retained = new SKBitmap(info);
        }
        _manager = manager; _info = info; _matrix = matrix; _background = background;
        _zoom = manager.Zoom; _pan = manager.PanOffset; _grid = manager.ShowGrid;
        _spacing = manager.GridSpacing; _gridColor = ThemeManager.Current.GridColor;
        _damage = SKRect.Empty; _rejectFrame = false;
        LastDrawnComponents = LastSkippedComponents = 0;
        // Repaint old and current overlay ink even when the bounds did not move:
        // overlay color/contents can change without changing its footprint.
        AddDamage(_overlay); AddDamage(DeviceOverlay(overlay)); _overlay = DeviceOverlay(overlay);
    }
    internal static bool Finite(SKRect r) => float.IsFinite(r.Left) && float.IsFinite(r.Top) && float.IsFinite(r.Right) && float.IsFinite(r.Bottom);
    private SKRect DeviceOverlay(SKRect css)
    {
        if (css.IsEmpty) return SKRect.Empty;
        var device = _matrix.MapRect(css); device.Inflate(2, 2); return device;
    }
    private SKRect DeviceInk(SKRect world)
    {
        if (world.IsEmpty) return SKRect.Empty;
        var css = new SKRect(world.Left * _zoom + _pan.X, world.Top * _zoom + _pan.Y,
            world.Right * _zoom + _pan.X, world.Bottom * _zoom + _pan.Y);
        var device = _matrix.MapRect(css); device.Inflate(2, 2); return device;
    }
    private void AddDamage(SKRect ink)
    {
        if (!Finite(ink)) { Full = true; _rejectFrame = true; return; }
        if (!ink.IsEmpty) _damage = _damage.IsEmpty ? ink : SKRect.Union(_damage, ink);
    }
    internal void PrepareAfterUpdate(IReadOnlyList<SkiaComponent> components)
    {
        if (_order.Count != components.Count) Full = true;
        else for (var i = 0; i < components.Count; i++) if (!ReferenceEquals(_order[i], components[i])) { Full = true; break; }
        if (Full) { _entries.Clear(); _order.Clear(); }
        foreach (var component in components)
        {
            if (component is not SketchShape shape || shape.IsStatic || shape.Children.Count != 0)
            { Full = true; _rejectFrame = true; continue; }
            var state = shape.VisualState;
            if (_entries.TryGetValue(shape, out var before) && before.State == state) continue;
            if (!shape.TryGetInkBounds(out var ink) || !Finite(ink)) { Full = true; _rejectFrame = true; continue; }
            AddDamage(DeviceInk(before.Ink)); AddDamage(DeviceInk(ink));
            _entries[shape] = new(state, ink);
        }
        if (Full)
        {
            _damage = new SKRect(0, 0, _info.Width, _info.Height);
            _order.Clear(); foreach (var component in components) _order.Add(component);
        }
        var left = Math.Max(0, _damage.Left); var top = Math.Max(0, _damage.Top);
        var right = Math.Min(_info.Width, _damage.Right); var bottom = Math.Min(_info.Height, _damage.Bottom);
        _damage = right > left && bottom > top ? new SKRect(left, top, right, bottom) : SKRect.Empty;
        LastDamage = _damage.IsEmpty ? SKRectI.Empty : new SKRectI((int)Math.Floor(_damage.Left), (int)Math.Floor(_damage.Top),
            (int)Math.Ceiling(_damage.Right), (int)Math.Ceiling(_damage.Bottom));
    }
    internal bool Contributes(SkiaComponent component)
    {
        if (Full) return true;
        if (LastDamage.IsEmpty) return false;
        return !_entries.TryGetValue(component, out var entry) || DeviceInk(entry.Ink).IntersectsWith(new SKRect(LastDamage.Left, LastDamage.Top, LastDamage.Right, LastDamage.Bottom));
    }
    internal void Present(SKPixmap target)
    {
        var source = _scratch.GetPixelSpan(); var retained = _retained.GetPixelSpan();
        for (var y = LastDamage.Top; y < LastDamage.Bottom; y++)
            source.Slice(y * _scratch.RowBytes + LastDamage.Left * 4, LastDamage.Width * 4)
                .CopyTo(retained.Slice(y * _retained.RowBytes + LastDamage.Left * 4, LastDamage.Width * 4));
        var output = target.GetPixelSpan();
        // Write the complete retained frame: a fresh same-size target allocation is safe too.
        for (var y = 0; y < _info.Height; y++) retained.Slice(y * _retained.RowBytes, _info.Width * 4)
            .CopyTo(output.Slice(y * target.RowBytes, _info.Width * 4));
        _valid = !_rejectFrame;
        if (Full) FullFrameCount++; else if (LastDamage.IsEmpty) ReusedFrameCount++; else PartialFrameCount++;
    }
    internal void Uncached(SKImageInfo info)
    {
        Reset(); Full = true; LastDrawnComponents = LastSkippedComponents = 0;
        LastDamage = new(0, 0, info.Width, info.Height); FullFrameCount++;
    }
    public void Dispose() { Reset(); _scratch?.Dispose(); _retained?.Dispose(); _scratch = _retained = null; }
}
