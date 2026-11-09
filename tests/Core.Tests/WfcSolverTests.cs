using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

public class WfcSolverTests
{
    private static readonly TileDefinition[] Stacked =
    {
        Tile3D("air", "air", "air", "air", "air", "air"),
        Tile3D("rock", "rock", "air", "rock", "rock", "rock"),
        Tile3D("rock", "rock", "rock", "rock", "rock", "rock"),
        Tile3D("rock", "air", "air", "rock", "air", "air", TileRotation.FourWay),
        Tile3D("air+", "air-", "air", "air", "air", "air", TileRotation.FourWay),
        Tile3D("air", "air", "s@0", "rock", "air", "air", TileRotation.FourWay),
        Tile3D("air", "air", "air", "s@0", "air", "air", TileRotation.FourWay),
    };

    private static readonly TileDefinition[] Pipes2D =
    {
        Tile2D("o", "o", "o", "o"),
        Tile2D("p", "p", "o", "o", TileRotation.FourWay),
        Tile2D("p", "o", "p", "o", TileRotation.FourWay),
        Tile2D("p", "p", "p", "o", TileRotation.FourWay),
    };

    /// <summary>Found by random search: solvable on large grids, but every seed hits contradictions.</summary>
    internal static TileDefinition[] ContradictionProne2D() =>
        new[] { "acbc", "acca", "caab", "acaa", "abbc", "ccca", "bcba", "bcaa" }
            .Select(t => Tile2D(t[0].ToString(), t[1].ToString(), t[2].ToString(), t[3].ToString()))
            .ToArray();

    private sealed class Recorder : ISolveObserver
    {
        public readonly int[] Cells;
        public int Events;
        public int Resets;
        public int Restarts;
        public CancellationTokenSource CancelAfter;
        public int CancelAt = int.MaxValue;

        public Recorder(int count)
        {
            Cells = new int[count];
            System.Array.Fill(Cells, -1);
        }

        public void OnCellResolved(int cell, int variant)
        {
            Cells[cell] = variant;
            if (++Events >= CancelAt)
                CancelAfter?.Cancel();
        }

        public void OnCellReset(int cell)
        {
            Cells[cell] = -1;
            Resets++;
        }

        public void OnRestart()
        {
            System.Array.Fill(Cells, -1);
            Restarts++;
        }
    }

    private static CompiledTileSet Compile(TileDefinition[] tiles, GridTopology grid)
    {
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        Assert.True(set.IsValid);
        return set;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(77)]
    public void SameSeedGivesSameResult(long seed)
    {
        var grid = GridTopology.Create3D(new Vector3I(12, 6, 12));
        var set = Compile(Stacked, grid);
        var first = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
        var second = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
        Assert.Equal(SolveStatus.Solved, first.Status);
        Assert.Equal(first.Cells, second.Cells);
    }

    [Fact]
    public void DifferentSeedsGiveDifferentResults()
    {
        var grid = GridTopology.Create3D(new Vector3I(12, 6, 12));
        var set = Compile(Stacked, grid);
        var a = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 1 });
        var b = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 2 });
        Assert.NotEqual(a.Cells, b.Cells);
    }

    [Fact]
    public void ReusingASolverGivesTheSameResult()
    {
        var grid = GridTopology.Create3D(new Vector3I(8, 4, 8));
        var solver = new WfcSolver(grid, Compile(Stacked, grid));
        var options = new SolveOptions { Seed = 5 };
        Assert.Equal(solver.Solve(options).Cells, solver.Solve(options).Cells);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Solves3DWithAllNeighboursMatching(long seed)
    {
        var grid = GridTopology.Create3D(new Vector3I(15, 8, 15));
        var set = Compile(Stacked, grid);
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
        Assert.Equal(SolveStatus.Solved, result.Status);
        Assert.All(result.Cells, c => Assert.InRange(c, 0, set.Variants.Count - 1));
        AssertValid(grid, set, result.Cells);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Solves2DWithAllNeighboursMatching(long seed)
    {
        var grid = GridTopology.Create2D(30, 30);
        var set = Compile(Pipes2D, grid);
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(grid, set, result.Cells);
    }

    [Fact]
    public void SingleVariantFillsTheGrid()
    {
        var grid = GridTopology.Create3D(new Vector3I(3, 3, 3));
        var set = Compile(new[] { Uniform3D("a") }, grid);
        var recorder = new Recorder(grid.CellCount);
        var result = new WfcSolver(grid, set).Solve(new SolveOptions(), recorder);
        Assert.Equal(SolveStatus.Solved, result.Status);
        Assert.All(result.Cells, c => Assert.Equal(0, c));
        Assert.Equal(result.Cells, recorder.Cells);
    }

    [Fact]
    public void FixedCellsAreKeptAndConstrainNeighbours()
    {
        var grid = GridTopology.Create3D(new Vector3I(8, 4, 8));
        var set = Compile(Stacked, grid);
        var initial = Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray();
        int fullRock = set.FindVariant(2, 0);
        for (int z = 0; z < 8; z++)
            for (int x = 0; x < 8; x++)
                initial[grid.Index(new Vector3I(x, 0, z))] = fullRock;

        for (long seed = 0; seed < 10; seed++)
        {
            var result = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = seed });
            Assert.Equal(SolveStatus.Solved, result.Status);
            for (int cell = 0; cell < grid.CellCount; cell++)
                if (initial[cell] >= 0)
                    Assert.Equal(initial[cell], result.Cells[cell]);
            AssertValid(grid, set, result.Cells);
        }
    }

    [Fact]
    public void ExcludedCellsImposeNoConstraint()
    {
        var grid = GridTopology.Create2D(3, 1);
        var set = Compile(new[] { Tile2D("a", "a", "a", "a") }, grid);
        var initial = new[] { WfcSolver.Free, WfcSolver.Excluded, WfcSolver.Free };
        var result = new WfcSolver(grid, set, initial).Solve();
        Assert.Equal(SolveStatus.Solved, result.Status);
        Assert.Equal(new[] { 0, -1, 0 }, result.Cells);

        // Without the excluded cell this pair could never sit together.
        var split = Compile(new[] { Tile2D("x", "y", "a", "a") }, grid);
        Assert.Equal(SolveStatus.Impossible, new WfcSolver(grid, split).Solve().Status);
        Assert.Equal(SolveStatus.Solved, new WfcSolver(grid, split, initial).Solve().Status);
    }

    [Fact]
    public void AdjacentFixedConflictIsReportedWithCoordinates()
    {
        var grid = GridTopology.Create2D(4, 1);
        var set = Compile(new[] { Tile2D("a", "a", "o", "o"), Tile2D("b", "b", "o", "o") }, grid);
        var initial = new[] { 0, 1, WfcSolver.Free, WfcSolver.Free };
        var recorder = new Recorder(grid.CellCount);
        var result = new WfcSolver(grid, set, initial).Solve(null, recorder);

        Assert.Equal(SolveStatus.Impossible, result.Status);
        var conflict = Assert.Single(result.FixedConflicts);
        Assert.Equal(new Vector3I(0, 0, 0), conflict.CellA);
        Assert.Equal(new Vector3I(1, 0, 0), conflict.CellB);
        Assert.Equal("a", conflict.SocketA);
        Assert.Equal("b", conflict.SocketB);
        Assert.Equal(0, recorder.Events);
    }

    [Fact]
    public void FixedCellsThatConflictThroughAGapAreImpossible()
    {
        var grid = GridTopology.Create2D(3, 1);
        var set = Compile(new[] { Tile2D("a", "a", "o", "o"), Tile2D("b", "b", "o", "o") }, grid);
        var result = new WfcSolver(grid, set, new[] { 0, WfcSolver.Free, 1 }).Solve();

        Assert.Equal(SolveStatus.Impossible, result.Status);
        Assert.Empty(result.FixedConflicts);
        Assert.Equal(new Vector3I(2, 0, 0), result.Failure.Cell);
        Assert.True(result.Failure.HasFixedCells);
        var neighbour = Assert.Single(result.Failure.Neighbours);
        Assert.Equal(Directions.NegX, neighbour.Direction);
        Assert.Equal(new[] { "a" }, neighbour.Sockets);
    }

    [Fact]
    public void TileSetWithNoSolutionIsProvenImpossible()
    {
        var grid = GridTopology.Create2D(2, 1);
        var set = Compile(new[] { Tile2D("x", "y", "a", "a") }, grid);
        var result = new WfcSolver(grid, set).Solve();
        Assert.Equal(SolveStatus.Impossible, result.Status);
        Assert.False(result.Failure.HasFixedCells);
    }

    private static bool HasSolution(GridTopology grid, CompiledTileSet set, int[] cells, int cell)
    {
        if (cell == grid.CellCount)
            return true;
        for (int variant = 0; variant < set.Variants.Count; variant++)
        {
            bool fits = true;
            foreach (int direction in grid.ActiveDirections)
            {
                int neighbour = grid.Neighbour(cell, direction);
                if (neighbour >= 0 && neighbour < cell && !set.Allows(variant, direction, cells[neighbour]))
                    fits = false;
            }
            if (!fits)
                continue;
            cells[cell] = variant;
            if (HasSolution(grid, set, cells, cell + 1))
                return true;
        }
        return false;
    }

    [Fact]
    public void AgreesWithBruteForceOnRandomSmallTileSets()
    {
        var random = new System.Random(1234);
        var grid = GridTopology.Create2D(3, 3);
        string Label() => "abc"[random.Next(3)].ToString();
        var options = new SolveOptions { Seed = 1, MaxBacktracks = int.MaxValue, MaxRestarts = 0 };

        int solved = 0, impossibleAfterSearch = 0;
        for (int trial = 0; trial < 3000; trial++)
        {
            var tiles = Enumerable.Range(0, 3 + random.Next(5))
                .Select(_ => Tile2D(Label(), Label(), Label(), Label()))
                .ToArray();
            var set = Compile(tiles, grid);
            var result = new WfcSolver(grid, set).Solve(options);

            bool exists = HasSolution(grid, set, new int[grid.CellCount], 0);
            Assert.Equal(exists ? SolveStatus.Solved : SolveStatus.Impossible, result.Status);
            if (exists)
            {
                AssertValid(grid, set, result.Cells);
                solved++;
            }
            else if (result.Backtracks > 0)
            {
                impossibleAfterSearch++;
            }
        }
        Assert.True(solved > 0, "Expected some solvable tile sets.");
        Assert.True(impossibleAfterSearch > 0, "Expected some tile sets that are only proven impossible by searching.");
    }

    [Fact]
    public void BacktrackingRecoversFromContradictions()
    {
        var grid = GridTopology.Create2D(24, 24);
        var set = Compile(ContradictionProne2D(), grid);
        long totalBacktracks = 0;
        for (long seed = 0; seed < 30; seed++)
        {
            var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
            Assert.Equal(SolveStatus.Solved, result.Status);
            AssertValid(grid, set, result.Cells);
            totalBacktracks += result.Backtracks;
        }
        Assert.True(totalBacktracks > 0, "The tile set is expected to cause contradictions.");
    }

    [Fact]
    public void ResultsStayDeterministicWithBacktracksAndRestarts()
    {
        var grid = GridTopology.Create2D(24, 24);
        var set = Compile(ContradictionProne2D(), grid);
        var options = new SolveOptions { MaxBacktracks = 1, MaxRestarts = 500 };
        int withRestarts = 0;
        for (long seed = 0; seed < 30; seed++)
        {
            var seeded = options with { Seed = seed };
            var first = new WfcSolver(grid, set).Solve(seeded);
            var second = new WfcSolver(grid, set).Solve(seeded);
            Assert.Equal(first.Status, second.Status);
            Assert.Equal(first.Cells, second.Cells);
            Assert.Equal(first.Backtracks, second.Backtracks);
            Assert.Equal(first.Restarts, second.Restarts);
            withRestarts += first.Restarts > 0 ? 1 : 0;
        }
        Assert.True(withRestarts > 0, "Expected at least one seed to restart.");
    }

    [Fact]
    public void OutOfAttemptsWhenLimitsAreExhausted()
    {
        var grid = GridTopology.Create2D(24, 24);
        var set = Compile(ContradictionProne2D(), grid);
        var limited = new SolveOptions { MaxBacktracks = 0, MaxRestarts = 0 };

        SolveResult failed = null;
        for (long seed = 0; seed < 200 && failed == null; seed++)
        {
            var result = new WfcSolver(grid, set).Solve(limited with { Seed = seed });
            if (result.Status != SolveStatus.Solved)
                failed = result;
        }

        Assert.NotNull(failed);
        Assert.Equal(SolveStatus.OutOfAttempts, failed.Status);
        Assert.NotNull(failed.Failure);
        Assert.NotEmpty(failed.Failure.Neighbours);
    }

    [Fact]
    public void AlreadyCancelledTokenStopsTheSolve()
    {
        var grid = GridTopology.Create2D(10, 10);
        var set = Compile(Pipes2D, grid);
        using var source = new CancellationTokenSource();
        source.Cancel();
        var result = new WfcSolver(grid, set).Solve(null, null, source.Token);
        Assert.Equal(SolveStatus.Cancelled, result.Status);
        Assert.Null(result.Cells);
    }

    [Fact]
    public void CancellingMidSolveStops()
    {
        var grid = GridTopology.Create2D(40, 40);
        var set = Compile(Pipes2D, grid);
        using var source = new CancellationTokenSource();
        var recorder = new Recorder(grid.CellCount) { CancelAfter = source, CancelAt = 10 };
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 1 }, recorder, source.Token);
        Assert.Equal(SolveStatus.Cancelled, result.Status);
        Assert.Contains(recorder.Cells, c => c == -1);
    }

    [Fact]
    public void ReplayingEventsReproducesTheGridAfterBacktracks()
    {
        var grid = GridTopology.Create2D(24, 24);
        var set = Compile(ContradictionProne2D(), grid);
        bool sawBacktrack = false;
        for (long seed = 0; seed < 30; seed++)
        {
            var recorder = new Recorder(grid.CellCount);
            var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed }, recorder);
            Assert.Equal(SolveStatus.Solved, result.Status);
            Assert.Equal(result.Cells, recorder.Cells);
            sawBacktrack |= result.Backtracks > 0 && recorder.Resets > 0;
        }
        Assert.True(sawBacktrack, "Expected a seed that backtracks.");
    }

    [Fact]
    public void ReplayingEventsReproducesTheGridAfterRestarts()
    {
        var grid = GridTopology.Create2D(8, 8);
        var set = Compile(ContradictionProne2D(), grid);
        var options = new SolveOptions { MaxBacktracks = 0, MaxRestarts = 2000 };
        bool sawRestart = false;
        for (long seed = 0; seed < 30; seed++)
        {
            var recorder = new Recorder(grid.CellCount);
            var result = new WfcSolver(grid, set).Solve(options with { Seed = seed }, recorder);
            if (result.Status != SolveStatus.Solved)
                continue;
            Assert.Equal(result.Cells, recorder.Cells);
            Assert.Equal(result.Restarts, recorder.Restarts);
            sawRestart |= result.Restarts > 0;
        }
        Assert.True(sawRestart, "Expected a seed that restarts.");
    }

    [Fact]
    public void WorksWithMoreThanOneBitsetWord()
    {
        var tiles = Enumerable.Range(0, 30)
            .Select(i => Tile3D($"h{i % 2}", $"h{(i + 1) % 2}", "v", "v", "z", "z", TileRotation.FourWay))
            .Concat(Enumerable.Range(0, 40).Select(_ => Tile3D("z", "z", "v", "v", "z", "z")))
            .ToArray();
        var grid = GridTopology.Create3D(new Vector3I(8, 3, 8));
        var set = Compile(tiles, grid);
        Assert.True(set.WordCount > 1);
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 9 });
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(grid, set, result.Cells);
    }
}
