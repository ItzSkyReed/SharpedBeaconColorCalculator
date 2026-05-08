using System.Diagnostics;
using System.Numerics;
using BeaconColorCalculator.Core.Cache;
using BeaconColorCalculator.Core.Models;
using BeaconColorCalculator.Generator.Processing;

namespace BeaconColorCalculator.Generator;

public static class Pipeline
{
    public static void GenerateCaches(string outputPath)
    {
        var storage = new BeaconStorage();
        Console.WriteLine("Starting generation...");

        for (var layers = 2; layers <= 8; layers++)
        {
            Console.WriteLine($"\n Processing layer {layers}");

            switch (layers)
            {
                // KD-Tree Phase
                case <= 3:
                {
                    var tree = BuildKdTree<short>(layers);
                    storage.KdTreesShort.Add(layers, tree.GetNodesSpan().ToArray());
                    break;
                }
                case <= 5:
                {
                    var tree = BuildKdTree<int>(layers);
                    storage.KdTreesInt.Add(layers, tree.GetNodesSpan().ToArray());
                    break;
                }
                case <= 7:
                {
                    var lut = BuildLut<int>(layers);
                    storage.LutsInt.Add(layers, lut);
                    break;
                }
                default:
                {
                    var lut = BuildLut<long>(layers);
                    storage.LutsLong.Add(layers, lut);
                    break;
                }
            }
        }

        var sw = Stopwatch.StartNew();
        Console.WriteLine("\n Saving to .zst archive ");
        var archivePath = Path.Combine(outputPath, "beacon_caches.zst");
        storage.SaveAll(archivePath);
        Console.WriteLine($"Saved successfully to: {archivePath}, took {sw.ElapsedMilliseconds} ms.");
    }


    /// <summary>
    /// Generates unique colors and builds a KD-Tree.
    /// </summary>
    private static OklabKdTree<T> BuildKdTree<T>(int layers) where T : unmanaged, IBinaryInteger<T>
    {
        var sw = Stopwatch.StartNew();

        var uniqueColorsDict = Combinatorics.GenerateUniqueColors<T>(layers);

        var colorEntries = uniqueColorsDict.Select(kvp =>
        {
            var rgb = RgbColor.FromIndex(kvp.Key);
            var lab = OklabColor.FromRgb(rgb);


            return new OklabKdTree<T>.ColorEntry { L = lab.L, A = lab.A, B = lab.B, Sequence = kvp.Value };
        }).ToList();

        Console.WriteLine($"[KD-Tree] Unique colors: {colorEntries.Count}. Generating KD-Tree...");

        var tree = new OklabKdTree<T>(colorEntries);

        sw.Stop();
        Console.WriteLine($"[KD-Tree] Built in {sw.ElapsedMilliseconds} ms.");

        return tree;
    }

    /// <summary>
    /// Builds a 16.7M elements Lookup Table (LUT) by querying a newly generated KD-Tree.
    /// </summary>
    private static T[] BuildLut<T>(int layers) where T : unmanaged, IBinaryInteger<T>
    {
        var tree = BuildKdTree<T>(layers);

        var sw = Stopwatch.StartNew();
        Console.WriteLine("[Strategy] Layers > KDTreeMax. Building colors LUT...");

        const int cacheSize = 16_777_216;
        var lutCache = GC.AllocateUninitializedArray<T>(cacheSize, pinned: true);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2)
        };

        Parallel.For(0L, cacheSize, parallelOptions, i =>
        {
            var r = (byte)(i >> 16);
            var g = (byte)(i >> 8);
            var b = (byte)i;

            var rgb = new RgbColor(r, g, b);
            var lab = OklabColor.FromRgb(rgb);
            var searchColor = new OklabColor(lab.L, lab.A, lab.B);

            var nearestEntry = tree.FindNearest(searchColor);

            lutCache[i] = nearestEntry.Value;
        });

        sw.Stop();
        Console.WriteLine($"[LUT] Generation completed in {sw.ElapsedMilliseconds} ms.");

        return lutCache;
    }
}