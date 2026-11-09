using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

public class WfcSolveJobTests
{
    private static readonly TileDefinition[] Tiles = { Uniform3D("a"), Uniform3D("a"), Uniform3D("a") };

    private static (GridTopology, CompiledTileSet, int[]) Setup(int size)
    {
        var grid = GridTopology.Create3D(new Vector3I(size, size, size));
        return (grid, CompiledTileSet.Compile(Tiles, grid.ActiveDirections), Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray());
    }

    [Fact]
    public void FinishesWithTheSameResultAsADirectSolve()
    {
        var (grid, set, initial) = Setup(10);
        var options = new SolveOptions { Seed = 3 };
        var job = new SolveJob(grid, set, initial, options);
        job.Start();
        Assert.True(job.Wait(10000));
        Assert.True(job.IsFinished);
        Assert.Null(job.Error);
        Assert.Equal(new WfcSolver(grid, set, initial).Solve(options).Cells, job.Result.Cells);
        Assert.Equal(grid.CellCount, job.ResolvedCells);
    }

    [Fact]
    public void CancelStopsALongSolve()
    {
        var (grid, set, initial) = Setup(100);
        var job = new SolveJob(grid, set, initial, new SolveOptions());
        job.Start();
        job.Cancel();
        Assert.True(job.Wait(10000));
        Assert.Equal(SolveStatus.Cancelled, job.Result.Status);
    }

    [Fact]
    public void SolverExceptionIsCaptured()
    {
        var (grid, set, _) = Setup(3);
        var badInitial = new int[grid.CellCount];
        // More initial values than cells makes the solver constructor throw.
        var bad = new SolveJob(GridTopology.Create3D(new Vector3I(2, 2, 2)), set, badInitial, new SolveOptions());
        bad.Start();
        Assert.True(bad.Wait(10000));
        Assert.NotNull(bad.Error);
        Assert.Null(bad.Result);
    }
}
