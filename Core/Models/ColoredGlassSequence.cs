using System.Collections;
using System.Numerics;
using BeaconColorCalculator.Core.Enums;

// 32 bits
// 6 colors
// 27 bits
// 3 (Length) + 4(bits per color) * 6 (colored glasses)
namespace BeaconColorCalculator.Core.Models;

public readonly struct ColoredGlassSequence<T> : IReadOnlyList<GlassColors>, IEquatable<ColoredGlassSequence<T>> where T : struct, IBinaryInteger<T>
{

    public readonly T Value;

    public ColoredGlassSequence(T value)
    {
        Value = value;
    }


    public IEnumerator<GlassColors> GetEnumerator()
    {
        var len = Count;
        for (var i = 0; i < len; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public GlassColors this[int index]
    {
        get
        {
            var shift = 4 + index * 4;
            var masked = (Value >>> shift) & T.CreateTruncating(0b1111);
            return (GlassColors)byte.CreateTruncating(masked);
        }
    }

    public ColoredGlassSequence(ReadOnlySpan<byte> colors)
    {
#if DEBUG
        // short (16 bit): (16 - 3) / 4 = 3
        // int   (32 bit): (32 - 3) / 4 = 7
        // long  (64 bit): (64 - 3) / 4 = 15
        var maxColors = (Unsafe.SizeOf<T>() * 8 - 4) / 4;

        if (colors.Length < 1 || colors.Length > maxColors)
            throw new ArgumentException($"For {typeof(T).Name}, length must be from 1 to {maxColors}");
#endif
        var packedValue = T.CreateTruncating(colors.Length);

        for (var i = 0; i < colors.Length; i++)
        {
            var colorVal = T.CreateTruncating(colors[i]);
            packedValue |= colorVal << (4 + i * 4);
        }

        Value = packedValue;
    }

    public int Count => int.CreateTruncating(Value & T.CreateTruncating(0b1111));

    /// <summary>
    /// Returns a mapping dictionary: Color ID -> Color name (For example: 0 -> "White").
    /// </summary>
    public static Dictionary<byte, string> GetColorMappings()
    {
        var map = new Dictionary<byte, string>(16);
        for (byte i = 0; i < MinecraftBlender.ColorNames.Length; i++)
        {
            map[i] = MinecraftBlender.ColorNames[i];
        }

        return map;
    }

    /// <summary>
    /// Returns an array of color names for the current glass sequence.
    /// </summary>
    public string[] GetColorNames()
    {
        var len = Count;
        var names = new string[len];
        for (var i = 0; i < len; i++)
        {
            names[i] = MinecraftBlender.ColorNames[(byte)this[i]];
        }

        return names;
    }

    public bool Equals(ColoredGlassSequence<T> other)
    {
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        return obj is ColoredGlassSequence<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public static bool operator ==(ColoredGlassSequence<T> left, ColoredGlassSequence<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ColoredGlassSequence<T> left, ColoredGlassSequence<T> right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// "Red -> Orange -> Yellow"
    /// </summary>
    public override string ToString() => string.Join(" -> ", GetColorNames());

    public GlassColors[] ToArray()
    {
        var len = Count;
        var arr = new GlassColors[len];
        for (var i = 0; i < len; i++)
        {
            arr[i] = this[i];
        }

        return arr;
    }
}