using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// godoowave 1.5: several socket labels on one face, separated by commas.
public class WfcSocketListTests
{
    private static SocketSet Parse(string label, bool vertical = false)
    {
        Assert.True(SocketSet.TryParse(label, vertical, out var set, out string error), error);
        return set;
    }

    private static string ParseError(string label, bool vertical = false)
    {
        Assert.False(SocketSet.TryParse(label, vertical, out _, out string error));
        return error;
    }

    [Theory]
    [InlineData("wall, window")]
    [InlineData("wall,window")]
    [InlineData(" wall ,  window ")]
    public void ListsParseWithOrWithoutSpaces(string label)
    {
        var set = Parse(label);
        Assert.Equal(new[] { new Socket("wall", SocketKind.HorizontalSymmetric), new Socket("window", SocketKind.HorizontalSymmetric) }, set.Labels);
        Assert.Equal("wall, window", set.ToString());
    }

    [Fact]
    public void ASingleLabelIsTheSameSocketAsBefore()
    {
        foreach (var (label, vertical) in new[] { ("wall", false), ("door+", false), ("door-", false), ("stair", true), ("stair@2", true) })
        {
            Assert.True(Socket.TryParse(label, vertical, out var single, out _));
            var set = Parse(label, vertical);
            Assert.Equal(single, Assert.Single(set.Labels));
            Assert.Equal(label, set.ToString());
            Assert.True(set == single);
        }
    }

    [Fact]
    public void KindsMayBeMixedInOneList()
    {
        var set = Parse("door+, wall");
        Assert.Equal(new[] { SocketKind.HorizontalPlus, SocketKind.HorizontalSymmetric }, set.Labels.Select(l => l.Kind));
        var vertical = Parse("stair@1,rock", vertical: true);
        Assert.Equal(new[] { SocketKind.VerticalRotated, SocketKind.VerticalAny }, vertical.Labels.Select(l => l.Kind));
    }

    [Theory]
    [InlineData("wall,,window")]
    [InlineData("wall,")]
    [InlineData(",wall")]
    [InlineData("wall, ,window")]
    public void EmptyEntriesAreRejected(string label) => Assert.Contains("empty entry", ParseError(label));

    [Fact]
    public void BadEntriesAndDuplicatesAreRejected()
    {
        Assert.Contains("'bad label' is not a valid", ParseError("wall, bad label"));
        Assert.Contains("a vertical rotation must be", ParseError("rock, stair@7", vertical: true));
        Assert.Contains("lists 'wall' twice", ParseError("wall, window, wall"));
        Assert.Equal("The socket label is empty.", ParseError("  "));
        Assert.Equal("The socket label is empty.", ParseError(null));
    }

    [Fact]
    public void FacesMatchWhenAnyPairOfLabelsMatches()
    {
        Assert.True(Parse("wall, window").Matches(Parse("window")));
        Assert.True(Parse("wall").Matches(Parse("door, wall")));
        Assert.False(Parse("wall, window").Matches(Parse("door")));
        Assert.True(Parse("door+, wall").Matches(Parse("door-")));
        Assert.False(Parse("door+, x").Matches(Parse("door+")));
        Assert.True(Parse("stair@1, rock", true).Matches(Parse("stair@1", true)));
        Assert.False(Parse("stair@1, rock", true).Matches(Parse("stair@2", true)));
        Assert.True(Parse("a, b").Matches(new Socket("b", SocketKind.HorizontalSymmetric)));
    }

    [Fact]
    public void RotationTurnsEveryRotationSpecificLabel()
    {
        var turned = Parse("stair@0, rock, step@3", true).RotatedY(1);
        Assert.Equal("stair@1, rock, step@0", turned.ToString());
    }

    [Fact]
    public void ATileWithAListFitsNextToEachListedNeighbour()
    {
        // Tile 0's +X face lists wall and window; tiles 1-3 offer wall, window and door on -X.
        var tiles = new[]
        {
            Tile2D("wall, window", "x", "x", "x"),
            Tile2D("x", "wall", "x", "x"),
            Tile2D("x", "window", "x", "x"),
            Tile2D("x", "door", "x", "x"),
        };
        var set = CompiledTileSet.Compile(tiles, Directions.All2D);
        Assert.True(set.IsValid);
        Assert.True(set.Allows(0, Directions.PosX, 1));
        Assert.True(set.Allows(0, Directions.PosX, 2));
        Assert.False(set.Allows(0, Directions.PosX, 3));
        Assert.True(set.Allows(1, Directions.NegX, 0));
        Assert.True(set.Allows(2, Directions.NegX, 0));
    }

    [Fact]
    public void AListAllowsTheSameNeighboursAsDuplicatedTiles()
    {
        // One tile with "a, b" on +X against the same tile duplicated with "a" and with "b".
        var others = new[] { Tile2D("q", "a", "q", "q"), Tile2D("q", "b", "q", "q"), Tile2D("q", "c", "q", "q"), Uniform2D("q") };
        var listed = CompiledTileSet.Compile(others.Prepend(Tile2D("a, b", "q", "q", "q")).ToArray(), Directions.All2D);
        var duplicated = CompiledTileSet.Compile(others.Prepend(Tile2D("b", "q", "q", "q")).Prepend(Tile2D("a", "q", "q", "q")).ToArray(), Directions.All2D);
        for (int other = 0; other < others.Length; other++)
            foreach (int direction in Directions.All2D)
                Assert.Equal(duplicated.Allows(0, direction, other + 2) || duplicated.Allows(1, direction, other + 2),
                    listed.Allows(0, direction, other + 1));
    }

    [Fact]
    public void ACompileErrorNamesTheFace()
    {
        var set = CompiledTileSet.Compile(new[] { Tile2D("a,,b", "a", "a", "a") }, Directions.All2D);
        var issue = Assert.Single(set.Errors);
        Assert.Equal(Directions.PosX, issue.Direction);
        Assert.Contains("empty entry", issue.Message);
    }

    [Fact]
    public void RotatedVariantsOfAListAreMergedWhenIdentical()
    {
        var set = CompiledTileSet.Compile(new[] { Tile2D("a, b", "a, b", "a,b", "a,b", TileRotation.FourWay) }, Directions.All2D);
        Assert.Single(set.Variants);
    }

    [Fact]
    public void TheCheckerCountsAFaceAsMatchedWhenAnyLabelIs()
    {
        var report = TileSetChecker.Check(CompiledTileSet.Compile(new[] { Uniform3D("a"), Tile3D("lonely, a", "a", "a", "a", "a", "a") }, Directions.All3D));
        Assert.True(report.IsClean);

        var lonely = TileSetChecker.Check(CompiledTileSet.Compile(new[] { Uniform3D("a"), Tile3D("lonely, alone", "a", "a", "a", "a", "a") }, Directions.All3D));
        Assert.Equal("lonely, alone", Assert.Single(lonely.UnmatchedFaces).Socket);
    }

    [Fact]
    public void ExternalSocketsMeetAnyLabel()
    {
        var set = CompiledTileSet.Compile(new[] { Uniform3D("a"), Tile3D("lonely, door+", "a", "a", "a", "a", "a") }, Directions.All3D);
        Assert.True(Socket.TryParse("door-", false, out var outside, out _));
        Assert.True(TileSetChecker.Check(set, new[] { outside }).IsClean);
        Assert.False(SocketEditing.UnmatchedFaces(set, 1, new[] { outside })[Directions.PosX]);
    }

    [Fact]
    public void ABoundarySocketHoldsATileWhoseFaceListsIt()
    {
        // Only the boundary offers "rock" from below; tile 0 lists it next to "v".
        var grid = GridTopology.Create3D(new Vector3I(4, 3, 4));
        var tiles = new[] { Tile3D("a", "a", "v", "rock, v", "a", "a"), Tile3D("a", "a", "w", "w", "a", "a") };
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        var boundary = new Socket?[Directions.Count];
        Assert.True(Socket.TryParse("rock", true, out var rock, out _));
        boundary[Directions.NegY] = rock;
        var result = new WfcSolver(grid, set).Solve(new SolveOptions { Seed = 3, Boundary = boundary });
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(grid, set, result.Cells);
        for (int cell = 0; cell < grid.CellCount; cell++)
            if (grid.Coordinates(cell).Y == 0)
                Assert.Equal(0, set.Variants[result.Cells[cell]].TileIndex);
    }

    [Fact]
    public void PathFacesCountAListThatNamesThePathSocket()
    {
        var set = CompiledTileSet.Compile(new[] { Tile2D("path, road", "g", "road", "g") }, Directions.All2D);
        Assert.Equal(1 << Directions.PosX, PathRouter.PathFaces(set, "path")[0]);
        Assert.Equal(1 << Directions.PosX | 1 << Directions.PosZ, PathRouter.PathFaces(set, "road")[0]);
    }

    [Fact]
    public void AGuidedPathRoutesThroughFacesWithLists()
    {
        // The flat path set of WfcPathRouterTests, with every path face also offering "road".
        const string P = "path, road";
        var tiles = new[]
        {
            Tile2D("g", "g", "g", "g"),
            Tile2D(P, P, "g", "g", TileRotation.FourWay),
            Tile2D(P, "g", P, "g", TileRotation.FourWay),
            Tile2D(P, P, P, "g", TileRotation.FourWay),
            Tile2D(P, P, P, P),
            Tile2D(P, "g", "g", "g", TileRotation.FourWay),
        };
        var grid = GridTopology.Create2D(10, 10);
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        Assert.True(set.IsValid);
        var faces = PathRouter.PathFaces(set, "path");
        var initial = Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray();
        initial[grid.Index(new Vector3I(1, 0, 1))] = System.Array.IndexOf(faces, 1 << Directions.PosX);
        initial[grid.Index(new Vector3I(8, 0, 7))] = System.Array.IndexOf(faces, 1 << Directions.NegX);

        var plan = PathRouter.Plan(grid, set, initial, "path", 0.3, 5);
        Assert.True(plan.IsValid, string.Join("; ", plan.Errors.Select(e => e.Message)));
        Assert.Equal(1, plan.RouteCount);
        var result = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = 1, Restrictions = plan.Restrictions });
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(grid, set, result.Cells);
        for (int cell = 0; cell < grid.CellCount; cell++)
            if (initial[cell] == WfcSolver.Free)
                Assert.Equal(plan.RouteFaces[cell], faces[result.Cells[cell]]);
    }

    [Fact]
    public void SuggestionsListEachLabelOfAList()
    {
        var tiles = new[] { Tile3D("wall, door+", "a", "rock, stair@1", "a", "a", "a") };
        Assert.Equal(new[] { "a", "door+", "door-", "wall" }, SocketEditing.Suggestions(tiles, false));
        Assert.Equal(new[] { "a", "rock", "stair@1" }, SocketEditing.Suggestions(tiles, true));
    }

    [Fact]
    public void EditsOnRotatedVariantsTurnEachVerticalLabelBack()
    {
        Assert.Equal("stair@1, rock", SocketEditing.ToLocalLabel("stair@2,rock", Directions.PosY, 1));
        Assert.Equal("wall,window", SocketEditing.ToLocalLabel(" wall,window ", Directions.PosX, 1));
    }

    // The entry after the last comma is the one being typed: it filters the suggestions, and a
    // picked suggestion replaces it. Without a comma this is the 1.4 behaviour.
    [Theory]
    [InlineData("", "wall", "wall")]
    [InlineData("wa", "wall", "wall")]
    [InlineData("wall", "window", "window")]
    [InlineData("wall,", "window", "wall, window")]
    [InlineData("wall, win", "window", "wall, window")]
    [InlineData("wall,win", "window", "wall, window")]
    [InlineData("wall, window, ", "window", "wall, window")]
    [InlineData("wall, ", "wall", "wall")]
    public void APickedSuggestionReplacesTheEntryBeingTyped(string typed, string suggestion, string expected) =>
        Assert.Equal(expected, SocketEditing.PickSuggestion(typed, suggestion));

    [Theory]
    [InlineData("", "")]
    [InlineData("wa", "wa")]
    [InlineData("wall, win", "win")]
    [InlineData("wall,", "")]
    public void TheEntryBeingTypedFiltersTheSuggestions(string typed, string filter) =>
        Assert.Equal(filter, SocketEditing.EntryBeingTyped(typed));
}
