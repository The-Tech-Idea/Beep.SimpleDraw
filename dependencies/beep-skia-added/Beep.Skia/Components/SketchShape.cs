using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beep.Skia.Model;
using SkiaSharp;

namespace Beep.Skia.Components;

public enum SketchShapeKind { Rectangle, Ellipse, Diamond, Line, Arrow, Freehand, Text }

// Render-cache key only. The component remains the scene/document authority.
internal readonly record struct SketchVisualState(SketchShapeKind Kind, float X, float Y, float Width, float Height,
    string Stroke, string Fill, float StrokeWidth, float Opacity, float Roughness, int Seed, string Path,
    string Text, float TextSize, bool Renderable, bool Static);

/// <summary>Lightweight sketch geometry on the existing scene and property contracts.</summary>
public sealed class SketchShape : SkiaComponent
{
    private SketchShapeKind _kind;
    private string _stroke = "#334155", _fill = "#00000000";
    private float _strokeWidth = 2;
    private SKPoint[] _points = Array.Empty<SKPoint>();
    private string _pathData = "";
    private float _roughness;
    private int _sketchSeed;
    private (SketchShapeKind Kind, float Width, float Height, string Path, float Roughness, int Seed)? _roughKey;
    private SKPoint[] _roughPoints = Array.Empty<SKPoint>();
    private (SketchShapeKind Kind, float X, float Y, float Width, float Height, string Path, float Roughness, int Seed, float StrokeWidth)? _drawKey;
    private SKPath _drawPath;
    private (string Fill, string Stroke, float Width)? _paintKey;
    private SKPaint _fillPaint, _strokePaint;
    private (float X, float Y, float Width, float Height, string Text, float Size, string Stroke)? _textKey;
    private SKPicture _textPicture;
    private SKRect _textInkBounds;
    private bool _textHasInkBounds;
    internal SketchVisualState VisualState => new(Kind, X, Y, Width, Height, Stroke, Fill, StrokeWidth, Opacity,
        Roughness, SketchSeed, PathData, DisplayText, TextFontSize,
        IsVisible && State != ComponentState.Disposing && State != ComponentState.Inactive, IsStatic);
    public const int MaxPathPoints = 4096;
    public SketchShape()
    {
        Width = 120; Height = 80;
        Kind = SketchShapeKind.Rectangle;
        Stroke = _stroke; Fill = _fill; StrokeWidth = _strokeWidth;
        PathData = "";
        DisplayText = ""; TextFontSize = 24; GroupId = Guid.Empty; Roughness = 0; SketchSeed = Id.GetHashCode() & int.MaxValue;
    }
    public SketchShape(Guid id) : this() { RestoreIdentity(id); }
    public const string TextFontFamily = "Kalam";
    public const int MaxTextLength = 4096;
    public new string DisplayText
    {
        get => base.DisplayText;
        set
        {
            if (value == null || value.Length > MaxTextLength || value.Count(c => c == '\n') >= 64
                || value.Split('\n').Any(line => line.Length > 512) || value.Any(c => char.IsControl(c) && c != '\n'))
                throw new ArgumentException("Text permits 4096 characters, 64 lines and 512 characters per line.", nameof(value));
            base.DisplayText = value; UpsertNodeProperty(nameof(DisplayText), typeof(string), currentValue: value);
        }
    }
    public new float TextFontSize
    {
        get => base.TextFontSize;
        set
        {
            if (!float.IsFinite(value) || value < 8 || value > 120) throw new ArgumentOutOfRangeException(nameof(value));
            base.TextFontSize = value; UpsertNodeProperty(nameof(TextFontSize), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
    public float Roughness
    {
        get => _roughness;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 2) throw new ArgumentOutOfRangeException(nameof(value));
            _roughness = value; UpsertNodeProperty(nameof(Roughness), typeof(string), currentValue: value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
    public int SketchSeed
    {
        get => _sketchSeed;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _sketchSeed = value; UpsertNodeProperty(nameof(SketchSeed), typeof(string), currentValue: value.ToString(CultureInfo.InvariantCulture));
        }
    }
    public SKSize MeasureText()
    {
        using var font = CreateTextFont();
        if (!font.ContainsGlyphs(DisplayText.Replace("\n", ""))) throw new ArgumentException("The bundled Kalam font does not support one or more characters.");
        var lines = DisplayText.Split('\n');
        return new(Math.Max(1, lines.Max(line => font.MeasureText(line))) + 8, lines.Length * TextFontSize * 1.4f + 8);
    }
    protected override void DrawText(SKCanvas canvas, DrawingContext context)
    {
        if (Kind != SketchShapeKind.Text || DisplayText.Length == 0) return;
        EnsureTextPicture();
        canvas.DrawPicture(_textPicture);
    }
    private void EnsureTextPicture(bool captureBounds = false)
    {
        var key = (X, Y, Width, Height, DisplayText, TextFontSize, Stroke);
        if (_textKey != key || _textPicture == null || captureBounds && !_textHasInkBounds)
        {
            using var recorder = new SKPictureRecorder();
            var recorded = recorder.BeginRecording(new SKRect(X - TextFontSize * 2, Y - TextFontSize * 2,
                X + Width + TextFontSize * 2, Y + Height + TextFontSize * 2));
            _textInkBounds = DrawTextRun(recorded, captureBounds);
            _textHasInkBounds = captureBounds;
            var picture = recorder.EndRecording();
            _textPicture?.Dispose(); _textPicture = picture; _textKey = key;
        }
    }
    private SKRect DrawTextRun(SKCanvas canvas, bool captureBounds)
    {
        var bounds = SKRect.Empty;
        using var font = CreateTextFont();
        using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Stroke) };
        var baseline = Y + 4 - font.Metrics.Ascent;
        foreach (var line in DisplayText.Split('\n'))
        {
            if (captureBounds)
            {
                TypefaceCache.DrawGlyphRun(canvas, font, line, new SKPoint(X + 4, baseline), paint, out var ink);
                if (!ink.IsEmpty) bounds = bounds.IsEmpty ? ink : SKRect.Union(bounds, ink);
            }
            else TypefaceCache.DrawGlyphRun(canvas, font, line, new SKPoint(X + 4, baseline), paint);
            baseline += TextFontSize * 1.4f;
        }
        return bounds;
    }
    /// <summary>Conservative bounds of this leaf's actual world-space drawing commands.
    /// Returns false when child content makes the footprint unknown. Empty means no ink.
    /// Transform the result and add at least two device pixels for antialias coverage;
    /// this contract does not by itself track damage, connections or host overlays.</summary>
    public bool TryGetInkBounds(out SKRect bounds)
    {
        bounds = SKRect.Empty;
        if (Children.Count != 0) return false;
        if (!IsVisible || State == ComponentState.Disposing || State == ComponentState.Inactive) return true;
        if (Kind == SketchShapeKind.Text)
        {
            if (DisplayText.Length == 0) return true;
            EnsureTextPicture(true); bounds = _textInkBounds;
        }
        else
        {
            var path = GetDrawPath();
            if (path == null || path.IsEmpty) return true;
            bounds = path.Bounds; bounds.Inflate(StrokeWidth / 2, StrokeWidth / 2);
        }
        return true;
    }
    private SKFont CreateTextFont() => new(TypefaceCache.GetRequired(TextFontFamily), TextFontSize)
    { Hinting = SKFontHinting.None, Subpixel = true, LinearMetrics = true, Edging = SKFontEdging.Antialias };
    /// <summary>Cached local sketch outline. Fixed xorshift algorithm preserves seed behavior across runtimes.</summary>
    public IReadOnlyList<SKPoint> SketchOutline()
    {
        var key = (Kind, Width, Height, PathData, Roughness, SketchSeed);
        if (_roughKey == key) return Array.AsReadOnly(_roughPoints);
        var points = new List<SKPoint>();
        if (IsPath)
        {
            for (int i = 0; i < _points.Length; i++) points.Add(new(_points[i].X * Width, _points[i].Y * Height));
            if (points.Count == 2) points.Insert(1, new((points[0].X + points[1].X) / 2, (points[0].Y + points[1].Y) / 2));
        }
        else if (Kind == SketchShapeKind.Ellipse)
        {
            for (int i = 0; i <= 64; i++) { var angle = i * Math.PI / 32; points.Add(new((float)(Width / 2 * (1 + Math.Cos(angle))), (float)(Height / 2 * (1 + Math.Sin(angle))))); }
        }
        else if (Kind != SketchShapeKind.Text)
        {
            SKPoint[] corners = Kind == SketchShapeKind.Diamond
                ? new[] { new SKPoint(Width / 2, 0), new SKPoint(Width, Height / 2), new SKPoint(Width / 2, Height), new SKPoint(0, Height / 2) }
                : new[] { new SKPoint(0, 0), new SKPoint(Width, 0), new SKPoint(Width, Height), new SKPoint(0, Height) };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 8; j++) { var a = corners[i]; var b = corners[(i + 1) % 4]; points.Add(new(a.X + (b.X - a.X) * j / 8, a.Y + (b.Y - a.Y) * j / 8)); }
            points.Add(points[0]);
        }
        uint state = (uint)SketchSeed | 1u;
        float Noise() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return (state / (float)uint.MaxValue * 2 - 1) * Roughness; }
        for (int i = IsPath ? 1 : 0; i < points.Count - 1; i++) points[i] += new SKPoint(Noise(), Noise());
        if (!IsPath && points.Count > 0) points[^1] = points[0];
        _roughPoints = points.ToArray(); _roughKey = key; return Array.AsReadOnly(_roughPoints);
    }
    /// <summary>Ordered points normalized to the shape bounds; direction survives resizing and serialization.</summary>
    public IReadOnlyList<SKPoint> PathPoints => Array.AsReadOnly(_points);
    public string PathData
    {
        get => _pathData;
        set
        {
            if (value == null || value.Length > 120_000) throw new ArgumentException("Invalid path data.", nameof(value));
            var parts = value.Length == 0 ? Array.Empty<string>() : value.Split(';');
            if (parts.Length > MaxPathPoints) throw new ArgumentException("Too many path points.", nameof(value));
            var points = new SKPoint[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var xy = parts[i].Split(',');
                if (xy.Length != 2 || !float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    || !float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                    || !float.IsFinite(x) || !float.IsFinite(y) || x < 0 || x > 1 || y < 0 || y > 1)
                    throw new ArgumentException("Path points must be finite normalized coordinates.", nameof(value));
                points[i] = new SKPoint(x, y);
            }
            _points = points; _pathData = value;
            UpsertNodeProperty(nameof(PathData), typeof(string), currentValue: value);
        }
    }
    public bool IsPath => Kind is SketchShapeKind.Line or SketchShapeKind.Arrow or SketchShapeKind.Freehand;
    public void SetWorldPath(IReadOnlyList<SKPoint> points)
    {
        if (points == null || points.Count < 2 || points.Count > MaxPathPoints
            || points.Any(p => !float.IsFinite(p.X) || !float.IsFinite(p.Y)))
            throw new ArgumentException("A path needs 2 to 4096 finite points.", nameof(points));
        var left = points.Min(p => p.X); var top = points.Min(p => p.Y);
        var width = Math.Max(0.01f, points.Max(p => p.X) - left);
        var height = Math.Max(0.01f, points.Max(p => p.Y) - top);
        PathData = string.Join(";", points.Select(p =>
            ((p.X - left) / width).ToString("R", CultureInfo.InvariantCulture) + "," +
            ((p.Y - top) / height).ToString("R", CultureInfo.InvariantCulture)));
        X = left; Y = top; Width = width; Height = height;
    }
    public SKPoint WorldPoint(int index) => new(X + _points[index].X * Width, Y + _points[index].Y * Height);
    public static float DistanceToSegment(SKPoint p, SKPoint a, SKPoint b)
    {
        var d = b - a; var length = d.X * d.X + d.Y * d.Y;
        var t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * d.X + (p.Y - a.Y) * d.Y) / length, 0, 1);
        return (p - (a + new SKPoint(d.X * t, d.Y * t))).Length;
    }
    private static float SegmentDistance(SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        var r = b - a; var s = d - c;
        static float Cross(SKPoint x, SKPoint y) => x.X * y.Y - x.Y * y.X;
        var denominator = Cross(r, s);
        if (denominator != 0)
        {
            var t = Cross(c - a, s) / denominator; var u = Cross(c - a, r) / denominator;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
        }
        return Math.Min(Math.Min(DistanceToSegment(a, c, d), DistanceToSegment(b, c, d)),
            Math.Min(DistanceToSegment(c, a, b), DistanceToSegment(d, a, b)));
    }
    /// <summary>Tests the whole eraser sweep, including sparse or fast pointer movement.</summary>
    public bool IntersectsSegment(SKPoint a, SKPoint b, float tolerance)
    {
        var distance = Math.Max(0, tolerance) + StrokeWidth / 2;
        if (IsPath)
        {
            for (int i = 1; i < _points.Length; i++)
                if (SegmentDistance(a, b, WorldPoint(i - 1), WorldPoint(i)) <= distance) return true;
            if (Kind == SketchShapeKind.Arrow && _points.Length >= 2)
            {
                var head = ArrowHead(); var end = WorldPoint(_points.Length - 1);
                return SegmentDistance(a, b, head.Left, end) <= distance || SegmentDistance(a, b, head.Right, end) <= distance;
            }
            return false;
        }
        if (Width <= 0 || Height <= 0) return false;
        if (ContainsPoint(a) || ContainsPoint(b)) return true;
        var cx = X + Width / 2; var cy = Y + Height / 2;
        if (Kind == SketchShapeKind.Ellipse)
        {
            var rx = Width / 2 + distance; var ry = Height / 2 + distance;
            return DistanceToSegment(SKPoint.Empty, new((a.X - cx) / rx, (a.Y - cy) / ry),
                new((b.X - cx) / rx, (b.Y - cy) / ry)) <= 1;
        }
        SKPoint[] corners = Kind == SketchShapeKind.Diamond
            ? new[] { new SKPoint(cx, Y), new SKPoint(X + Width, cy), new SKPoint(cx, Y + Height), new SKPoint(X, cy) }
            : new[] { new SKPoint(X, Y), new SKPoint(X + Width, Y), new SKPoint(X + Width, Y + Height), new SKPoint(X, Y + Height) };
        for (int i = 0; i < corners.Length; i++)
            if (SegmentDistance(a, b, corners[i], corners[(i + 1) % corners.Length]) <= distance) return true;
        return false;
    }
    public (SKPoint Left, SKPoint Right) ArrowHead()
    {
        var end = WorldPoint(_points.Length - 1); var previous = WorldPoint(_points.Length - 2);
        var d = end - previous; var length = d.Length;
        if (length <= 0) return (end, end);
        var size = Math.Min(length * 0.45f, Math.Max(10, StrokeWidth * 4));
        var ux = d.X / length; var uy = d.Y / length;
        return (new(end.X - ux * size - uy * size * .5f, end.Y - uy * size + ux * size * .5f),
            new(end.X - ux * size + uy * size * .5f, end.Y - uy * size - ux * size * .5f));
    }
    public SketchShapeKind Kind
    {
        get => _kind;
        set
        {
            if (!Enum.IsDefined(typeof(SketchShapeKind), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _kind = value;
            UpsertNodeProperty(nameof(Kind), typeof(string), currentValue: value.ToString());
        }
    }
    public string Stroke
    {
        get => _stroke;
        set { ValidateColor(value); _stroke = value; UpsertNodeProperty(nameof(Stroke), typeof(string), currentValue: value); }
    }
    public string Fill
    {
        get => _fill;
        set { ValidateColor(value); _fill = value; UpsertNodeProperty(nameof(Fill), typeof(string), currentValue: value); }
    }
    public float StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            if (!float.IsFinite(value) || value < 0.5f || value > 20) throw new ArgumentOutOfRangeException(nameof(value));
            _strokeWidth = value;
            UpsertNodeProperty(nameof(StrokeWidth), typeof(string), currentValue: value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
    private static void ValidateColor(string value)
    {
        if (value == null || (value.Length != 7 && value.Length != 9) || value[0] != '#' || !SKColor.TryParse(value, out _))
            throw new ArgumentException("Use a #RRGGBB or #AARRGGBB color.", nameof(value));
    }
    private SKPath GetDrawPath()
    {
        var key = (Kind, X, Y, Width, Height, PathData, Roughness, SketchSeed, StrokeWidth);
        if (_drawKey == key) return _drawPath;
        _drawPath?.Dispose(); _drawPath = null; _drawKey = key;
        if (Kind == SketchShapeKind.Text || IsPath && _points.Length < 2) return null;
        using var builder = new SKPathBuilder();
        var bounds = new SKRect(X, Y, X + Width, Y + Height);
        switch (Kind)
        {
            case SketchShapeKind.Line:
            case SketchShapeKind.Arrow:
            case SketchShapeKind.Freehand:
                builder.MoveTo(WorldPoint(0));
                for (int i = 1; i < _points.Length; i++) builder.LineTo(WorldPoint(i));
                if (Kind == SketchShapeKind.Arrow)
                {
                    var head = ArrowHead(); var end = WorldPoint(_points.Length - 1);
                    builder.MoveTo(head.Left); builder.LineTo(end); builder.LineTo(head.Right);
                }
                break;
            case SketchShapeKind.Rectangle: builder.AddRoundRect(bounds, 8, 8); break;
            case SketchShapeKind.Ellipse: builder.AddOval(bounds); break;
            case SketchShapeKind.Diamond:
                builder.MoveTo(bounds.MidX, bounds.Top); builder.LineTo(bounds.Right, bounds.MidY);
                builder.LineTo(bounds.MidX, bounds.Bottom); builder.LineTo(bounds.Left, bounds.MidY); builder.Close(); break;
        }
        if (Roughness > 0)
        {
            var outline = SketchOutline(); builder.Reset();
            if (outline.Count == 0) return null;
            builder.MoveTo(X + outline[0].X, Y + outline[0].Y);
            for (int i = 1; i < outline.Count; i++) builder.LineTo(X + outline[i].X, Y + outline[i].Y);
            if (!IsPath) builder.Close();
            if (Kind == SketchShapeKind.Arrow)
            { var head = ArrowHead(); builder.MoveTo(head.Left); builder.LineTo(WorldPoint(_points.Length - 1)); builder.LineTo(head.Right); }
        }
        return _drawPath = builder.Detach();
    }
    protected override void DrawContent(SKCanvas canvas, DrawingContext context)
    {
        var path = GetDrawPath(); if (path == null) return;
        var key = (Fill, Stroke, StrokeWidth);
        if (_paintKey != key || _fillPaint == null || _strokePaint == null)
        {
            _fillPaint?.Dispose(); _strokePaint?.Dispose();
            _fillPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Fill), Style = SKPaintStyle.Fill };
            _strokePaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(Stroke), Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round };
            _paintKey = key;
        }
        if (!IsPath) canvas.DrawPath(path, _fillPaint);
        canvas.DrawPath(path, _strokePaint);
    }
    protected override void DisposeUnmanagedResources()
    {
        _drawPath?.Dispose(); _drawPath = null; _drawKey = null;
        _fillPaint?.Dispose(); _fillPaint = null; _strokePaint?.Dispose(); _strokePaint = null; _paintKey = null;
        _textPicture?.Dispose(); _textPicture = null; _textKey = null;
        base.DisposeUnmanagedResources();
    }
    public override bool ContainsPoint(SKPoint point)
    {
        return ContainsPoint(point, 5);
    }
    public bool ContainsPoint(SKPoint point, float tolerance)
    {
        if (IsPath)
        {
            var distance = Math.Max(0, tolerance) + StrokeWidth / 2;
            for (int i = 1; i < _points.Length; i++)
                if (DistanceToSegment(point, WorldPoint(i - 1), WorldPoint(i)) <= distance) return true;
            if (Kind == SketchShapeKind.Arrow && _points.Length >= 2)
            {
                var head = ArrowHead(); var end = WorldPoint(_points.Length - 1);
                return DistanceToSegment(point, head.Left, end) <= distance || DistanceToSegment(point, head.Right, end) <= distance;
            }
            return false;
        }
        if (Width <= 0 || Height <= 0) return false;
        var nx = (point.X - X - Width / 2) / (Width / 2);
        var ny = (point.Y - Y - Height / 2) / (Height / 2);
        return Kind switch
        {
            SketchShapeKind.Ellipse => nx * nx + ny * ny <= 1,
            SketchShapeKind.Diamond => Math.Abs(nx) + Math.Abs(ny) <= 1,
            _ => Math.Abs(nx) <= 1 && Math.Abs(ny) <= 1
        };
    }
}
