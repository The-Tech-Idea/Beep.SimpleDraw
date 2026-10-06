using System.Text;
using System.Text.Json.Nodes;
using Beep.SimpleDraw.Core;
using Beep.Skia.Components;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class NativeBoundaryTests
{
    [Fact]
    public void TwoThousandObjectsRoundtripAndCountOverflowBelowByteLimitKeepsTheActiveScene()
    {
        var board = new DrawingSession();
        for (int i = 0; i < SimpleDrawDocument.MaxShapes; i++)
            board.Manager.AddComponent(new SketchShape { Name = "Shape", X = i % 50 * 20, Y = i / 50 * 20, Width = 12, Height = 10, SketchSeed = i + 1 });
        var json = SimpleDrawDocument.Serialize(board.Manager);
        Assert.True(Encoding.UTF8.GetByteCount(json) <= SimpleDrawDocument.MaxBytes);
        var restored = new DrawingSession(); restored.OpenDocument(json);
        Assert.Equal(2000, restored.Manager.GetComponents().Count);
        var overflow = JsonNode.Parse(json)!; var components = overflow["diagram"]!["components"]!.AsArray();
        var extra = components[0]!.DeepClone(); extra["id"] = Guid.NewGuid(); components.Add(extra);
        var compact = overflow.ToJsonString(); Assert.True(Encoding.UTF8.GetByteCount(compact) < SimpleDrawDocument.MaxBytes);
        var first = restored.Manager.GetComponents()[0];
        Assert.Throws<InvalidDataException>(() => restored.OpenDocument(compact));
        Assert.Equal(2000, restored.Manager.GetComponents().Count); Assert.Same(first, restored.Manager.GetComponents()[0]);
        restored.Tool = DrawingTool.Rectangle; restored.PointerDown(new SKPoint(5,5));
        Assert.False(restored.HasGesture); Assert.Contains("2000", restored.GestureNotice);
        foreach (var scene in new[] { board, restored }) foreach (var shape in scene.Manager.GetComponents()) shape.Dispose();
    }
}
