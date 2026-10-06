using Beep.SimpleDraw.Core;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class ViewportScrollTests
{
    [Theory]
    [InlineData(0,120,false,50,-40)]
    [InlineData(75,0,false,-25,80)]
    [InlineData(20,30,false,30,50)]
    [InlineData(0,120,true,-70,80)]
    [InlineData(20,30,true,30,50)]
    public void ScrollingMovesTheViewportWithoutZoomOrDocumentEdits(float x,float y,bool shift,float expectedX,float expectedY)
    {
        var session = new DrawingSession(); session.ResizeViewport(1000,700);
        session.InsertTemplate("Beep.Skia.UML.UMLClass");
        session.Manager.PanOffset = new(50,80); session.Manager.Zoom = 2;
        var native = SimpleDrawDocument.Serialize(session.Manager);
        session.ScrollViewport(new(200,300),x,y,horizontal:shift);
        Assert.Equal(new SKPoint(expectedX,expectedY),session.Manager.PanOffset);
        Assert.Equal(2,session.Manager.Zoom);
        Assert.Equal(native,SimpleDrawDocument.Serialize(session.Manager));
    }
    [Fact]
    public void ModifiedWheelAnchorsZoomAtThePointerAndZeroOrInvalidInputDoesNothing()
    {
        var session = new DrawingSession(); var pointer = new SKPoint(250,190);
        var anchor = session.ScreenToWorld(pointer);
        session.ScrollViewport(pointer,0,-100,zoom:true);
        Assert.True(session.Manager.Zoom > 1);
        var after = session.ScreenToWorld(pointer);
        Assert.InRange(Math.Abs(after.X-anchor.X),0,.001f); Assert.InRange(Math.Abs(after.Y-anchor.Y),0,.001f);
        var zoom = session.Manager.Zoom; var pan = session.Manager.PanOffset;
        session.ScrollViewport(pointer,0,0,zoom:true);
        session.ScrollViewport(pointer,float.NaN,20);
        Assert.Equal(zoom,session.Manager.Zoom); Assert.Equal(pan,session.Manager.PanOffset);
    }
}
