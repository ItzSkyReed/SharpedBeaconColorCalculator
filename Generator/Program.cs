using System.Diagnostics;
using BeaconColorCalculator.Cache;
using BeaconColorCalculator.Models;
using Generator.Processing;

const string cachePath = "beacon_cache.bin.gz"; // default path

// Check if the generated cache file already exists
if (File.Exists(cachePath))
{
    Console.WriteLine($"Found existing cache file '{cachePath}'. Starting load...");

    var cache = new BeaconCache(cachePath);


    var target = new RgbColor(115, 81, 132); // Пример сиреневого (Lilac)

    var seq = cache.GetSequence(target);
    for (var i = 0; i < seq.Length; i++)
    {
        var colorId = seq.GetColor(i);
        var name = MinecraftBlender.ColorNames[colorId];
        Console.WriteLine($"Block {i + 1}: {name}");
    }
}
else
{
    Console.WriteLine($"File '{cachePath}' not found. Starting full generation...");

    var totalTime = Stopwatch.StartNew();

    // Combinatorics
    Console.WriteLine("\n[1/3] Generating all glass combinations from 1 to 6...");
    var uniqueColors = Combinatorics.GenerateUniqueColors();

    // SIMD Matching
    Console.WriteLine("\n[2/3] Preparing SIMD and searching for best matches...");
    var matcher = new Matcher(uniqueColors);
    var masterCache = matcher.GenerateMasterCache();

    // Saving to disk
    Console.WriteLine("\n[3/3] Saving cache to disk...");
    BeaconCache.Save(cachePath, masterCache);

    totalTime.Stop();

    Console.WriteLine($"\nSuccessfully completed! Cache saved to file: {cachePath}");
    Console.WriteLine($"Total generator execution time: {totalTime.Elapsed}");
}