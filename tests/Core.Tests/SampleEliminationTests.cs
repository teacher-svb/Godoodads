using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Scatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class SampleEliminationTests
{
    private static Vector3[] RandomPlanePoints(int count, float size, ulong seed)
    {
        var rng = new Pcg32(seed);
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
            points[i] = new Vector3(rng.Range(0, size), 0, rng.Range(0, size));
        return points;
    }

    private static float MinPairDistance(IReadOnlyList<Vector3> points)
    {
        float best = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
            for (int j = i + 1; j < points.Count; j++)
                best = MathF.Min(best, points[i].DistanceTo(points[j]));
        return best;
    }

    [Fact]
    public void Keeps_exactly_the_target_count_when_space_allows()
    {
        var candidates = RandomPlanePoints(2500, 10, 1);
        var result = SampleElimination.Eliminate(candidates, 500, 0.1f, 100f);
        Assert.Equal(500, result.Kept.Length);
        Assert.Equal(0, result.RemovedForSpacing);
        Assert.True(result.Kept.SequenceEqual(result.Kept.OrderBy(i => i)));
    }

    [Fact]
    public void Spreads_points_better_than_random_selection()
    {
        var candidates = RandomPlanePoints(2500, 10, 2);
        var kept = SampleElimination.Eliminate(candidates, 500, 0.01f, 100f).Kept.Select(i => candidates[i]).ToList();
        var random = candidates.Take(500).ToList();
        // Poisson-disk sets reach about 0.75 r_max; random subsets are far lower.
        float rMax = MathF.Sqrt(100f / (2f * MathF.Sqrt(3f) * 500));
        Assert.True(MinPairDistance(kept) > 0.5f * rMax, $"elimination min {MinPairDistance(kept)}");
        Assert.True(MinPairDistance(kept) > 3f * MinPairDistance(random));
    }

    [Fact]
    public void Min_distance_is_a_hard_limit_and_reports_the_shortfall()
    {
        var candidates = RandomPlanePoints(2000, 10, 3);
        // At most about 100 / (0.866 * 1.5^2) ≈ 51 points fit, far fewer than requested.
        var result = SampleElimination.Eliminate(candidates, 400, 1.5f, 100f);
        var kept = result.Kept.Select(i => candidates[i]).ToList();
        Assert.True(MinPairDistance(kept) >= 1.5f);
        Assert.True(kept.Count < 400);
        Assert.Equal(400 - kept.Count, result.RemovedForSpacing);
    }

    [Fact]
    public void Min_distance_far_above_the_natural_spacing_still_fills_the_area()
    {
        // 10,000 requested at 3 m on 100 × 100 m: at most about 1,280 fit, and the candidates
        // are thinned. Random packing reaches about half the hexagonal bound.
        var candidates = RandomPlanePoints(50_000, 100, 6);
        var fixedPoints = new[] { new Vector3(50, 0, 50) };
        var result = SampleElimination.Eliminate(candidates, 10_000, 3f, 10_000f, fixedPoints);
        var kept = result.Kept.Select(i => candidates[i]).ToList();
        Assert.True(MinPairDistance(kept) >= 3f);
        Assert.All(kept, p => Assert.True(p.DistanceTo(fixedPoints[0]) >= 3f));
        Assert.True(kept.Count > 400, $"{kept.Count} kept");
        Assert.Equal(10_000 - kept.Count, result.RemovedForSpacing);
        Assert.True(result.Kept.SequenceEqual(result.Kept.OrderBy(i => i)));
    }

    [Fact]
    public void A_thin_strip_keeps_its_candidates()
    {
        // 100 × 0.2 m holds about 100 points 1 m apart, five times what its area suggests;
        // the neighbour count is low, so no candidates are dropped. Capping candidates by area
        // (about 23 fit by area, so 115 candidates) would keep fewer.
        var rng = new Pcg32(7);
        var candidates = new Vector3[500];
        for (int i = 0; i < candidates.Length; i++)
            candidates[i] = new Vector3(rng.Range(0, 100), 0, rng.Range(0, 0.2f));
        var result = SampleElimination.Eliminate(candidates, 100, 1f, 20f);
        var kept = result.Kept.Select(i => candidates[i]).ToList();
        Assert.True(MinPairDistance(kept) >= 1f);
        Assert.True(kept.Count >= 50, $"{kept.Count} kept");
    }

    [Fact]
    public void Fixed_points_are_avoided()
    {
        var candidates = RandomPlanePoints(1000, 10, 4);
        var fixedPoints = new[] { new Vector3(5, 0, 5), new Vector3(2, 0, 2) };
        var result = SampleElimination.Eliminate(candidates, 100, 1f, 100f, fixedPoints);
        foreach (var i in result.Kept)
            foreach (var f in fixedPoints)
                Assert.True(candidates[i].DistanceTo(f) >= 1f);
    }

    [Fact]
    public void Is_deterministic()
    {
        var candidates = RandomPlanePoints(1500, 10, 5);
        var a = SampleElimination.Eliminate(candidates, 300, 0.2f, 100f).Kept;
        var b = SampleElimination.Eliminate(candidates, 300, 0.2f, 100f).Kept;
        Assert.Equal(a, b);
    }
}
