using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeaconColorCalculator.Core.Models;
using ZstdSharp;

namespace BeaconColorCalculator.Core.Cache;

public class BeaconStorage
{
    // KD-Trees (2-5 layers)
    public Dictionary<int, OklabKdTree<short>.KdNode[]> KdTreesShort { get; } = new();
    public Dictionary<int, OklabKdTree<int>.KdNode[]> KdTreesInt { get; } = new();

    // LUTs (6-9 layers)
    public Dictionary<int, int[]> LutsInt { get; } = new();
    public Dictionary<int, long[]> LutsLong { get; } = new();

    private enum StructType : byte
    {
        KdTree = 0,
        Lut = 1
    }

    private enum DataType : byte
    {
        Short = 0,
        Int = 1,
        Long = 2
    }

    /// <summary>
    /// Saves all accumulated trees and LUTs into one compressed .zst file
    /// </summary>
    public void SaveAll(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zstd = new CompressionStream(fs, level: 3); // level 19
        using var writer = new BinaryWriter(zstd);

        // Count and record the total number of blocks (to know how many to read)
        var totalBlocks = KdTreesShort.Count + KdTreesInt.Count + LutsInt.Count + LutsLong.Count;
        writer.Write(totalBlocks);

        foreach (var kvp in KdTreesShort)
            WriteBlock(kvp.Key, StructType.KdTree, DataType.Short, kvp.Value);

        foreach (var kvp in KdTreesInt)
            WriteBlock(kvp.Key, StructType.KdTree, DataType.Int, kvp.Value);

        foreach (var kvp in LutsInt)
            WriteBlock(kvp.Key, StructType.Lut, DataType.Int, kvp.Value);

        foreach (var kvp in LutsLong)
            WriteBlock(kvp.Key, StructType.Lut, DataType.Long, kvp.Value);
        return;

        void WriteBlock<T>(int layer, StructType sType, DataType dType, ReadOnlySpan<T> data) where T : unmanaged
        {
            var bytes = MemoryMarshal.AsBytes(data);
            writer.Write(layer);
            writer.Write((byte)sType);
            writer.Write((byte)dType);
            writer.Write(bytes.Length);
            zstd.Write(bytes);
        }
    }

    /// <summary>
    /// Loads all trees and LUTs from a single .zst file
    /// </summary>
    public static BeaconStorage Load(string filePath)
    {
        var storage = new BeaconStorage();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zstd = new DecompressionStream(fs);
        using var reader = new BinaryReader(zstd);

        var totalBlocks = reader.ReadInt32();

        for (var i = 0; i < totalBlocks; i++)
        {
            var layer = reader.ReadInt32();
            var sType = (StructType)reader.ReadByte();
            var dType = (DataType)reader.ReadByte();
            var byteLength = reader.ReadInt32();

            switch (sType)
            {
                case StructType.KdTree when dType == DataType.Short:
                    storage.KdTreesShort[layer] = ReadArray<OklabKdTree<short>.KdNode>();
                    break;
                case StructType.KdTree:
                {
                    if (dType == DataType.Int) storage.KdTreesInt[layer] = ReadArray<OklabKdTree<int>.KdNode>();
                    break;
                }
                case StructType.Lut when dType == DataType.Int:
                    storage.LutsInt[layer] = ReadArray<int>();
                    break;
                case StructType.Lut:
                {
                    if (dType == DataType.Long) storage.LutsLong[layer] = ReadArray<long>();
                    break;
                }
            }

            continue;

            T[] ReadArray<T>() where T : unmanaged
            {
                var arrayLength = byteLength / Unsafe.SizeOf<T>();
                var array = GC.AllocateUninitializedArray<T>(arrayLength, pinned: true);
                var byteSpan = MemoryMarshal.AsBytes(array.AsSpan());
                zstd.ReadExactly(byteSpan);
                return array;
            }
        }

        return storage;
    }
}