using Beep.Skia;
using Beep.Skia.Flowchart;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public sealed class ConnectionHistoryTests
{
    private static (DrawingManager Manager, ProcessNode Source, DecisionNode Target) Scene()
    {
        var manager = new DrawingManager();
        var source = new ProcessNode(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], [Guid.NewGuid(), Guid.NewGuid()]) { X = 20, Y = 60 };
        var target = new DecisionNode(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], [Guid.NewGuid(), Guid.NewGuid()]) { X = 340, Y = 100 };
        manager.AddComponent(source); manager.AddComponent(target); manager.HistoryManager.Clear();
        return (manager, source, target);
    }
    private static ConnectionLine Connect(DrawingManager manager, SkiaComponent source, SkiaComponent target, int index = 1)
    {
        var line = new ConnectionLine(source.OutConnectionPoints[index], target.InConnectionPoints[index], () => { })
        { Label1 = "branch", Label2 = "accepted", ShowStartArrow = true, LineColor = SKColors.DarkGreen, RoutingMode = LineRoutingMode.Orthogonal };
        line.Paint.StrokeWidth = 5;
        Assert.True(manager.ConnectComponents(line)); return line;
    }
    private static void Attached(ConnectionLine line, bool attached)
    {
        Assert.Equal(attached, line.IsConnected);
        Assert.Equal(!attached, line.Start.IsAvailable); Assert.Equal(!attached, line.End.IsAvailable);
        Assert.Equal(attached ? line.End : null, line.Start.Connection); Assert.Equal(attached ? line.Start : null, line.End.Connection);
    }
    [Fact]
    public void RedoRestoresExactBranchLineAndItsStyleInsteadOfChoosingFirstPorts()
    {
        var (manager, source, target) = Scene(); var line = Connect(manager, source, target);
        var start = line.Start; var end = line.End;
        for (int i = 0; i < 3; i++)
        {
            manager.HistoryManager.Undo(); Assert.Empty(manager.GetLines()); Attached(line, false);
            Assert.False(source.IsConnectedTo(target)); Assert.False(target.IsConnectedTo(source));
            manager.HistoryManager.Redo(); Assert.Same(line, Assert.Single(manager.GetLines())); Attached(line, true);
            Assert.Same(start, line.Start); Assert.Same(end, line.End);
            Assert.True(source.IsConnectedTo(target)); Assert.True(target.IsConnectedTo(source));
            Assert.Equal(5, line.Paint.StrokeWidth); Assert.Equal(SKColors.DarkGreen, line.LineColor);
            Assert.Equal("accepted", line.Label2); Assert.True(line.ShowStartArrow); Assert.Equal(LineRoutingMode.Orthogonal, line.RoutingMode);
        }
        Assert.True(source.OutConnectionPoints[0].IsAvailable); Assert.Equal(1, manager.HistoryManager.UndoCount);
    }
    [Fact]
    public void ExactDisconnectRetainsOtherBranchAndUndoRestoresTheSameLine()
    {
        var (manager, source, target) = Scene(); var first = Connect(manager, source, target, 0); var second = Connect(manager, source, target);
        manager.DisconnectComponents(first); Attached(first, false); Attached(second, true);
        Assert.Same(second, Assert.Single(manager.GetLines())); Assert.True(source.IsConnectedTo(target));
        manager.HistoryManager.Undo(); Assert.Contains(first, manager.GetLines()); Attached(first, true);
        manager.HistoryManager.Redo(); Attached(first, false); Assert.Same(second, Assert.Single(manager.GetLines()));
        manager.DisconnectComponents(second); Assert.False(source.IsConnectedTo(target)); Assert.False(target.IsConnectedTo(source));
        manager.HistoryManager.Undo(); Attached(second, true); Assert.True(source.IsConnectedTo(target));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ComponentAndSelectionDeletionRestoreExactLinesAndPortAvailability(bool selection)
    {
        var (manager, source, target) = Scene(); var line = Connect(manager, source, target);
        if (selection) { manager.SelectAllComponents(); manager.DeleteSelectedComponents(); }
        else manager.RemoveComponent(source);
        Attached(line, false); Assert.Empty(manager.GetLines()); Assert.False(target.IsConnectedTo(source));
        for (int i = 0; i < 2; i++)
        {
            manager.HistoryManager.Undo(); Assert.Equal(new SkiaComponent[] { source, target }, manager.GetComponents());
            Assert.Same(line, Assert.Single(manager.GetLines())); Attached(line, true);
            manager.HistoryManager.Redo(); Assert.Empty(manager.GetLines()); Attached(line, false);
        }
    }
    [Theory]
    [InlineData("foreign-owner")] [InlineData("occupied")] [InlineData("same-owner")] [InlineData("wrong-direction")]
    public void InvalidConnectionDoesNotMutateSceneOrHistory(string failure)
    {
        var (manager, source, target) = Scene(); var existing = Connect(manager, source, target);
        using var foreign = new ProcessNode();
        var start = source.OutConnectionPoints[0]; var end = target.InConnectionPoints[0];
        if (failure == "foreign-owner") start = foreign.OutConnectionPoints[0];
        if (failure == "occupied") start = source.OutConnectionPoints[1];
        if (failure == "same-owner") end = source.InConnectionPoints[0];
        if (failure == "wrong-direction") start = source.InConnectionPoints[0];
        using var rejected = new ConnectionLine(start, end, () => { });
        Assert.False(manager.ConnectComponents(rejected)); Assert.Equal(1, manager.HistoryManager.UndoCount);
        Assert.Same(existing, Assert.Single(manager.GetLines())); Attached(existing, true);
        Assert.True(target.InConnectionPoints[0].IsAvailable); Assert.Null(target.InConnectionPoints[0].Connection);
    }
    [Fact]
    public void ClearDetachesSurvivingReferencesAndGeometryUpdatesWithoutAFirstPaint()
    {
        var (manager, source, target) = Scene(); var line = Connect(manager, source, target);
        var before = line.Start.Center; Assert.NotEqual(SKPoint.Empty, before);
        source.X += 40; source.Y += 30; manager.RefreshConnectionPoints(source);
        Assert.Equal(new SKPoint(before.X + 40, before.Y + 30), line.Start.Center); Attached(line, true);
        manager.ClearComponents(); Attached(line, false); Assert.False(source.IsConnectedTo(target));
        Assert.Empty(manager.GetLines()); Assert.Empty(manager.GetComponents());
    }
    [Fact]
    public void RewireUpdatesBothPortsAndComponentRelationshipsThroughUndoAndRedo()
    {
        var (manager, source, target) = Scene(); var line = Connect(manager, source, target);
        var other = new ProcessNode { X = 600, Y = 200 }; manager.AddComponent(other);
        var oldEnd = line.End; var nextEnd = other.InConnectionPoints[0];
        manager.MoveConnectionLine(line, newEndPoint: nextEnd);
        Assert.Same(nextEnd, line.End); Attached(line, true); Assert.True(oldEnd.IsAvailable); Assert.Null(oldEnd.Connection);
        Assert.False(source.IsConnectedTo(target)); Assert.True(source.IsConnectedTo(other));
        for (int i = 0; i < 2; i++)
        {
            manager.HistoryManager.Undo(); Assert.Same(oldEnd, line.End); Attached(line, true);
            Assert.True(nextEnd.IsAvailable); Assert.False(source.IsConnectedTo(other)); Assert.True(source.IsConnectedTo(target));
            manager.HistoryManager.Redo(); Assert.Same(nextEnd, line.End); Attached(line, true);
            Assert.True(oldEnd.IsAvailable); Assert.True(source.IsConnectedTo(other));
        }
        var count = manager.HistoryManager.UndoCount;
        manager.MoveConnectionLine(line, newEndPoint: source.InConnectionPoints[0]);
        manager.MoveConnectionLine(line, newEndPoint: nextEnd);
        Assert.Equal(count, manager.HistoryManager.UndoCount); Assert.Same(nextEnd, line.End); Attached(line, true);
    }
    [Fact]
    public void ConnectedInsertionBatchRollsBackAndKeepsThePriorRedo()
    {
        var (manager, source, target) = Scene(); var line = Connect(manager, source, target);
        manager.HistoryManager.Undo();
        using (manager.HistoryManager.BeginBatch())
        {
            var added = new ProcessNode(); manager.AddComponent(added); Connect(manager, source, added, 0);
        }
        Assert.Equal(new SkiaComponent[] { source, target }, manager.GetComponents()); Assert.Empty(manager.GetLines());
        Assert.True(source.OutConnectionPoints[0].IsAvailable); Assert.Equal(1, manager.HistoryManager.RedoCount);
        manager.HistoryManager.Redo(); Assert.Same(line, Assert.Single(manager.GetLines())); Attached(line, true);
    }
}
