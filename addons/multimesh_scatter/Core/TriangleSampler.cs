using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

public enum SamplerMode
{
    /// <summary>Every unit of surface area is equally likely (even density).</summary>
    AreaWeighted,
    /// <summary>Every triangle is equally likely (denser where triangles are small).</summary>
    UniformPerTriangle,
}

/// <summary>A sampled point: triangle index, barycentric coordinates and world position.</summary>
public readonly record struct SurfacePoint(int Triangle, Vector3 Barycentric, Vector3 Position);

/// <summary>
/// Picks random points on a surface. Triangles are chosen in O(1) with Vose's alias method;
/// optional per-triangle weights (for example from a slope filter) scale the probabilities,
/// and zero-weight or degenerate triangles are never chosen.
/// </summary>
public sealed class TriangleSampler
{
    private readonly SurfaceData _surface;
    private readonly float[] _probability;
    private readonly int[] _alias;
    private readonly int[] _triangles;

    /// <summary>Total area of the triangles that can be sampled, scaled by their weights.</summary>
    public float SampledArea { get; }

    public bool IsEmpty => _triangles.Length == 0;

    public TriangleSampler(SurfaceData surface, SamplerMode mode, float[] triangleWeights = null)
    {
        _surface = surface;
        var triangles = new List<int>();
        var weights = new List<double>();
        double area = 0;
        for (int t = 0; t < surface.TriangleCount; t++)
        {
            double w = triangleWeights?[t] ?? 1.0;
            if (w <= 0 || surface.Areas[t] <= 0)
                continue;
            triangles.Add(t);
            weights.Add(mode == SamplerMode.AreaWeighted ? w * surface.Areas[t] : w);
            area += w * surface.Areas[t];
        }
        _triangles = triangles.ToArray();
        SampledArea = (float)area;
        (_probability, _alias) = BuildAlias(weights);
    }

    public SurfacePoint Sample(Pcg32 rng)
    {
        if (IsEmpty)
            throw new InvalidOperationException("The surface has no triangles to sample.");

        int slot = rng.NextInt(_triangles.Length);
        int triangle = _triangles[rng.NextFloat() < _probability[slot] ? slot : _alias[slot]];

        // Uniform point in a triangle (Osada et al. 2002).
        float r1 = MathF.Sqrt(rng.NextFloat());
        float r2 = rng.NextFloat();
        var bary = new Vector3(1 - r1, r1 * (1 - r2), r1 * r2);
        return new SurfacePoint(triangle, bary, _surface.PositionAt(triangle, bary));
    }

    private static (float[] Probability, int[] Alias) BuildAlias(List<double> weights)
    {
        int n = weights.Count;
        var probability = new float[n];
        var alias = new int[n];
        if (n == 0)
            return (probability, alias);

        double sum = 0;
        foreach (var w in weights)
            sum += w;

        var scaled = new double[n];
        var small = new Stack<int>();
        var large = new Stack<int>();
        for (int i = 0; i < n; i++)
        {
            scaled[i] = weights[i] * n / sum;
            (scaled[i] < 1 ? small : large).Push(i);
        }
        while (small.Count > 0 && large.Count > 0)
        {
            int s = small.Pop(), l = large.Pop();
            probability[s] = (float)scaled[s];
            alias[s] = l;
            scaled[l] = scaled[l] + scaled[s] - 1;
            (scaled[l] < 1 ? small : large).Push(l);
        }
        while (large.Count > 0)
            probability[large.Pop()] = 1;
        while (small.Count > 0)
            probability[small.Pop()] = 1; // only reached through rounding error
        return (probability, alias);
    }
}
