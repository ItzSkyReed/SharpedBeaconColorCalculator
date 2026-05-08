using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BeaconColorCalculator.Core.Enums;
using BeaconColorCalculator.Core.Models;

namespace BeaconColorCalculator.Core;

public static class MinecraftBlender
{
    private static readonly Vector128<float>[] BaseVectors = new Vector128<float>[16];

    private static readonly Vector128<float> HalfVector = Vector128.Create(0.5f);
    private static readonly Vector128<float> ByteScaleVector = Vector128.Create(255f);

    public static readonly uint[] HexColors =
    [
        0xf9fffe, 0x9d9d97, 0x474f52, 0x1d1d21, // White, LightGray, Gray, Black
        0x835432, 0xb02e26, 0xf9801d, 0xfed83d, // Brown, Red, Orange, Yellow
        0x80c71f, 0x5e7c16, 0x169c9c, 0x3ab3da, // Lime, Green, Cyan, LightBlue
        0x3c44aa, 0x8932b8, 0xc74ebd, 0xf38baa  // Blue, Purple, Magenta, Pink
    ];

    public static readonly string[] ColorNames =
    [
        "White", "LightGray", "Gray", "Black",
        "Brown", "Red", "Orange", "Yellow",
        "Lime", "Green", "Cyan", "LightBlue",
        "Blue", "Purple", "Magenta", "Pink"
    ];

    static MinecraftBlender()
    {
        for (var i = 0; i < 16; i++)
        {
            var r = ((HexColors[i] >> 16) & 0xFF) / 255f;
            var g = ((HexColors[i] >> 8) & 0xFF) / 255f;
            var b = (HexColors[i] & 0xFF) / 255f;

            BaseVectors[i] = Vector128.Create(r, g, b, 0f);
        }
    }


    /// <summary>
    /// Mixes the glass sequence according to Minecraft rules.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RgbColor Blend(ReadOnlySpan<GlassColors> sequence)
    {
        if (sequence.IsEmpty) return default;

        ref var sequenceRef = ref MemoryMarshal.GetReference(sequence);
        ref var baseVectorsRef = ref MemoryMarshal.GetArrayDataReference(BaseVectors);
        var length = sequence.Length;

        var firstColor = (int)sequenceRef;
        var total = Unsafe.Add(ref baseVectorsRef, firstColor);

        for (var i = 1; i < length; i++)
        {
            var colorId = (int)Unsafe.Add(ref sequenceRef, i);
            var nextColor = Unsafe.Add(ref baseVectorsRef, colorId);

            total = (total + nextColor) * HalfVector;
        }

        // Multiply by 255 before converting to bytes
        total *= ByteScaleVector;


        return new RgbColor(
            (byte)total.ToScalar(),
            (byte)total.GetElement(1),
            (byte)total.GetElement(2)
        );
    }
}