using System.Runtime.Intrinsics;
using System.Numerics;
using BeaconColorCalculator.Core;
using BeaconColorCalculator.Core.Models;

namespace BeaconColorCalculator.Generator.Processing;

public static class Combinatorics
{
    private static readonly Vector128<float>[] BaseVectors = new Vector128<float>[16];
    private static readonly Vector128<float> HalfVector = Vector128.Create(0.5f);
    private static readonly Vector128<float> ByteScaleVector = Vector128.Create(255f);

    static Combinatorics()
    {
        for (var i = 0; i < 16; i++)
        {
            var r = ((MinecraftBlender.HexColors[i] >> 16) & 0xFF) / 255f;
            var g = ((MinecraftBlender.HexColors[i] >> 8) & 0xFF) / 255f;
            var b = (MinecraftBlender.HexColors[i] & 0xFF) / 255f;
            BaseVectors[i] = Vector128.Create(r, g, b, 0f);
        }
    }

    public static Dictionary<int, ColoredGlassSequence<T>> GenerateUniqueColors<T>(int maxGlasses = 6)
        where T : struct, IBinaryInteger<T>
    {
        if (maxGlasses is < 1 or > 15) throw new ArgumentOutOfRangeException(nameof(maxGlasses));

        var lookup = new ColoredGlassSequence<T>[16777216];
        var found = new int[16777216];
        var uniqueCount = 0;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2)
        };

        for (var length = 1; length <= maxGlasses; length++)
        {
            // If the length is 1, we have only 16 tasks. If it's more, we split it into 256 tasks to balance the load.
            var tasks = length == 1 ? 16 : 256;

            var localLength = length;
            Parallel.For(0, tasks, parallelOptions, index =>
            {
                // each thread should have its own buffer so they don't overwrite each other's colors
                Span<byte> localBuffer = stackalloc byte[maxGlasses];

                if (localLength == 1)
                {
                    var colorId = (byte)index;
                    localBuffer[0] = colorId;
                    GeneratePermutations(localBuffer, 1, localLength, BaseVectors[colorId], lookup, found, ref uniqueCount);
                }
                else
                {
                    var c1 = (byte)(index / 16);
                    var c2 = (byte)(index % 16);

                    localBuffer[0] = c1;
                    localBuffer[1] = c2;

                    var color1 = BaseVectors[c1];
                    var color2 = (color1 + BaseVectors[c2]) * HalfVector;

                    GeneratePermutations(localBuffer, 2, localLength, color2, lookup, found, ref uniqueCount);
                }
            });
        }

        Console.WriteLine($"Generation finished. Unique colors found: {uniqueCount}");

        var result = new Dictionary<int, ColoredGlassSequence<T>>(uniqueCount);
        for (var i = 0; i < found.Length; i++)
        {
            if (found[i] == 1)
            {
                result.Add(i, lookup[i]);
            }
        }

        return result;
    }

    private static void GeneratePermutations<T>(
        Span<byte> buffer,
        int depth,
        int targetLength,
        Vector128<float> currentColor,
        ColoredGlassSequence<T>[] lookup,
        int[] found,
        ref int uniqueCount)
        where T : struct, IBinaryInteger<T>
    {
        if (depth == targetLength)
        {
            var finalColor = currentColor * ByteScaleVector;
            var r = (int)finalColor.ToScalar();
            var g = (int)finalColor.GetElement(1);
            var b = (int)finalColor.GetElement(2);

            var rgbIndex = (r << 16) | (g << 8) | b;


            if (found[rgbIndex] != 0) return;

            if (Interlocked.CompareExchange(ref found[rgbIndex], 1, 0) != 0) return;

            lookup[rgbIndex] = new ColoredGlassSequence<T>(buffer[..targetLength]);
            Interlocked.Increment(ref uniqueCount);

            return;
        }

        for (byte colorId = 0; colorId < 16; colorId++)
        {
            buffer[depth] = colorId;
            var nextColor = (currentColor + BaseVectors[colorId]) * HalfVector;
            GeneratePermutations(buffer, depth + 1, targetLength, nextColor, lookup, found, ref uniqueCount);
        }
    }
}