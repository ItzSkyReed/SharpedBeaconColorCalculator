namespace BeaconColorCalculator.Models;

public readonly struct Sequence
{
    /// <summary>
    /// Raw value in int-array cache.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initialization from raw cache value
    /// </summary>
    public Sequence(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Initialization from glass array (from generation process).
    /// </summary>
    public Sequence(ReadOnlySpan<byte> colors)
    {
        if (colors.Length is < 1 or > 6)
            throw new ArgumentException("The length must be from 1 to 6 bytes");

        // First 3 bits contains length (from 1 to 6)
        var packedValue = colors.Length;

        // pack colors, every 4 bits
        for (var i = 0; i < colors.Length; i++)
        {
            // took colors (0-15) and shift it: 3 + (i * 4)
            packedValue |= colors[i] << (3 + i * 4);
        }

        Value = packedValue;
    }

    /// <summary>
    /// Get a representation of the sequence.
    /// Read the first 3 bits (mask 0b111 = 7).
    /// </summary>
    public int Length => Value & 0b111;

    /// <summary>
    /// Get the glass color by its index (from 0 to Length - 1).
    /// </summary>
    public byte GetColor(int index)
    {
        // Shift the required number of bits to the right and apply the mask 0b1111 (15)
        return (byte)((Value >> (3 + index * 4)) & 0b1111);
    }

    public byte[] ToArray()
    {
        var len = Length;
        var arr = new byte[len];
        for (var i = 0; i < len; i++)
        {
            arr[i] = GetColor(i);
        }
        return arr;
    }

    public override string ToString() => string.Join(" -> ", ToArray());
}