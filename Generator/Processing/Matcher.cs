using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using BeaconColorCalculator.Cache;
using BeaconColorCalculator.Models;

namespace Generator.Processing;

public class Matcher
{
    private readonly float[] _refL;
    private readonly float[] _refA;
    private readonly float[] _refB;
    private readonly int[] _refSequences;
    private readonly int _uniqueCount;

    public Matcher(Dictionary<int, Sequence> uniqueColors)
    {
        _uniqueCount = uniqueColors.Count;
        _refL = new float[_uniqueCount];
        _refA = new float[_uniqueCount];
        _refB = new float[_uniqueCount];
        _refSequences = new int[_uniqueCount];

        var i = 0;
        foreach (var kvp in uniqueColors)
        {
            var rgb = RgbColor.FromIndex(kvp.Key);
            var lab = OklabColor.FromRgb(rgb);

            _refL[i] = lab.L;
            _refA[i] = lab.A;
            _refB[i] = lab.B;
            _refSequences[i] = kvp.Value.Value;
            i++;
        }
    }

    public int[] GenerateMasterCache()
    {
        var masterCache = new int[BeaconCache.CacheSize];

        Console.WriteLine($"Running parallel search for {_uniqueCount} unique colors...");
        var processedCount = 0;

        const int percentStep = BeaconCache.CacheSize / 100;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Parallel.For(0, BeaconCache.CacheSize, rgbIndex =>
        {
            var targetRgb = RgbColor.FromIndex(rgbIndex);
            var targetLab = OklabColor.FromRgb(targetRgb);

            var bestSequence = FindBestMatch(targetLab);
            masterCache[rgbIndex] = bestSequence;

            var current = Interlocked.Increment(ref processedCount);

            if (current % percentStep != 0) return;
            var percent = current / percentStep;

            var elapsed = stopwatch.Elapsed;

            var ticksPerPercent = elapsed.Ticks / (float)percent;
            var estimatedTotal = TimeSpan.FromTicks((long)(ticksPerPercent * 100));
            var timeLeft = estimatedTotal - elapsed;

            Console.WriteLine($@"[{DateTime.Now:HH:mm:ss}] Progress: {percent,3}% | Passed: {elapsed:hh\:mm\:ss} | Left: ~{timeLeft:hh\:mm\:ss}");
        });

        stopwatch.Stop();
        Console.WriteLine($@"Done! Full generation time: {stopwatch.Elapsed:hh\:mm\:ss}");

        return masterCache;
    }

    private int FindBestMatch(OklabColor target)
    {
        ref var pL = ref MemoryMarshal.GetArrayDataReference(_refL);
        ref var pA = ref MemoryMarshal.GetArrayDataReference(_refA);
        ref var pB = ref MemoryMarshal.GetArrayDataReference(_refB);

        var bestIndex = 0;
        var minDistance = float.MaxValue;

        var i = 0;

        // AVX-512
        if (Vector512.IsHardwareAccelerated && _uniqueCount >= 16)
        {
            var vTargetL = Vector512.Create(target.L);
            var vTargetA = Vector512.Create(target.A);
            var vTargetB = Vector512.Create(target.B);

            var vMinDists = Vector512.Create(float.MaxValue);
            var vBestIndices = Vector512<int>.Zero;

            ReadOnlySpan<int> initialIndices = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
            var vCurrentIndices = Vector512.LoadUnsafe(ref MemoryMarshal.GetReference(initialIndices));
            var vStepInt = Vector512.Create(16);

            for (; i <= _uniqueCount - 16; i += 16)
            {
                var vL = Vector512.LoadUnsafe(ref pL, (nuint)i);
                var vA = Vector512.LoadUnsafe(ref pA, (nuint)i);
                var vB = Vector512.LoadUnsafe(ref pB, (nuint)i);

                var dL = vL - vTargetL;
                var dA = vA - vTargetA;
                var dB = vB - vTargetB;

                // DeltaE76
                var dist = dL * dL + dA * dA + dB * dB;


                // Create a mask (where new distances are smaller than old ones)
                var mask = Vector512.LessThan(dist, vMinDists);


                // Replace only those distances and indices where mask = true
                vMinDists = Vector512.ConditionalSelect(mask, dist, vMinDists);
                vBestIndices = Vector512.ConditionalSelect(mask.AsInt32(), vCurrentIndices, vBestIndices);

                vCurrentIndices += vStepInt;
            }

            ExtractMinimum(vMinDists, vBestIndices, ref minDistance, ref bestIndex);
        }
        // AVX2
        else if (Vector256.IsHardwareAccelerated && _uniqueCount >= 8)
        {
            var vTargetL = Vector256.Create(target.L);
            var vTargetA = Vector256.Create(target.A);
            var vTargetB = Vector256.Create(target.B);

            var vMinDists = Vector256.Create(float.MaxValue);
            var vBestIndices = Vector256<int>.Zero;

            var vCurrentIndices = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
            var vStepInt = Vector256.Create(8);

            for (; i <= _uniqueCount - 8; i += 8)
            {
                var vL = Vector256.LoadUnsafe(ref pL, (nuint)i);
                var vA = Vector256.LoadUnsafe(ref pA, (nuint)i);
                var vB = Vector256.LoadUnsafe(ref pB, (nuint)i);

                var dL = vL - vTargetL;
                var dA = vA - vTargetA;
                var dB = vB - vTargetB;

                var dist = dL * dL + dA * dA + dB * dB;

                var mask = Vector256.LessThan(dist, vMinDists);
                vMinDists = Vector256.ConditionalSelect(mask, dist, vMinDists);
                vBestIndices = Vector256.ConditionalSelect(mask.AsInt32(), vCurrentIndices, vBestIndices);

                vCurrentIndices += vStepInt;
            }

            ExtractMinimum(vMinDists, vBestIndices, ref minDistance, ref bestIndex);
        }

        // Tail (if less than 8/16 elements remain) or no SIMD (Scalar)
        for (; i < _uniqueCount; i++)
        {
            var dL = _refL[i] - target.L;
            var dA = _refA[i] - target.A;
            var dB = _refB[i] - target.B;
            var dist = dL * dL + dA * dA + dB * dB;

            if (!(dist < minDistance)) continue;
            minDistance = dist;
            bestIndex = i;
        }

        return _refSequences[bestIndex];
    }

    private static void ExtractMinimum(Vector512<float> dists, Vector512<int> indices, ref float minD, ref int bestIdx)
    {
        for (var k = 0; k < 16; k++)
        {
            if (!(dists[k] < minD)) continue;
            minD = dists[k];
            bestIdx = indices[k];
        }
    }

    private static void ExtractMinimum(Vector256<float> dists, Vector256<int> indices, ref float minD, ref int bestIdx)
    {
        for (var k = 0; k < 8; k++)
        {
            if (!(dists[k] < minD)) continue;
            minD = dists[k];
            bestIdx = indices[k];
        }
    }
}