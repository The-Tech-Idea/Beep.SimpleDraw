using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Diagnostics;
using Beep.Skia.Components;
using Beep.Skia.Model;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class RendererStressTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void All5000UseTheSharedRendererAndHistoryWhileNativeSerializationStillRefuses(bool mixed)
    {
        using var scene = new RendererStressScene(mixed);
        var components = scene.Manager.GetComponents();
        Assert.Equal(5000, components.Count);
        Assert.Equal(5000, components.Select(c => c.Id).Distinct().Count());
        Assert.Equal(mixed ? 7 : 1, components.Cast<SketchShape>().Select(s => s.Kind).Distinct().Count());
        scene.Fit(1200, 440);
        var original = Paint(scene, manual: false);
        Assert.Equal(Paint(scene, manual: true), original);
        Assert.Equal(original, Paint(scene, manual: false));
        var startX = scene.EditedShape.X;
        scene.Edit(true);
        Assert.Equal(startX + 1, scene.EditedShape.X);
        Assert.Equal(1, scene.Manager.HistoryManager.UndoCount);
        scene.Manager.HistoryManager.Undo();
        Assert.Equal(startX, scene.EditedShape.X);
        Assert.Equal(original, Paint(scene, manual: false));
        scene.Manager.HistoryManager.Redo();
        Assert.Equal(startX + 1, scene.EditedShape.X);
        Assert.Throws<InvalidDataException>(() => SimpleDrawDocument.Serialize(scene.Manager));
        scene.Dispose(); scene.Dispose();
        Assert.Empty(scene.Manager.GetComponents());
        Assert.Equal(0, scene.Manager.HistoryManager.UndoCount);
        Assert.Throws<ObjectDisposedException>(() => scene.Edit(true));
    }

    private static byte[] Paint(RendererStressScene scene, bool manual)
    {
        using var surface = SKSurface.Create(new SKImageInfo(1200, 440));
        var canvas = surface.Canvas; canvas.Clear(SKColors.White);
        if (!manual) scene.Manager.Draw(canvas);
        else
        {
            canvas.Save(); canvas.Translate(scene.Manager.PanOffset); canvas.Scale(scene.Manager.Zoom);
            var context = new DrawingContext { Bounds = canvas.LocalClipBounds, Zoom = scene.Manager.Zoom, PanOffset = scene.Manager.PanOffset };
            foreach (var component in scene.Manager.GetComponents()) { component.Update(context); component.Draw(canvas, context); }
            canvas.Restore();
        }
        using var image = surface.Snapshot(); using var bytes = image.Encode(SKEncodedImageFormat.Png, 100);
        return bytes.ToArray();
    }
}
