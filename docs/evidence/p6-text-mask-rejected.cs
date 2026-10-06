using Beep.SimpleDraw.Core;
using Beep.Skia;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Beep.SimpleDraw.Tests;

// Temporary native experiment. Not a production cache or a browser acceptance test.
public class TextMaskTrialTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(.41f, "#e06b56")] [InlineData(.875f, "#e06b56")] [InlineData(1.75f, "#e06b56")]
    [InlineData(.41f, "#80334155")] [InlineData(.875f, "#80334155")] [InlineData(1.75f, "#80334155")]
    public void AlphaOnlyGlyphImagesMustMatchOriginalPathPixels(float zoom, string stroke)
    {
        SimpleDrawFonts.EnsureLoaded();
        using var expected = new SKBitmap(500, 400); using var actual = new SKBitmap(500, 400);
        using var ec = new SKCanvas(expected); using var ac = new SKCanvas(actual);
        foreach (var canvas in new[] { ec, ac })
        { canvas.Clear(SKColor.Parse("#ffd9e1e7")); canvas.Translate(12.375f, 7.125f); canvas.Scale(zoom); canvas.ClipRect(new SKRect(15, 10, 400, 300)); }
        using var font = new SKFont(TypefaceCache.GetRequired("Kalam"), 24)
        { Hinting = SKFontHinting.None, Subpixel = true, LinearMetrics = true, Edging = SKFontEdging.Antialias };
        using var paint = new SKPaint { Color = SKColor.Parse(stroke), IsAntialias = true };
        using var white = new SKPaint { Color = SKColors.White, IsAntialias = true };
        var text = "Cache WWW iii idea";
        var glyphs = font.GetGlyphs(text); var positions = font.GetGlyphPositions(glyphs, new(15.25f, 65.125f));
        for(int i=0;i<glyphs.Length;i++)
        {
            using var path = font.GetGlyphPath(glyphs[i]); if(path is null) continue;
            ec.Save(); ec.Translate(positions[i]); ec.DrawPath(path,paint); ec.Restore();
            using var mask = new SKBitmap(new SKImageInfo(500,400,SKColorType.Alpha8,SKAlphaType.Premul));
            using(var mc=new SKCanvas(mask)) { mc.Clear(SKColors.Transparent); mc.SetMatrix(ac.TotalMatrix); mc.Translate(positions[i]); mc.DrawPath(path,white); }
            using var image = SKImage.FromBitmap(mask);
            ac.Save(); ac.ResetMatrix(); ac.DrawImage(image,0,0,new SKSamplingOptions(SKFilterMode.Nearest,SKMipmapMode.None),paint); ac.Restore();
        }
        var a=expected.Bytes; var b=actual.Bytes;
        int changed=0,max=0; for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) {changed++; max=Math.Max(max,Math.Abs(a[i]-b[i]));}
        output.WriteLine($"Changed channels={changed}; max difference={max}; zoom={zoom}; stroke={stroke}");
        Assert.Equal(a,b);
    }
}
