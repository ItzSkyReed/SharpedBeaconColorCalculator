using BeaconColorCalculator.Models;

namespace Generator.Processing;

public static class Combinatorics
{
    /// <summary>
    /// Generates all possible combinations of glasses from 1 to 6 and returns
    /// only unique resulting colors with their shortest sequences.
    /// </summary>
    public static Dictionary<int, Sequence> GenerateUniqueColors()
    {
        // Dictionary for storing results.
        // Key - encoded RGB (ToIndex()), Value - Sequence structure
        var uniqueColors = new Dictionary<int, Sequence>(65536);

        Span<byte> buffer = stackalloc byte[6];

        // Go from the shortest combinations (1 glass) to the longest (6 glasses)
        // This ensures that the first combination found for a color is always the shortest!
        for (var length = 1; length <= 6; length++)
        {
            GeneratePermutations(buffer[..length], 0, uniqueColors);
        }

        Console.WriteLine($"Generation finished. Unique colors found: {uniqueColors.Count}");
        return uniqueColors;
    }

    /// <summary>
    ///  Recursive method for fast iteration using Span..
    /// </summary>
    private static void GeneratePermutations(
        Span<byte> buffer,
        int depth,
        Dictionary<int, Sequence> uniqueColors)
    {
        // Basic case of recursion: we filled the buffer with the required length
        if (depth == buffer.Length)
        {
            var resultColor = MinecraftBlender.Blend(buffer);
            var rgbIndex = resultColor.ToIndex();

            // If we haven't received this color yet, keep it!
            // If we have, skip it, since the old combination was shorter (or the same length)
            uniqueColors.TryAdd(rgbIndex, new Sequence(buffer));

            return;
        }

        for (byte colorId = 0; colorId < 16; colorId++)
        {
            buffer[depth] = colorId;
            GeneratePermutations(buffer, depth + 1, uniqueColors);
        }
    }
}