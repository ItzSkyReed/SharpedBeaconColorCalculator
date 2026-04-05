namespace BeaconColorCalculator.Models;

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
}