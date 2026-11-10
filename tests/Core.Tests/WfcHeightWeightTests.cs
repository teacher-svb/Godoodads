using System;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §12.2: a height curve multiplies a tile's weight, from the bottom layer (0) to the top (1).
public class WfcHeightWeightTests
{
    // Vertical sockets match each other, so every tile fits anywhere and layers are independent.
    private static readonly GridTopology Grid = GridTopology.Create3D(new Vector3I(32, 4, 32));

    private static TileDefinition Tile(double weight = 1, params double[] profile) =>
        new(new[] { "a", "a", "v", "v", "a", "a" }, TileRotation.None, weight, profile.Length > 0 ? profile : null);

    private static SolveResult Solve(CompiledTileSet set, int[] initial = null, long seed = 1) =>
        new WfcSolver(Grid, set, initial).Solve(new SolveOptions { Seed = seed });

    private static double ShareInLayer(CompiledTileSet set, int[] cells, int tile, int layer)
    {
        var inLayer = Enumerable.Range(0, Grid.CellCount).Where(c => Grid.Coordinates(c).Y == layer).ToArray();
        return inLayer.Count(c => set.Variants[cells[c]].TileIndex == tile) / (double)inLayer.Length;
    }

    [Fact]
    public void FrequenciesPerLayerFollowTheCurve()
    {
        // A rises from 0 at the bottom to 1 at the top; B stays at 1. With 4 layers A's multiplier
        // is 0, 1/3, 2/3 and 1, so its share is 0, 1/4, 2/5 and 1/2.
        var set = CompiledTileSet.Compile(new[] { Tile(1, 0, 1), Tile() }, Grid.ActiveDirections);
        var cells = Solve(set).Cells;
        var expected = new[] { 0, 0.25, 0.4, 0.5 };
        for (int layer = 0; layer < 4; layer++)
            Assert.InRange(ShareInLayer(set, cells, 0, layer), expected[layer] - 0.05, expected[layer] + 0.05);
    }

    [Fact]
    public void AFlatCurveOfOneGivesTheSameResultAsNoCurve()
    {
        var plain = CompiledTileSet.Compile(new[] { Tile(2), Tile(1) }, Grid.ActiveDirections);
        var flat = CompiledTileSet.Compile(new[] { Tile(2, 1, 1, 1), Tile(1) }, Grid.ActiveDirections);
        Assert.False(plain.HasHeightProfiles);
        Assert.True(flat.HasHeightProfiles);
        Assert.Equal(Solve(plain, seed: 9).Cells, Solve(flat, seed: 9).Cells);
    }

    [Fact]
    public void WeightAtInterpolatesBetweenSamples()
    {
        var set = CompiledTileSet.Compile(new[] { Tile(4, 0, 1, 0.5) }, Grid.ActiveDirections);
        Assert.Equal(0, set.WeightAt(0, 0), 9);
        Assert.Equal(2, set.WeightAt(0, 0.25), 9);
        Assert.Equal(4, set.WeightAt(0, 0.5), 9);
        Assert.Equal(2, set.WeightAt(0, 1), 9);
        Assert.Equal(0, set.WeightAt(0, -3), 9);
    }

    [Fact]
    public void AZeroMultiplierIsNeverGeneratedThereButMayBeFixed()
    {
        var set = CompiledTileSet.Compile(new[] { Tile(1, 0, 1), Tile() }, Grid.ActiveDirections);
        var initial = Enumerable.Repeat(WfcSolver.Free, Grid.CellCount).ToArray();
        int fixedCell = Grid.Index(new Vector3I(3, 0, 4));
        initial[fixedCell] = set.FindVariant(0, 0);

        var cells = Solve(set, initial).Cells;
        Assert.Equal(initial[fixedCell], cells[fixedCell]);
        int bottomA = Enumerable.Range(0, Grid.CellCount)
            .Count(c => Grid.Coordinates(c).Y == 0 && set.Variants[cells[c]].TileIndex == 0);
        Assert.Equal(1, bottomA);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidCurveValuesAreErrors(double value)
    {
        var set = CompiledTileSet.Compile(new[] { Tile(), Tile(1, 1, value) }, Grid.ActiveDirections);
        var error = Assert.Single(set.Errors);
        Assert.Equal(1, error.TileIndex);
        Assert.Contains("height curve", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALayerWithoutWeightIsFoundAndCannotBeSolved()
    {
        var set = CompiledTileSet.Compile(new[] { Tile(1, 0, 1), Tile(0) }, Grid.ActiveDirections);
        Assert.True(set.IsValid);
        Assert.Equal(new[] { 0 }, set.LayersWithoutWeight(Grid.Size.Y));
        var result = Solve(set);
        Assert.Equal(SolveStatus.Impossible, result.Status);
        Assert.Equal(0, result.Failure.Cell.Y);
    }

    [Fact]
    public void ZeroEverywhereIsACompileError()
    {
        var set = CompiledTileSet.Compile(new[] { Tile(1, 0, 0), Tile(0) }, Grid.ActiveDirections);
        Assert.Contains(set.Errors, e => e.TileIndex == -1 && e.Message.Contains("weight 0"));
    }
}
