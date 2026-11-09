using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

public class WfcTileSetCheckerTests
{
    private static TileSetReport Check(params TileDefinition[] tiles) =>
        TileSetChecker.Check(CompiledTileSet.Compile(tiles, Directions.All3D));

    [Fact]
    public void CleanSetHasNoWarnings() => Assert.True(Check(Uniform3D("a"), Uniform3D("a")).IsClean);

    [Fact]
    public void UnmatchedFaceIsReportedWithItsLabel()
    {
        var report = Check(Uniform3D("a"), Tile3D("a", "a", "a", "a", "a", "lonely"));
        var face = Assert.Single(report.UnmatchedFaces);
        Assert.Equal(1, face.TileIndex);
        Assert.Equal(Directions.NegZ, face.Direction);
        Assert.Equal("lonely", face.Socket);
    }

    [Fact]
    public void FaceOfARotatedTileIsReportedInTheTilesOwnDirection()
    {
        // A one-sided socket, because a plain label would connect to itself on a rotated copy.
        var tile = Tile3D("lonely+", "a", "a", "a", "a", "a", TileRotation.FourWay);
        var report = Check(Uniform3D("a"), tile);
        var face = Assert.Single(report.UnmatchedFaces);
        Assert.Equal(Directions.PosX, face.Direction);
        Assert.Equal("lonely+", face.Socket);
    }

    [Fact]
    public void RotationSpecificSocketMatchedByAnotherRotationIsFine()
    {
        var top = Tile3D("a", "a", "a", "s@2", "a", "a");
        var bottom = Tile3D("a", "a", "s@0", "a", "a", "a", TileRotation.FourWay);
        Assert.Empty(Check(top, bottom, Uniform3D("a")).UnmatchedFaces);
    }

    [Fact]
    public void TileThatDependsOnAnUnmatchedTileIsEdgeOnly()
    {
        var broken = Tile3D("link", "link", "a", "a", "a", "lonely");
        var dependent = Tile3D("link", "x", "a", "a", "a", "a");
        var partner = Tile3D("x", "a", "a", "a", "a", "a");
        var report = Check(Uniform3D("a"), broken, dependent, partner);
        Assert.Contains(report.UnmatchedFaces, f => f.TileIndex == 1);
        Assert.DoesNotContain(1, report.EdgeOnlyTiles);
    }

    [Fact]
    public void ClosedChainOfTilesIsClean()
    {
        var a = Tile3D("ab", "start", "u", "u", "z", "z");
        var b = Tile3D("end", "ab", "u", "u", "z", "z");
        var cap = Tile3D("start", "end", "u", "u", "z", "z");
        Assert.True(Check(a, b, cap).IsClean);
    }

    [Fact]
    public void TileWhoseOnlyNeighbourIsRemovedIsEdgeOnly()
    {
        var doomed = Tile3D("a", "a", "a", "a", "a", "nothing");
        var reliant = Tile3D("reli", "a", "a", "a", "a", "a");
        var onlyReliantPartner = Tile3D("a", "reli", "a", "a", "a", "nothing");
        var report = Check(Uniform3D("a"), doomed, reliant, onlyReliantPartner);
        Assert.Contains(2, report.EdgeOnlyTiles);
        Assert.DoesNotContain(0, report.EdgeOnlyTiles);
    }
}
