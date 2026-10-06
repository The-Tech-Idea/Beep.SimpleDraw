using Beep.Skia;

namespace Beep.SimpleDraw.Core;

public static class SimpleDrawFonts
{
    private static readonly Lazy<bool> Registration = new(() =>
    {
        using var stream = typeof(SimpleDrawFonts).Assembly.GetManifestResourceStream("Beep.SimpleDraw.Core.Fonts.Kalam-Regular.ttf")
            ?? throw new InvalidOperationException("The bundled Kalam font is missing.");
        using var buffer = new MemoryStream(); stream.CopyTo(buffer);
        TypefaceCache.Register("Kalam", buffer.ToArray()); TypefaceCache.DefaultFamily = "Kalam"; return true;
    });
    public static void EnsureLoaded() => _ = Registration.Value;
}
