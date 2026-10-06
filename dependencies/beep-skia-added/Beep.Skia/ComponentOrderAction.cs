namespace Beep.Skia;

/// <summary>Restores component paint order without removing components or their connections.</summary>
public sealed class ComponentOrderAction : DrawingAction
{
    private readonly DrawingManager _manager;
    private readonly SkiaComponent[] _before, _after;
    public ComponentOrderAction(DrawingManager manager, SkiaComponent[] before, SkiaComponent[] after)
    { _manager = manager; _before = before; _after = after; }
    public override void Execute() => _manager.RestoreComponentOrder(_after);
    public override void Undo() => _manager.RestoreComponentOrder(_before);
}
