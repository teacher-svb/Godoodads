using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// Brief §13.1: editing sockets in the preview.
public class WfcSocketEditingTests
{
    [Fact]
    public void AnEditOnARotatedVariantLandsOnTheTilesOwnFace()
    {
        // After one quarter turn the tile's +X face points to -Z, so -Z on the variant is +X on the tile.
        Assert.Equal(Directions.PosX, SocketEditing.LocalFace(Directions.NegZ, 1));
        Assert.Equal(Directions.PosY, SocketEditing.LocalFace(Directions.PosY, 3));
        for (int turns = 0; turns < 4; turns++)
            foreach (int local in Directions.All3D)
                Assert.Equal(local, SocketEditing.LocalFace(Directions.RotateY(local, turns), turns));
    }

    [Fact]
    public void RotationSpecificVerticalLabelsAreTurnedBack()
    {
        Assert.Equal("stair@1", SocketEditing.ToLocalLabel("stair@2", Directions.PosY, 1));
        Assert.Equal("stair@3", SocketEditing.ToLocalLabel("stair@0", Directions.NegY, 1));
        Assert.Equal("stair", SocketEditing.ToLocalLabel("stair", Directions.PosY, 2));
        Assert.Equal("path+", SocketEditing.ToLocalLabel(" path+ ", Directions.PosX, 3));
        Assert.Equal("bad label", SocketEditing.ToLocalLabel("bad label", Directions.PosY, 1));
    }

    [Fact]
    public void SuggestionsIncludePartnersAndKeepFaceKindsApart()
    {
        var tiles = new[]
        {
            Tile3D("path+", "open", "air", "ground", "open", "wall"),
            Tile3D("open", "open", "stair@1", "ground", "open", "open"),
        };
        Assert.Equal(new[] { "open", "path+", "path-", "wall" }, SocketEditing.Suggestions(tiles, false));
        Assert.Equal(new[] { "air", "ground", "stair@1" }, SocketEditing.Suggestions(tiles, true));
        Assert.Equal(new[] { "path+", "path-" }, SocketEditing.Suggestions(tiles, false, "PA"));
    }

    [Fact]
    public void FacesNothingCanFaceAreFound()
    {
        // B's +X "lonely" has no partner; everything else matches.
        var tiles = new[] { Uniform3D("a"), Tile3D("lonely", "a", "a", "a", "a", "a") };
        var set = CompiledTileSet.Compile(tiles, Directions.All3D);
        Assert.True(set.IsValid);
        Assert.All(SocketEditing.UnmatchedFaces(set, set.FindVariant(0, 0)), Assert.False);
        var unmatched = SocketEditing.UnmatchedFaces(set, set.FindVariant(1, 0));
        Assert.True(unmatched[Directions.PosX]);
        Assert.Single(System.Array.FindAll(unmatched, u => u));
    }
}
