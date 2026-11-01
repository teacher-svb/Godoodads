using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// Poisson-disk-like subset selection by sample elimination (Yuksel 2015, "Sample Elimination
/// for Generating Poisson Disk Sample Sets"). From an oversampled candidate set, the most crowded
/// candidate is removed repeatedly until the target count remains, which spreads the survivors
/// evenly without the dart-throwing retry loop.
///
/// On top of the paper, <c>minDistance</c> is a hard limit: candidates closer than that to another
/// survivor or to a fixed point are always removed first, even if that leaves fewer than the
/// target count. Fixed points (existing instances) influence the weights but are never removed.
/// </summary>
public static class SampleElimination
{
    // Weight function exponent and weight-limiting parameters from the paper.
    private const float Alpha = 8f;
    private const float Beta = 0.65f;
    private const float Gamma = 1.5f;
    // Puts conflicting candidates ahead of every non-conflicting one in the heap.
    private const double ConflictPriority = 1e9;

    public readonly record struct Result(int[] Kept, int RemovedForSpacing);

    /// <param name="candidates">Candidate positions (world space).</param>
    /// <param name="targetCount">Number of samples to keep.</param>
    /// <param name="minDistance">Hard minimum distance between kept samples and to fixed points.</param>
    /// <param name="domainArea">Area of the region the candidates cover, used to derive the spreading radius.</param>
    /// <param name="fixedPoints">Positions that are kept unconditionally (for example existing instances).</param>
    /// <returns>Indices of kept candidates in ascending order.</returns>
    public static Result Eliminate(IReadOnlyList<Vector3> candidates, int targetCount, float minDistance,
        float domainArea, IReadOnlyList<Vector3> fixedPoints = null)
    {
        int n = candidates.Count;
        targetCount = Math.Clamp(targetCount, 0, n);
        if (n == 0)
            return new Result(Array.Empty<int>(), 0);

        // Spreading radius: the paper's r_max for targetCount samples on a 2D domain, never
        // smaller than the hard limit.
        float rMax = domainArea > 0 && targetCount > 0
            ? MathF.Sqrt(domainArea / (2f * MathF.Sqrt(3f) * targetCount))
            : 0f;
        float radius = MathF.Max(2f * rMax, minDistance);
        if (radius <= 0f)
            return new Result(FirstN(targetCount), 0);
        float rMin = 2f * rMax * Beta * (1f - MathF.Pow((float)targetCount / n, Gamma));
        float minDistanceSq = minDistance * minDistance;

        var grid = new SpatialHash(radius, candidates);
        var weights = new double[n];
        var conflicts = new int[n];

        // Neighbour lists in compressed rows: neighbours of i are in [start[i], start[i + 1]).
        var start = new int[n + 1];
        var neighbours = new List<int>(n * 16);
        var neighbourWeight = new List<float>(n * 16);
        var neighbourConflict = new List<bool>(n * 16);
        float radiusSq = radius * radius;
        for (int i = 0; i < n; i++)
        {
            start[i] = neighbours.Count;
            var p = candidates[i];
            grid.ForEachNear(p, j =>
            {
                if (j == i)
                    return;
                float dSq = p.DistanceSquaredTo(candidates[j]);
                if (dSq >= radiusSq)
                    return;
                float w = Weight(MathF.Sqrt(dSq), radius, rMin);
                bool conflict = dSq < minDistanceSq;
                neighbours.Add(j);
                neighbourWeight.Add(w);
                neighbourConflict.Add(conflict);
                weights[i] += w;
                if (conflict)
                    conflicts[i]++;
            });
        }
        start[n] = neighbours.Count;

        if (fixedPoints is { Count: > 0 })
        {
            var fixedGrid = new SpatialHash(radius, fixedPoints);
            for (int i = 0; i < n; i++)
            {
                var p = candidates[i];
                fixedGrid.ForEachNear(p, f =>
                {
                    float dSq = p.DistanceSquaredTo(fixedPoints[f]);
                    if (dSq >= radiusSq)
                        return;
                    weights[i] += Weight(MathF.Sqrt(dSq), radius, rMin);
                    if (dSq < minDistanceSq)
                        conflicts[i]++;
                });
            }
        }

        var heap = new IndexedMaxHeap(n);
        for (int i = 0; i < n; i++)
            heap.Push(i, Priority(weights[i], conflicts[i]));

        int alive = n, removed = 0;
        var isAlive = new bool[n];
        Array.Fill(isAlive, true);
        while (heap.Count > 0)
        {
            int top = heap.Peek();
            if (alive <= targetCount && conflicts[top] == 0)
                break;
            heap.Pop();
            isAlive[top] = false;
            alive--;
            if (alive < targetCount)
                removed++;
            for (int k = start[top]; k < start[top + 1]; k++)
            {
                int j = neighbours[k];
                if (!isAlive[j])
                    continue;
                weights[j] -= neighbourWeight[k];
                if (neighbourConflict[k])
                    conflicts[j]--;
                heap.Update(j, Priority(weights[j], conflicts[j]));
            }
        }

        var kept = new int[alive];
        for (int i = 0, k = 0; i < n; i++)
            if (isAlive[i])
                kept[k++] = i;
        return new Result(kept, removed);
    }

    private static float Weight(float distance, float radius, float rMin)
    {
        float d = MathF.Max(distance, rMin);
        return MathF.Pow(1f - d / radius, Alpha);
    }

    private static double Priority(double weight, int conflicts) =>
        conflicts > 0 ? ConflictPriority + weight : weight;

    private static int[] FirstN(int count)
    {
        var result = new int[count];
        for (int i = 0; i < count; i++)
            result[i] = i;
        return result;
    }

    /// <summary>Uniform grid over 3D points with cell size equal to the query radius.</summary>
    private sealed class SpatialHash
    {
        private readonly float _cellSize;
        private readonly Dictionary<(int, int, int), List<int>> _cells = new();

        public SpatialHash(float cellSize, IReadOnlyList<Vector3> points)
        {
            _cellSize = cellSize;
            for (int i = 0; i < points.Count; i++)
            {
                var key = Cell(points[i]);
                if (!_cells.TryGetValue(key, out var list))
                    _cells[key] = list = new List<int>(4);
                list.Add(i);
            }
        }

        public void ForEachNear(Vector3 p, Action<int> visit)
        {
            var (cx, cy, cz) = Cell(p);
            for (int x = cx - 1; x <= cx + 1; x++)
            for (int y = cy - 1; y <= cy + 1; y++)
            for (int z = cz - 1; z <= cz + 1; z++)
                if (_cells.TryGetValue((x, y, z), out var list))
                    foreach (var i in list)
                        visit(i);
        }

        private (int, int, int) Cell(Vector3 p) =>
            ((int)MathF.Floor(p.X / _cellSize), (int)MathF.Floor(p.Y / _cellSize), (int)MathF.Floor(p.Z / _cellSize));
    }

    /// <summary>Binary max-heap over item ids 0..n-1 with key updates. Ties break on the lower id.</summary>
    private sealed class IndexedMaxHeap
    {
        private readonly int[] _heap;
        private readonly int[] _position;
        private readonly double[] _key;

        public int Count { get; private set; }

        public IndexedMaxHeap(int capacity)
        {
            _heap = new int[capacity];
            _position = new int[capacity];
            _key = new double[capacity];
            Array.Fill(_position, -1);
        }

        public void Push(int id, double key)
        {
            _key[id] = key;
            _heap[Count] = id;
            _position[id] = Count;
            SiftUp(Count++);
        }

        public int Peek() => _heap[0];

        public int Pop()
        {
            int top = _heap[0];
            _position[top] = -1;
            Count--;
            if (Count > 0)
            {
                _heap[0] = _heap[Count];
                _position[_heap[0]] = 0;
                SiftDown(0);
            }
            return top;
        }

        public void Update(int id, double key)
        {
            int pos = _position[id];
            if (pos < 0)
                return;
            double old = _key[id];
            _key[id] = key;
            if (key > old)
                SiftUp(pos);
            else
                SiftDown(pos);
        }

        private bool Above(int a, int b) => _key[a] > _key[b] || (_key[a] == _key[b] && a < b);

        private void SiftUp(int pos)
        {
            while (pos > 0)
            {
                int parent = (pos - 1) / 2;
                if (!Above(_heap[pos], _heap[parent]))
                    break;
                Swap(pos, parent);
                pos = parent;
            }
        }

        private void SiftDown(int pos)
        {
            while (true)
            {
                int left = pos * 2 + 1, right = left + 1, best = pos;
                if (left < Count && Above(_heap[left], _heap[best]))
                    best = left;
                if (right < Count && Above(_heap[right], _heap[best]))
                    best = right;
                if (best == pos)
                    return;
                Swap(pos, best);
                pos = best;
            }
        }

        private void Swap(int a, int b)
        {
            (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
            _position[_heap[a]] = a;
            _position[_heap[b]] = b;
        }
    }
}
