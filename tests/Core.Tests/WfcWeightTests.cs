using System;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// Brief §11.1: a weight per tile, shared by its rotations, used only to choose a cell's tile.
public class WfcWeightTests
{
    // One layer, so vertical faces sit on the boundary and every tile fits next to every other.
    private static readonly GridTopology Layer = GridTopology.Create3D(new Vector3I(64, 1, 64));

    private static TileDefinition Plain(double weight) => new(new[] { "a", "a", "v", "v", "a", "a" }, TileRotation.None, weight);

    /// <summary>A tile whose four rotations differ only in their rotation-specific vertical sockets.</summary>
    private static TileDefinition Turning(double weight) =>
        new(new[] { "a", "a", "v@0", "v@0", "a", "a" }, TileRotation.FourWay, weight);

    private static double TileShare(CompiledTileSet set, int[] cells, int tile) =>
        cells.Count(v => set.Variants[v].TileIndex == tile) / (double)cells.Length;

    private static int[] Solve(CompiledTileSet set, int[] initial = null, long seed = 1)
    {
        var result = new WfcSolver(Layer, set, initial).Solve(new SolveOptions { Seed = seed });
        Assert.Equal(SolveStatus.Solved, result.Status);
        return result.Cells;
    }

    [Fact]
    public void ChoicesFollowTheWeights()
    {
        var set = CompiledTileSet.Compile(new[] { Plain(3), Plain(1) }, Layer.ActiveDirections);
        foreach (long seed in new long[] { 1, 2, 3 })
        {
            // 4096 independent choices: the standard deviation of the share is about 0.007.
            double share = TileShare(set, Solve(set, seed: seed), 0);
            Assert.InRange(share, 0.72, 0.78);
        }
    }

    [Fact]
    public void RotationsShareTheirTilesWeight()
    {
        var set = CompiledTileSet.Compile(new[] { Turning(1), Plain(1) }, Layer.ActiveDirections);
        Assert.Equal(5, set.Variants.Count);
        Assert.Equal(new[] { 0.25, 0.25, 0.25, 0.25, 1.0 }, set.VariantWeights);

        // Without sharing, the four-way tile would fill four cells in five.
        var cells = Solve(set);
        Assert.InRange(TileShare(set, cells, 0), 0.46, 0.54);
        for (int turns = 0; turns < 4; turns++)
            Assert.InRange(cells.Count(v => v == set.FindVariant(0, turns)) / (double)cells.Length, 0.10, 0.15);
    }

    [Fact]
    public void MergedRotationsKeepTheWholeWeight()
    {
        // Symmetric under a half turn, so its four rotations merge into two variants.
        var tile = new TileDefinition(new[] { "a", "a", "v", "v", "b", "b" }, TileRotation.FourWay, 3);
        var set = CompiledTileSet.Compile(new[] { tile }, Layer.ActiveDirections);
        Assert.Equal(new[] { 1.5, 1.5 }, set.VariantWeights);
    }

    [Fact]
    public void DefaultWeightIsOne()
    {
        var set = CompiledTileSet.Compile(new[] { Uniform3D("a") }, Layer.ActiveDirections);
        Assert.Equal(new[] { 1.0 }, set.VariantWeights);
    }

    [Fact]
    public void SameSeedGivesTheSameResult()
    {
        var set = CompiledTileSet.Compile(new[] { Turning(2), Plain(1), Plain(0.5) }, Layer.ActiveDirections);
        Assert.Equal(Solve(set, seed: 42), Solve(set, seed: 42));
        Assert.NotEqual(Solve(set, seed: 42), Solve(set, seed: 43));
    }

    [Fact]
    public void WeightZeroTilesAreNeverGenerated()
    {
        var set = CompiledTileSet.Compile(new[] { Plain(1), Turning(0) }, Layer.ActiveDirections);
        foreach (long seed in new long[] { 1, 2, 3 })
            Assert.All(Solve(set, seed: seed), v => Assert.Equal(0, set.Variants[v].TileIndex));
    }

    [Fact]
    public void WeightZeroTilesAreAcceptedAsFixedCells()
    {
        var set = CompiledTileSet.Compile(new[] { Plain(1), Turning(0) }, Layer.ActiveDirections);
        var initial = Enumerable.Repeat(WfcSolver.Free, Layer.CellCount).ToArray();
        int fixedCell = Layer.Index(new Vector3I(10, 0, 20));
        initial[fixedCell] = set.FindVariant(1, 3);

        var cells = Solve(set, initial);
        Assert.Equal(initial[fixedCell], cells[fixedCell]);
        Assert.Equal(1, cells.Count(v => set.Variants[v].TileIndex == 1));
    }

    [Fact]
    public void AWeightBehavesLikeThatManyCopies()
    {
        // Cell order and choices both follow weight, so weight 3 acts like three copies of weight 1.
        // Not exactly: a cell left with three copies still needs a decision, which uses a random number.
        var grid = GridTopology.Create2D(12, 12);
        var tiles = WfcSolverTests.ContradictionProne2D();
        int Copies(int i) => i == 2 ? 3 : i == 5 ? 2 : 1;
        var weighted = CompiledTileSet.Compile(tiles.Select((t, i) => t with { Weight = Copies(i) }).ToArray(), grid.ActiveDirections);
        var copies = CompiledTileSet.Compile(tiles.SelectMany((t, i) => Enumerable.Repeat(t, Copies(i))).ToArray(), grid.ActiveDirections);
        int[] tileOfCopy = tiles.SelectMany((t, i) => Enumerable.Repeat(i, Copies(i))).ToArray();

        var shareA = new double[tiles.Length];
        var shareB = new double[tiles.Length];
        const int seeds = 40;
        for (long seed = 0; seed < seeds; seed++)
        {
            foreach (int v in Solved(grid, weighted, seed))
                shareA[weighted.Variants[v].TileIndex] += 1.0 / (grid.CellCount * seeds);
            foreach (int v in Solved(grid, copies, seed))
                shareB[tileOfCopy[copies.Variants[v].TileIndex]] += 1.0 / (grid.CellCount * seeds);
        }
        for (int tile = 0; tile < tiles.Length; tile++)
            Assert.InRange(shareA[tile] - shareB[tile], -0.03, 0.03);
    }

    private static int[] Solved(GridTopology grid, CompiledTileSet set, long seed)
    {
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = seed });
        Assert.Equal(SolveStatus.Solved, result.Status);
        return result.Cells;
    }

    [Fact]
    public void LightCellsAreChosenFirst()
    {
        // Two cells with two options each: the left pair is light, the right pair heavy. The
        // first choice must be the left cell whatever the seed, which the observer shows.
        var grid = GridTopology.Create2D(3, 1);
        var tiles = new[]
        {
            new TileDefinition(new[] { "l", "e", null, null, "a", "a" }),
            new TileDefinition(new[] { "l", "e", null, null, "a", "a" }),
            new TileDefinition(new[] { "e", "h", null, null, "a", "a" }, TileRotation.None, 50),
            new TileDefinition(new[] { "e", "h", null, null, "a", "a" }, TileRotation.None, 50),
            new TileDefinition(new[] { "h", "l", null, null, "a", "a" }),
        };
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        // The middle cell is fixed: its -X face only takes "l" faces, its +X face only "h" faces.
        var initial = new[] { WfcSolver.Free, 4, WfcSolver.Free };
        for (long seed = 0; seed < 20; seed++)
        {
            var order = new FirstResolved();
            Assert.Equal(SolveStatus.Solved, new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = seed }, order).Status);
            Assert.Equal(0, order.Cell);
        }
    }

    private sealed class FirstResolved : ISolveObserver
    {
        public int Cell = -1;
        public void OnCellResolved(int cell, int variant)
        {
            if (Cell < 0 && cell != 1)
                Cell = cell;
        }
        public void OnCellReset(int cell) { }
        public void OnRestart() { }
    }

    [Fact]
    public void WeightZeroTileThatOnlyFitsItselfMakesNeighboursImpossible()
    {
        // A fixed weight-0 tile still constrains its neighbours, and only it can match "lonely".
        var grid = GridTopology.Create2D(3, 1);
        var tiles = new[]
        {
            new TileDefinition(new[] { "a", "a", null, null, "a", "a" }),
            new TileDefinition(new[] { "lonely", "lonely", null, null, "a", "a" }, TileRotation.None, 0),
        };
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        var result = new WfcSolver(grid, set, new[] { WfcSolver.Free, 1, WfcSolver.Free }).Solve();
        Assert.Equal(SolveStatus.Impossible, result.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidWeightsAreErrors(double weight)
    {
        var set = CompiledTileSet.Compile(new[] { Plain(1), Plain(weight) }, Layer.ActiveDirections);
        var error = Assert.Single(set.Errors);
        Assert.Equal(1, error.TileIndex);
        Assert.Equal(-1, error.Direction);
        Assert.Contains("Weight", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AllZeroWeightsAreAnError()
    {
        var set = CompiledTileSet.Compile(new[] { Plain(0), Turning(0) }, Layer.ActiveDirections);
        var error = Assert.Single(set.Errors);
        Assert.Equal(-1, error.TileIndex);
        Assert.Contains("weight 0", error.Message, StringComparison.Ordinal);
    }
}
