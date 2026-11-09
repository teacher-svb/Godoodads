using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

public class WfcGridTests
{
    [Fact]
    public void OppositeDirectionsAreInverse()
    {
        foreach (int d in Directions.All3D)
        {
            Assert.Equal(d, Directions.Opposite(Directions.Opposite(d)));
            Assert.Equal(-Directions.Offset(d), Directions.Offset(Directions.Opposite(d)));
        }
    }

    [Fact]
    public void IndexAndCoordinatesRoundTrip()
    {
        var grid = GridTopology.Create3D(new Vector3I(3, 4, 5));
        Assert.Equal(60, grid.CellCount);
        for (int cell = 0; cell < grid.CellCount; cell++)
            Assert.Equal(cell, grid.Index(grid.Coordinates(cell)));
    }

    [Fact]
    public void Neighbours3DStopAtEdgesAndCorners()
    {
        var grid = GridTopology.Create3D(new Vector3I(3, 3, 3));
        int centre = grid.Index(new Vector3I(1, 1, 1));
        foreach (int d in Directions.All3D)
            Assert.Equal(new Vector3I(1, 1, 1) + Directions.Offset(d), grid.Coordinates(grid.Neighbour(centre, d)));

        int corner = grid.Index(Vector3I.Zero);
        Assert.Equal(-1, grid.Neighbour(corner, Directions.NegX));
        Assert.Equal(-1, grid.Neighbour(corner, Directions.NegY));
        Assert.Equal(-1, grid.Neighbour(corner, Directions.NegZ));
        Assert.NotEqual(-1, grid.Neighbour(corner, Directions.PosX));
    }

    [Fact]
    public void Grid2DHasNoVerticalNeighbours()
    {
        var grid = GridTopology.Create2D(4, 4);
        int cell = grid.Index(new Vector3I(1, 0, 1));
        Assert.Equal(-1, grid.Neighbour(cell, Directions.PosY));
        Assert.Equal(-1, grid.Neighbour(cell, Directions.NegY));
        Assert.Equal(new Vector3I(1, 0, 2), grid.Coordinates(grid.Neighbour(cell, Directions.PosZ)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RotateYMatchesGodotBasis(int turns)
    {
        var basis = new Basis(Vector3.Up, turns * Mathf.Pi / 2f);
        foreach (int d in Directions.All3D)
        {
            var rotated = basis * (Vector3)Directions.Offset(d);
            Assert.Equal((Vector3)Directions.Offset(Directions.RotateY(d, turns)), rotated.Round());
        }
    }
}

public class WfcSocketTests
{
    private static Socket Parse(string label, bool vertical)
    {
        Assert.True(Socket.TryParse(label, vertical, out var socket, out string error), error);
        return socket;
    }

    [Theory]
    [InlineData("path", "path", false, true)]
    [InlineData("path", "road", false, false)]
    [InlineData("path+", "path-", false, true)]
    [InlineData("path-", "path+", false, true)]
    [InlineData("path+", "path+", false, false)]
    [InlineData("path-", "path-", false, false)]
    [InlineData("path", "path+", false, false)]
    [InlineData("stair", "stair", true, true)]
    [InlineData("stair", "ramp", true, false)]
    [InlineData("stair@1", "stair@1", true, true)]
    [InlineData("stair@1", "stair@2", true, false)]
    [InlineData("stair", "stair@0", true, false)]
    public void Matching(string a, string b, bool vertical, bool expected)
    {
        Assert.Equal(expected, Parse(a, vertical).Matches(Parse(b, vertical)));
        Assert.Equal(expected, Parse(b, vertical).Matches(Parse(a, vertical)));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, true)]
    [InlineData("+", false)]
    [InlineData("a b", false)]
    [InlineData("a+b", false)]
    [InlineData("a@1", false)]
    [InlineData("a+", true)]
    [InlineData("a@4", true)]
    [InlineData("a@", true)]
    [InlineData("@1", true)]
    public void InvalidLabelsAreRejected(string label, bool vertical)
    {
        Assert.False(Socket.TryParse(label, vertical, out _, out string error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void RotationOnlyChangesRotationSpecificSockets()
    {
        Assert.Equal("stair@3", Parse("stair@1", true).RotatedY(2).ToString());
        Assert.Equal("stair@0", Parse("stair@3", true).RotatedY(1).ToString());
        Assert.Equal("stair", Parse("stair", true).RotatedY(1).ToString());
        Assert.Equal("path+", Parse("path+", false).RotatedY(3).ToString());
    }
}

public class WfcVariantTests
{
    private static readonly TileDefinition Asymmetric = Tile3D("a", "b", "t", "u", "c", "d", TileRotation.FourWay);

    [Theory]
    [InlineData(TileRotation.None, 1)]
    [InlineData(TileRotation.TwoWay, 2)]
    [InlineData(TileRotation.FourWay, 4)]
    public void RotationModeSetsVariantCount(TileRotation mode, int expected)
    {
        var set = CompiledTileSet.Compile(new[] { Asymmetric with { Rotation = mode } }, Directions.All3D);
        Assert.True(set.IsValid);
        Assert.Equal(expected, set.Variants.Count);
    }

    [Fact]
    public void QuarterTurnMovesFacesLikeBasis()
    {
        var set = CompiledTileSet.Compile(new[] { Asymmetric }, Directions.All3D);
        var turned = set.Variants[set.FindVariant(0, 1)];
        Assert.Equal("a", turned.Sockets[Directions.NegZ].ToString());
        Assert.Equal("d", turned.Sockets[Directions.NegX].ToString());
        Assert.Equal("b", turned.Sockets[Directions.PosZ].ToString());
        Assert.Equal("c", turned.Sockets[Directions.PosX].ToString());
        Assert.Equal("t", turned.Sockets[Directions.PosY].ToString());
    }

    [Fact]
    public void VerticalRotationIndexFollowsTheTile()
    {
        var tile = Tile3D("a", "b", "s@0", "s@1", "c", "d", TileRotation.FourWay);
        var set = CompiledTileSet.Compile(new[] { tile }, Directions.All3D);
        var turned = set.Variants[set.FindVariant(0, 3)];
        Assert.Equal("s@3", turned.Sockets[Directions.PosY].ToString());
        Assert.Equal("s@0", turned.Sockets[Directions.NegY].ToString());
    }

    [Fact]
    public void FullySymmetricTileMergesToOneVariant()
    {
        var tile = Uniform3D("a") with { Rotation = TileRotation.FourWay };
        var set = CompiledTileSet.Compile(new[] { tile }, Directions.All3D);
        Assert.Single(set.Variants);
        for (int turns = 0; turns < 4; turns++)
            Assert.Equal(0, set.FindVariant(0, turns));
    }

    [Fact]
    public void HalfSymmetricTileMergesToTwoVariants()
    {
        var tile = Tile3D("a", "a", "t", "t", "b", "b", TileRotation.FourWay);
        var set = CompiledTileSet.Compile(new[] { tile }, Directions.All3D);
        Assert.Equal(2, set.Variants.Count);
        Assert.Equal(set.FindVariant(0, 0), set.FindVariant(0, 2));
        Assert.Equal(set.FindVariant(0, 1), set.FindVariant(0, 3));
    }

    [Fact]
    public void DifferentTilesWithEqualSocketsStaySeparate()
    {
        var set = CompiledTileSet.Compile(new[] { Uniform3D("a"), Uniform3D("a") }, Directions.All3D);
        Assert.Equal(2, set.Variants.Count);
    }

    [Fact]
    public void UnavailableRotationIsNotFound()
    {
        var set = CompiledTileSet.Compile(new[] { Asymmetric with { Rotation = TileRotation.None } }, Directions.All3D);
        Assert.Equal(0, set.FindVariant(0, 0));
        Assert.Equal(-1, set.FindVariant(0, 1));
    }

    [Fact]
    public void InvalidSocketsAreReportedPerFace()
    {
        var tile = Tile3D("a", "", "t", "t@9", "c", "d");
        var set = CompiledTileSet.Compile(new[] { tile }, Directions.All3D);
        Assert.False(set.IsValid);
        Assert.Equal(new[] { Directions.NegX, Directions.NegY }, set.Errors.Select(e => e.Direction).ToArray());
    }

    [Fact]
    public void EmptyTileSetIsAnError() =>
        Assert.False(CompiledTileSet.Compile(new TileDefinition[0], Directions.All3D).IsValid);

    [Fact]
    public void TwoDimensionalTilesIgnoreVerticalFaces()
    {
        var set = CompiledTileSet.Compile(new[] { Tile2D("a", "b", "c", "d", TileRotation.FourWay) }, Directions.All2D);
        Assert.True(set.IsValid);
        Assert.Equal(4, set.Variants.Count);
    }
}

public class WfcAdjacencyTests
{
    [Fact]
    public void HandWorkedHorizontalAdjacency()
    {
        var a = Tile2D("x", "y", "z", "z");
        var b = Tile2D("y", "x", "z", "w");
        var set = CompiledTileSet.Compile(new[] { a, b }, Directions.All2D);

        Assert.True(set.Allows(0, Directions.PosX, 1));   // a's +X x faces b's -X x
        Assert.False(set.Allows(0, Directions.PosX, 0));  // a's +X x faces a's -X y
        Assert.True(set.Allows(1, Directions.PosX, 0));   // b's +X y faces a's -X y
        Assert.True(set.Allows(0, Directions.PosZ, 0));   // z faces z
        Assert.True(set.Allows(1, Directions.NegZ, 1) == false); // b's -Z w faces b's +Z z
    }

    [Fact]
    public void HandWorkedVerticalAdjacency()
    {
        var lower = Tile3D("s", "s", "top", "ground", "s", "s");
        var upper = Tile3D("s", "s", "air", "top", "s", "s");
        var set = CompiledTileSet.Compile(new[] { lower, upper }, Directions.All3D);

        Assert.True(set.Allows(0, Directions.PosY, 1));
        Assert.False(set.Allows(1, Directions.PosY, 0));
        Assert.True(set.Allows(1, Directions.NegY, 0));
    }

    [Fact]
    public void RotationSpecificVerticalSocketsConnectOnlyAtMatchingRotation()
    {
        var bottom = Tile3D("a", "a", "s@0", "g", "a", "a");
        var top = Tile3D("a", "a", "o", "s@0", "a", "a", TileRotation.FourWay);
        var set = CompiledTileSet.Compile(new[] { bottom, top }, Directions.All3D);

        Assert.True(set.Allows(set.FindVariant(0, 0), Directions.PosY, set.FindVariant(1, 0)));
        for (int turns = 1; turns < 4; turns++)
            Assert.False(set.Allows(set.FindVariant(0, 0), Directions.PosY, set.FindVariant(1, turns)));
    }

    [Fact]
    public void TableIsSymmetricAcrossManyVariants()
    {
        var tiles = Enumerable.Range(0, 40)
            .Select(i => Tile3D($"h{i % 3}", $"h{i % 5}", $"v{i % 2}@{i % 4}", $"v{(i + 1) % 2}@{(i + 2) % 4}", $"h{i % 7}+", $"h{i % 7}-", TileRotation.FourWay))
            .ToArray();
        var set = CompiledTileSet.Compile(tiles, Directions.All3D);
        Assert.True(set.IsValid);
        Assert.True(set.Variants.Count > 64, "needs more than one bitset word");

        int matches = 0;
        for (int a = 0; a < set.Variants.Count; a++)
            for (int b = 0; b < set.Variants.Count; b++)
                foreach (int d in Directions.All3D)
                {
                    bool forward = set.Allows(a, d, b);
                    Assert.Equal(forward, set.Allows(b, Directions.Opposite(d), a));
                    matches += forward ? 1 : 0;
                }
        Assert.True(matches > 0);
    }
}
