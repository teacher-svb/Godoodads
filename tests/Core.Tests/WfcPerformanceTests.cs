using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using Xunit.Abstractions;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// Brief §8: 100×100×16 in under 30 s. Skip with --filter "Category!=Performance".
[Trait("Category", "Performance")]
public class WfcPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public WfcPerformanceTests(ITestOutputHelper output) => _output = output;

    /// <summary>Random four-way tiles, plus one plain tile that keeps the set solvable.</summary>
    private static TileDefinition[] Synthetic(int tileCount, int horizontalLabels)
    {
        var random = new Random(tileCount * 31 + horizontalLabels);
        string H() => $"h{random.Next(horizontalLabels)}";
        string V() => $"v{random.Next(3)}";
        return Enumerable.Range(0, tileCount)
            .Select(_ => Tile3D(H(), H(), V(), V(), H(), H(), TileRotation.FourWay))
            .Append(Tile3D("h0", "h0", "v0", "v0", "h0", "h0"))
            .ToArray();
    }

    [Theory]
    [InlineData(25, 3, true)]
    [InlineData(50, 4, true)]
    // Too constrained for the default limits. It measures the time a run takes to give up.
    [InlineData(12, 3, false)]
    public void FullSizeGrid(int tileCount, int horizontalLabels, bool mustSolve)
    {
        var grid = GridTopology.Create3D(new Vector3I(100, 16, 100));
        var set = CompiledTileSet.Compile(Synthetic(tileCount, horizontalLabels), grid.ActiveDirections);
        Assert.True(set.IsValid);

        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        var solver = new WfcSolver(grid, set);
        var clock = Stopwatch.StartNew();
        var result = solver.Solve(new SolveOptions { Seed = 7 });
        clock.Stop();
        long allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

        _output.WriteLine($"PERF tiles={tileCount} variants={set.Variants.Count} status={result.Status} " +
            $"time={clock.Elapsed.TotalSeconds:F2}s allocated={allocated / (1024 * 1024)}MB " +
            $"backtracks={result.Backtracks} restarts={result.Restarts}");

        if (mustSolve)
            Assert.Equal(SolveStatus.Solved, result.Status);
        if (result.Status == SolveStatus.Solved)
            AssertValid(grid, set, result.Cells);
        Assert.True(clock.Elapsed.TotalSeconds < 30, $"Took {clock.Elapsed.TotalSeconds:F1} s.");
    }
}
