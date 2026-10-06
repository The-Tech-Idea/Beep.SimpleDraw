using System;
using System.Collections.Generic;
using System.Linq;
using Beep.Skia.Components;

namespace Beep.Skia;

/// <summary>One history entry for an already-applied edit of existing sketch components.</summary>
public sealed class SketchEditAction : DrawingAction
{
    public readonly record struct State(float X, float Y, float Width, float Height, string Name,
        SketchShapeKind Kind, string Stroke, string Fill, float StrokeWidth, string PathData,
        string DisplayText, float TextFontSize, Guid GroupId, float Roughness, int SketchSeed)
    {
        public static State Capture(SketchShape s) => new(s.X, s.Y, s.Width, s.Height, s.Name,
            s.Kind, s.Stroke, s.Fill, s.StrokeWidth, s.PathData, s.DisplayText, s.TextFontSize, s.GroupId, s.Roughness, s.SketchSeed);
        public void Apply(SketchShape s)
        {
            s.Kind = Kind; s.Stroke = Stroke; s.Fill = Fill; s.StrokeWidth = StrokeWidth;
            s.PathData = PathData; s.DisplayText = DisplayText; s.TextFontSize = TextFontSize;
            s.GroupId = GroupId; s.Roughness = Roughness; s.SketchSeed = SketchSeed;
            s.Name = Name; s.X = X; s.Y = Y; s.Width = Width; s.Height = Height;
        }
    }
    private readonly SketchShape[] _shapes;
    private readonly State[] _before, _after;
    public SketchEditAction(IEnumerable<SketchShape> shapes, IEnumerable<State> before)
    {
        _shapes = shapes.ToArray(); _before = before.ToArray();
        if (_shapes.Length != _before.Length) throw new ArgumentException("Each shape requires its previous state.");
        _after = _shapes.Select(State.Capture).ToArray();
    }
    public override void Execute() { for (int i = 0; i < _shapes.Length; i++) _after[i].Apply(_shapes[i]); }
    public override void Undo() { for (int i = 0; i < _shapes.Length; i++) _before[i].Apply(_shapes[i]); }
}
