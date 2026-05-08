using System.Numerics;
using System.Runtime.CompilerServices;

namespace BeaconColorCalculator.Core.Models;

public class OklabKdTree<T> where T : struct, IBinaryInteger<T>
{
    public struct ColorEntry(ColoredGlassSequence<T> sequence)
    {
        public float L, A, B;
        public ColoredGlassSequence<T> Sequence = sequence;
    }

    public struct KdNode
    {
        public float L, A, B; // 12 byte
        public ColoredGlassSequence<T> Sequence; // 2-8 byte
        public int Left; // 4 byte
        public int Right; // 4 byte
    }


    private readonly KdNode[] _nodes;
    private int _nodeCount;

    public OklabKdTree(List<ColorEntry> uniqueColors)
    {
        _nodes = new KdNode[uniqueColors.Count];
        _nodeCount = 0;

        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(uniqueColors);
        BuildTree(span, 0);
    }

    public OklabKdTree(KdNode[] nodes, int nodeCount)
    {
        _nodes = nodes;
        _nodeCount = nodeCount;
    }

    public ReadOnlySpan<KdNode> GetNodesSpan() => new(_nodes, 0, _nodeCount);

    /// <summary>
    /// Recursive Tree Building
    /// </summary>
    private int BuildTree(Span<ColorEntry> points, int depth)
    {
        if (points.Length == 0) return -1;

        var axis = depth % 3;
        var medianIndex = points.Length / 2;

        QuickSelect(points, 0, points.Length - 1, medianIndex, axis);

        var nodeIndex = _nodeCount++;
        ref var node = ref _nodes[nodeIndex];

        var medianPoint = points[medianIndex];
        node.L = medianPoint.L;
        node.A = medianPoint.A;
        node.B = medianPoint.B;
        node.Sequence = medianPoint.Sequence;

        node.Left = BuildTree(points[..medianIndex], depth + 1);
        node.Right = BuildTree(points[(medianIndex + 1)..], depth + 1);

        return nodeIndex;
    }

    /// <summary>
    /// Поиск ближайшего цвета к целевому (O(log N))
    /// </summary>
    public ColoredGlassSequence<T> FindNearest(OklabColor target)
    {
        if (_nodeCount == 0) throw new InvalidOperationException("Tree is empty");

        var bestDistSq = float.MaxValue;
        ColoredGlassSequence<T> bestSequence = default;

        SearchNearest(0, target.L, target.A, target.B, 0, ref bestDistSq, ref bestSequence);

        return bestSequence;
    }

    private void SearchNearest(int nodeIndex, float tL, float tA, float tB, int depth, ref float bestDistSq, ref ColoredGlassSequence<T> bestSequence)
    {
        while (true)
        {
            if (nodeIndex == -1) return;

            ref var node = ref _nodes[nodeIndex];

            var dL = node.L - tL;
            var dA = node.A - tA;
            var dB = node.B - tB;
            var distSq = dL * dL + dA * dA + dB * dB;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestSequence = node.Sequence;
            }

            var axis = depth % 3;
            var nodeAxisVal = axis switch
            {
                0 => node.L,
                1 => node.A,
                _ => node.B
            };
            var targetAxisVal = axis switch
            {
                0 => tL,
                1 => tA,
                _ => tB
            };

            var firstPath = targetAxisVal < nodeAxisVal ? node.Left : node.Right;
            var secondPath = targetAxisVal < nodeAxisVal ? node.Right : node.Left;

            SearchNearest(firstPath, tL, tA, tB, depth + 1, ref bestDistSq, ref bestSequence);

            var axisDist = targetAxisVal - nodeAxisVal;
            if (axisDist * axisDist < bestDistSq)
            {
                nodeIndex = secondPath;
                depth += 1;
                continue;
            }

            break;
        }
    }

    /// <summary>
    /// median search in O(N) on average
    /// </summary>
    private static void QuickSelect(Span<ColorEntry> arr, int left, int right, int k, int axis)
    {
        while (left < right)
        {
            var pivotIndex = Partition(arr, left, right, axis);
            if (pivotIndex == k) return;
            if (k < pivotIndex)
                right = pivotIndex - 1;
            else
                left = pivotIndex + 1;
        }
    }

    private static int Partition(Span<ColorEntry> arr, int left, int right, int axis)
    {
        var mid = left + (right - left) / 2;
        Swap(arr, mid, right);

        var pivotValue = GetAxisValue(ref arr[right], axis);
        var storeIndex = left;

        for (var i = left; i < right; i++)
        {
            if (!(GetAxisValue(ref arr[i], axis) < pivotValue)) continue;
            Swap(arr, i, storeIndex);
            storeIndex++;
        }
        Swap(arr, storeIndex, right);
        return storeIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GetAxisValue(ref ColorEntry entry, int axis)
    {
        return axis switch
        {
            0 => entry.L,
            1 => entry.A,
            _ => entry.B
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap(Span<ColorEntry> arr, int i, int j)
    {
        (arr[i], arr[j]) = (arr[j], arr[i]);
    }
}