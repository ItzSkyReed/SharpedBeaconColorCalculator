using System.IO.Compression; // Required for GZipStream
using System.Runtime.InteropServices;
using BeaconColorCalculator.Models;

namespace BeaconColorCalculator.Cache;

public class BeaconCache
{
    // Cache size for 16.7 million of colors (256^3)
    public const int CacheSize = 16_777_216;

    private readonly int[] _cache;

    /// <summary>
    /// Initializing cache from GZIP compressed binary file.
    /// </summary>
    public BeaconCache(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        _cache = GC.AllocateUninitializedArray<int>(CacheSize, pinned: true);

        var cacheBytes = MemoryMarshal.AsBytes(_cache.AsSpan());

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);

        gz.ReadExactly(cacheBytes);
    }

    /// <summary>
    /// Saves generated cache array into a GZIP compressed file.
    /// </summary>
    public static void Save(string path, int[] generatedCache)
    {
        if (generatedCache.Length != CacheSize)
            throw new ArgumentException("Wrong array size.");

        ReadOnlySpan<byte> cacheBytes = MemoryMarshal.AsBytes(generatedCache.AsSpan());

        // Create file and wrap it in GZipStream for compression
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

        using var gz = new GZipStream(fs, CompressionLevel.SmallestSize);

        gz.Write(cacheBytes);
    }

    /// <summary>
    /// O(1) cache access
    /// </summary>
    public Sequence GetSequence(RgbColor color)
    {
        var index = color.ToIndex();
        var rawValue = _cache[index];
        return new Sequence(rawValue);
    }

    /// <summary>
    /// get cache access from R, G, B
    /// </summary>
    public Sequence GetSequence(byte r, byte g, byte b)
    {
        var index = (r << 16) | (g << 8) | b;
        return new Sequence(_cache[index]);
    }
}