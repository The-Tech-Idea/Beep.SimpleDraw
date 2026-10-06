using Beep.Skia.WellLogs;
using SkiaSharp;
using Xunit;

namespace Beep.SimpleDraw.Tests;

public class WellLogBrushBoundsTests
{
    public static IEnumerable<object[]> Brushes => WellLogBrushLibrary.Definitions.Keys.Select(key => new object[] { key });

    [Theory, MemberData(nameof(Brushes))]
    public void EveryBrushContainsInkAndRestoresCallerCanvasState(string key)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(160,160));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate(13,9);
        canvas.ClipRect(new SKRect(0,0,140,140));
        var matrix = canvas.TotalMatrix; var saveCount = canvas.SaveCount;
        WellLogBrushLibrary.DrawPattern(canvas,new SKRect(40,40,80,100),key);
        Assert.Equal(matrix,canvas.TotalMatrix); Assert.Equal(saveCount,canvas.SaveCount);
        var inside = 0; var outside = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x,y).Alpha != 0)
                {
                    if (x >= 53 && x < 93 && y >= 49 && y < 109) inside++;
                    else outside++;
                }
        Assert.True(inside >= 2000,$"{key} must retain its interior fill.");
        Assert.Equal(0,outside);
        using var paint = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(new SKRect(0,0,5,5),paint);
        Assert.Equal(SKColors.Red,bitmap.GetPixel(14,10));
        canvas.DrawRect(new SKRect(141,141,147,147),paint);
        Assert.Equal((byte)0,bitmap.GetPixel(155,151).Alpha);
    }
}
