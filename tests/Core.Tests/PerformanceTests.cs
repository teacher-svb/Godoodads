using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godoodads.Scatter.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

// Brief §8: 50,000 instances on a 100,000-triangle mesh in under 3 s without minimum distance and
// under 6 s with it. Runs with the other tests (about 2 s); skip with --filter "Category!=Performance".
[Trait("Category", "Performance")]
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output) => _output = output;

    private static SurfaceData Grid(int quadsPerSide, float size)
    {
        int v = quadsPerSide + 1;
        var vertices = new Vector3[v * v];
        var normals = new Vector3[v * v];
        for (int z = 0; z < v; z++)
            for (int x = 0; x < v; x++)
            {
                vertices[z * v + x] = new Vector3(x * size / quadsPerSide, Mathf.Sin(x * 0.1f) * Mathf.Cos(z * 0.1f), z * size / quadsPerSide);
                normals[z * v + x] = Vector3.Up;
            }
        var indices = new List<int>(quadsPerSide * quadsPerSide * 6);
        for (int z = 0; z < quadsPerSide; z++)
            for (int x = 0; x < quadsPerSide; x++)
            {
                int i = z * v + x;
                indices.AddRange(new[] { i, i + 1, i + v + 1, i, i + v + 1, i + v });
            }
        return SurfaceData.Build(new[] { new SurfaceArrays { Vertices = vertices, Normals = normals, Indices = indices.ToArray() } }, Transform3D.Identity);
    }

    [Theory]
    [InlineData(0f, 3000)]
    [InlineData(0.3f, 6000)]
    public void Fifty_thousand_instances_on_a_hundred_thousand_triangles(float minDistance, long budgetMs)
    {
        var surface = Grid(224, 200f);
        Assert.True(surface.TriangleCount >= 100_000);

        var clock = Stopwatch.StartNew();
        var result = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 50_000, MinDistance = minDistance, RandomTiltDegrees = 10 });
        clock.Stop();

        _output.WriteLine($"min distance {minDistance}: {result.Placed} placed from {result.Candidates} candidates in {clock.ElapsedMilliseconds} ms");
        Assert.Equal(50_000, result.Placed);
        Assert.True(clock.ElapsedMilliseconds < budgetMs, $"{clock.ElapsedMilliseconds} ms");
    }

    // Amount at the top of its slider with a tree-sized Min Distance. Before 1.3.1 this took
    // minutes and gigabytes (about 12 GB at 3 m), and around 5 m the editor ran out of memory.
    [Theory]
    [InlineData(1f)]
    [InlineData(5f)]
    public void Large_min_distance_with_a_large_amount_stays_bounded(float minDistance)
    {
        var surface = Grid(10, 100f);
        long allocated = System.GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var result = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 100_000, MinDistance = minDistance });
        clock.Stop();
        allocated = System.GC.GetAllocatedBytesForCurrentThread() - allocated;

        _output.WriteLine($"min distance {minDistance}: {result.Placed} placed in {clock.ElapsedMilliseconds} ms, {allocated / 1_000_000} MB allocated");
        Assert.True(result.Placed > 0);
        Assert.Equal(result.Requested - result.Placed, result.RemovedBySpacing);
        Assert.True(clock.ElapsedMilliseconds < 4000, $"{clock.ElapsedMilliseconds} ms");
        Assert.True(allocated < 600_000_000, $"{allocated / 1_000_000} MB allocated");
    }
}
