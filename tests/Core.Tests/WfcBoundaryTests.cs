using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// Brief §12.1: a socket per bounds face, as if a fixed neighbour sat outside it.
public class WfcBoundaryTests
{
    private static readonly GridTopology Grid = GridTopology.Create3D(new Vector3I(6, 4, 6));

    // A can only rest on "rock", which no tile offers upwards, so only the boundary can hold it.
    private static readonly TileDefinition[] Tiles =
    {
        Tile3D("a", "a", "v", "rock", "a", "a"),
        Tile3D("a", "a", "v", "v", "a", "a"),
        Tile3D("a", "a", "v", "v", "a", "a"),
    };

    private static Socket?[] Boundary(int direction, string label)
    {
        var boundary = new Socket?[Directions.Count];
        Assert.True(Socket.TryParse(label, Directions.IsVertical(direction), out var socket, out _));
        boundary[direction] = socket;
        return boundary;
    }

    private static SolveResult Solve(CompiledTileSet set, Socket?[] boundary, int[] initial = null, long seed = 1, GridTopology grid = null) =>
        new WfcSolver(grid ?? Grid, set, initial).Solve(new SolveOptions { Seed = seed, Boundary = boundary });

    [Fact]
    public void AnEmptyBoundaryChangesNothing()
    {
        var set = CompiledTileSet.Compile(Tiles, Grid.ActiveDirections);
        var plain = new WfcSolver(Grid, set).Solve(new SolveOptions { Seed = 5 });
        Assert.Equal(plain.Cells, Solve(set, null, seed: 5).Cells);
        Assert.Equal(plain.Cells, Solve(set, new Socket?[Directions.Count], seed: 5).Cells);
    }

    [Fact]
    public void ABottomSocketOnlyRestrictsTheBottomLayer()
    {
        var set = CompiledTileSet.Compile(Tiles, Grid.ActiveDirections);
        var result = Solve(set, Boundary(Directions.NegY, "rock"));
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(Grid, set, result.Cells);

        var tile = (int cell) => set.Variants[result.Cells[cell]].TileIndex;
        var cells = Enumerable.Range(0, Grid.CellCount).ToArray();
        Assert.All(cells.Where(c => Grid.Coordinates(c).Y == 0), c => Assert.Equal(0, tile(c)));
        var upper = cells.Where(c => Grid.Coordinates(c).Y > 0).Select(tile).ToArray();
        Assert.DoesNotContain(0, upper);
        Assert.Contains(1, upper);
        Assert.Contains(2, upper);
    }

    [Fact]
    public void WithoutTheBoundaryTheBottomLayerIsMixed()
    {
        var set = CompiledTileSet.Compile(Tiles, Grid.ActiveDirections);
        var result = Solve(set, null);
        var bottom = Enumerable.Range(0, Grid.CellCount).Where(c => Grid.Coordinates(c).Y == 0)
            .Select(c => set.Variants[result.Cells[c]].TileIndex).Distinct().Count();
        Assert.True(bottom > 1);
    }

    [Fact]
    public void APlacedCellThatCannotFaceTheBoundaryIsReported()
    {
        var set = CompiledTileSet.Compile(Tiles, Grid.ActiveDirections);
        var initial = Enumerable.Repeat(WfcSolver.Free, Grid.CellCount).ToArray();
        initial[Grid.Index(new Vector3I(2, 0, 3))] = set.FindVariant(1, 0);

        var result = Solve(set, Boundary(Directions.NegY, "rock"), initial);
        Assert.Equal(SolveStatus.Impossible, result.Status);
        var conflict = Assert.Single(result.FixedConflicts);
        Assert.Equal(new Vector3I(2, 0, 3), conflict.CellA);
        Assert.Equal(new Vector3I(2, -1, 3), conflict.CellB);
        Assert.Equal("v", conflict.SocketA);
        Assert.Equal("rock", conflict.SocketB);
    }

    [Fact]
    public void ABoundaryNoTileFitsIsImpossible()
    {
        var set = CompiledTileSet.Compile(Tiles, Grid.ActiveDirections);
        var result = Solve(set, Boundary(Directions.PosX, "water"));
        Assert.Equal(SolveStatus.Impossible, result.Status);
        Assert.Equal(Grid.Size.X - 1, result.Failure.Cell.X);
    }

    [Fact]
    public void OneSidedBoundarySocketsFaceTheirPartner()
    {
        // The boundary outside +X is "path-", so cells on that face must show "path+".
        var tiles = new[] { Tile3D("path+", "a", "v", "v", "a", "a", TileRotation.FourWay), Tile3D("a", "a", "v", "v", "a", "a") };
        var set = CompiledTileSet.Compile(tiles, Grid.ActiveDirections);
        var result = Solve(set, Boundary(Directions.PosX, "path-"));
        Assert.Equal(SolveStatus.Solved, result.Status);
        foreach (int cell in Enumerable.Range(0, Grid.CellCount).Where(c => Grid.Coordinates(c).X == Grid.Size.X - 1))
            Assert.Equal("path+", set.Variants[result.Cells[cell]].Sockets[Directions.PosX].ToString());
    }

    [Fact]
    public void TwoDimensionalGridsIgnoreVerticalBoundaries()
    {
        var grid = GridTopology.Create2D(5, 5);
        var set = CompiledTileSet.Compile(new[] { Tile2D("a", "a", "a", "a") }, grid.ActiveDirections);
        var boundary = new Socket?[Directions.Count];
        Socket.TryParse("rock", true, out var rock, out _);
        boundary[Directions.NegY] = rock;
        Assert.Equal(SolveStatus.Solved, Solve(set, boundary, grid: grid).Status);
    }
}
