using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §13.2: measured tile shares in the preview.
public class WfcTileSharesTests
{
    private static TileDefinition Tile(double weight = 1, TileRotation rotation = TileRotation.None, params double[] profile) =>
        new(new[] { "a", "a", "v", "v", "a", "a" }, rotation, weight, profile.Length > 0 ? profile : null);

    [Fact]
    public void CountsCellsPerTileAndWeightsPerTile()
    {
        var grid = GridTopology.Create3D(new Vector3I(2, 1, 2));
        // A four-way tile of weight 4 next to a plain tile of weight 1: weight shares 4/5 and 1/5.
        var set = CompiledTileSet.Compile(new[] { Tile(4, TileRotation.FourWay), Tile(1) }, grid.ActiveDirections);
        int b = set.FindVariant(1, 0);
        var cells = new[] { set.FindVariant(0, 0), set.FindVariant(0, 1), b, b };
        var shares = TileShares.Measure(grid, set, cells, new[] { 0, 1 });
        Assert.Equal(0.8, shares.WeightShare[0], 9);
        Assert.Equal(0.2, shares.WeightShare[1], 9);
        Assert.Equal(0.5, shares.MeasuredShare[0], 9);
        Assert.Equal(0.5, shares.MeasuredShare[1], 9);
        Assert.Null(shares.ThirdShare);
        Assert.False(shares.IsFlagged(0));
        Assert.True(shares.IsFlagged(1));
    }

    [Fact]
    public void CopiesShareAGroup()
    {
        Assert.Equal(new[] { 0, 1, 0, 2 }, TileShares.GroupByKey(new[] { "air", "rock", "air", "grass" }));
        var grid = GridTopology.Create3D(new Vector3I(3, 1, 1));
        var set = CompiledTileSet.Compile(new[] { Tile(), Tile(), Tile(2) }, grid.ActiveDirections);
        var cells = new[] { set.FindVariant(0, 0), set.FindVariant(1, 0), set.FindVariant(2, 0) };
        var shares = TileShares.Measure(grid, set, cells, new[] { 0, 0, 1 });
        Assert.Equal(new[] { 0.5, 0.5 }, shares.WeightShare);
        Assert.Equal(2 / 3.0, shares.MeasuredShare[0], 9);
    }

    [Fact]
    public void ThirdsFollowTheLayers()
    {
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2 }, Enumerable.Range(0, 9).Select(l => TileShares.ThirdOf(l, 9)));
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 2, 2 }, Enumerable.Range(0, 8).Select(l => TileShares.ThirdOf(l, 8)));

        // The height-weight grid: A is only possible above the bottom layer.
        var grid = GridTopology.Create3D(new Vector3I(16, 3, 16));
        var set = CompiledTileSet.Compile(new[] { Tile(1, TileRotation.None, 0, 1), Tile() }, grid.ActiveDirections);
        var cells = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 3 }).Cells;
        var shares = TileShares.Measure(grid, set, cells, new[] { 0, 1 });
        Assert.Equal(0, shares.ThirdShare[0][0]);
        Assert.Equal(1, shares.ThirdShare[1][0]);
        Assert.Equal(0.5, shares.ThirdShare[0][2], 1);
        Assert.Equal(1, shares.ThirdShare[0].Zip(shares.ThirdShare[1], (x, y) => x + y).Max(), 9);
    }

    [Theory]
    [InlineData(0.1, 0.2, false)]
    [InlineData(0.1, 0.21, true)]
    [InlineData(0.1, 0.05, false)]
    [InlineData(0.1, 0.049, true)]
    [InlineData(0.1, 0, true)]
    [InlineData(0, 0, false)]
    public void FlagsBigGapsEitherWay(double weight, double measured, bool flagged)
    {
        var grid = GridTopology.Create3D(new Vector3I(1, 1, 1));
        var set = CompiledTileSet.Compile(new[] { Tile() }, grid.ActiveDirections);
        var shares = TileShares.Measure(grid, set, new[] { 0 }, new[] { 0 });
        shares.WeightShare[0] = weight;
        shares.MeasuredShare[0] = measured;
        Assert.Equal(flagged, shares.IsFlagged(0));
    }
}
