namespace BeaconColorCalculator.Core.Models;

public readonly record struct RgbColor(byte R, byte G, byte B)
{
    /// <summary>
    /// Converts RGB to array cache index (from 0 to 16777215)
    /// </summary>
    public int ToIndex() => (R << 16) | (G << 8) | B;

    /// <summary>
    /// Creates color from cache index
    /// </summary>
    public static RgbColor FromIndex(int index) => new(
        (byte)((index >> 16) & 0xFF),
        (byte)((index >> 8) & 0xFF),
        (byte)(index & 0xFF)
    );

    public string ToHexString()
    {
        return $"#{R:X2}{G:X2}{B:X2}";
    }

    /// <summary>
    /// Converts from Oklab to RGB float values (Linear -> sRGB).
    /// Returns floats because the color might be out of sRGB gamut.
    /// </summary>
    public static (float R, float G, float B) FromOklabToSrgb(OklabColor oklab)
    {
        var l = MathF.Pow(oklab.L + 0.3963377774F * oklab.A + 0.2158037573F * oklab.B, 3);
        var m = MathF.Pow(oklab.L - 0.1055613458F * oklab.A - 0.0638541728F * oklab.B, 3);
        var s = MathF.Pow(oklab.L - 0.0894841775F * oklab.A - 1.291485548F * oklab.B, 3);

        var r = GammaCorrection(4.0767416621F * l - 3.3077115913F * m + 0.2309699292F * s);
        var g = GammaCorrection(-1.2684380046F * l + 2.6097574011F * m - 0.3413193965F * s);
        var b = GammaCorrection(-0.0041960863F * l - 0.7034186147F * m + 1.707614701F * s);
        return (r * 255F, g * 255F, b * 255F);
    }

    public static RgbColor FromOklab(OklabColor oklab)
    {
        var sRgb = FromOklabToSrgb(oklab);
        return new RgbColor(ClampToRgb(sRgb.R), ClampToRgb(sRgb.G), ClampToRgb(sRgb.B));
    }

    private static byte ClampToRgb(float value)
    {
        if (value < 0.0F) value = 0.0F;
        if (value > 1.0F) value = 1.0F;
        return (byte)MathF.Round(value * 255.0F);
    }

    private static float GammaCorrection(float c)
    {
        return c <= 0.0031308f
            ? 12.92f * c
            : 1.055f * MathF.Pow(c, 1.0f / 2.4f) - 0.055f;
    }
}