using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

/// <summary>Several path sockets and the Doorway Socket (card dgn-1-3-1-doorway-socket, brief §14.1).</summary>
public class DungeonDoorwaySocketTests
{
    private static string[] S(params string[] sockets) => sockets;

    private const string Paths = "corridor, pitfall";

    // Corridor pieces meet pitfalls and each other; the pitfall straight only meets "pitfall".
    private static readonly TileDefinition[] CorridorTiles =
    {
        new(S("corridor, pitfall", "x", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("corridor, pitfall", "corridor, pitfall", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("corridor, pitfall", "x", "v", "v", "corridor, pitfall", "x"), TileRotation.FourWay),
        new(S("corridor, pitfall", "corridor, pitfall", "v", "v", "corridor, pitfall", "x"), TileRotation.FourWay),
        new(S("corridor, pitfall", "corridor, pitfall", "v", "v", "corridor, pitfall", "corridor, pitfall")),
        new(S("x", "x", "v", "v", "x", "x"), TileRotation.None, 20),
        new(S("pitfall", "pitfall", "v", "v", "x", "x"), TileRotation.FourWay, 50),
    };

    private const int PitfallTile = 6;

    private static readonly CompiledTileSet Corridor = CompiledTileSet.Compile(CorridorTiles.ToList(), Directions.All3D);

    private static readonly LayoutSettings Settings = new() { Bounds = new Vector2I(40, 40), RoomCount = 8 };

    private sealed record Run(DungeonLayout Layout, SolveInput Input, int[] Cells);

    private static Run Solve(long seed, CompiledTileSet tiles, string paths, string doorways)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var layout = LayoutGenerator.Generate(Settings, seed, attempt, Vector3I.Zero);
            Assert.True(layout.IsValid, string.Join("; ", layout.Errors));
            var plan = CorridorRouter.Route(layout, Settings, null, 0.3, LayoutSeeds.Corridors(seed, attempt));
            if (!plan.IsValid)
                continue;
            var input = SolveInputs.Corridors(layout, plan, tiles, paths, null, doorways);
            Assert.True(input.IsValid, input.Error);
            var result = new WfcSolver(input.Grid, tiles, input.Initial).Solve(new SolveOptions { Seed = seed, Restrictions = input.Restrictions });
            Assert.Equal(SolveStatus.Solved, result.Status);
            return new Run(layout, input, result.Cells);
        }
        Assert.Fail($"seed {seed}: no layout attempt could be routed.");
        return null;
    }

    private static IEnumerable<int> DoorTiles(Run run) => run.Layout.Doors
        .Select(d => run.Cells[run.Input.Grid.Index(new Vector3I(d.Outside.X, 0, d.Outside.Y))])
        .Select(v => Corridor.Variants[v].TileIndex);

    [Fact]
    public void SocketNames_SplitsTrimsAndDropsEmptyAndRepeatedEntries()
    {
        Assert.Equal(new[] { "corridor", "pitfall" }, DungeonTileRules.SocketNames(" corridor,pitfall , corridor,"));
        Assert.Empty(DungeonTileRules.SocketNames(""));
        Assert.Empty(DungeonTileRules.SocketNames(null));
    }

    [Fact]
    public void PathFaces_CountsAFaceWithAnyListedName()
    {
        int pitfall = Corridor.FindVariant(PitfallTile, 0);
        int alongX = (1 << Directions.PosX) | (1 << Directions.NegX);
        Assert.Equal(alongX, DungeonTileRules.PathFaces(Corridor.Variants[pitfall], Paths));
        Assert.Equal(alongX, DungeonTileRules.PathFaces(Corridor.Variants[pitfall], "pitfall"));
        Assert.Equal(0, DungeonTileRules.PathFaces(Corridor.Variants[pitfall], "corridor"));
    }

    [Fact]
    public void WithoutDoorwaySocket_PitfallsAlsoStandOutsideDoors()
    {
        bool any = false;
        for (long seed = 1; seed <= 20 && !any; seed++)
        {
            var run = Solve(seed, Corridor, Paths, "");
            Assert.Empty(DungeonConnectivity.UnreachableRooms(run.Layout, run.Input, run.Cells, Corridor, Paths));
            any = DoorTiles(run).Contains(PitfallTile);
        }
        Assert.True(any, "The pitfall straight never stood outside a door, so the doorway test below proves nothing.");
    }

    [Fact]
    public void DoorwaySocket_KeepsPitfallsAwayFromDoors_AndTheDungeonConnected()
    {
        int pitfalls = 0;
        for (long seed = 1; seed <= 20; seed++)
        {
            var run = Solve(seed, Corridor, Paths, "corridor");
            Assert.DoesNotContain(PitfallTile, DoorTiles(run));
            Assert.Empty(DungeonConnectivity.UnreachableRooms(run.Layout, run.Input, run.Cells, Corridor, Paths));
            pitfalls += run.Cells.Count(v => v >= 0 && Corridor.Variants[v].TileIndex == PitfallTile);
        }
        Assert.True(pitfalls > 0, "No pitfalls at all: the doorway socket should only keep them from the doors.");
    }

    [Fact]
    public void EmptyDoorwaySocket_GivesTheSameRestrictionsAsBefore()
    {
        var layout = LayoutGenerator.Generate(Settings, 3, 0, Vector3I.Zero);
        var plan = CorridorRouter.Route(layout, Settings, null, 0.3, LayoutSeeds.Corridors(3, 0));
        Assert.True(plan.IsValid, plan.Error);
        var tiles = DungeonTestTiles.Corridor();
        var before = SolveInputs.Corridors(layout, plan, tiles, DungeonTestTiles.Path);
        var after = SolveInputs.Corridors(layout, plan, tiles, " path ", null, " ");
        var a = new WfcSolver(before.Grid, tiles, before.Initial).Solve(new SolveOptions { Seed = 3, Restrictions = before.Restrictions });
        var b = new WfcSolver(after.Grid, tiles, after.Initial).Solve(new SolveOptions { Seed = 3, Restrictions = after.Restrictions });
        Assert.Equal(a.Cells, b.Cells);
    }

    [Fact]
    public void DoorwaySocket_WithoutAMatchingPiece_NamesTheDoorSide()
    {
        // Only the pitfall straight carries "pitfall", and it does not turn: doors on the Z sides cannot be reached.
        var layout = LayoutGenerator.Generate(Settings, 3, 0, Vector3I.Zero);
        var plan = CorridorRouter.Route(layout, Settings, null, 0.3, LayoutSeeds.Corridors(3, 0));
        Assert.Contains(layout.Doors, d => d.Side is DoorSide.PosY or DoorSide.NegY);
        var tiles = CompiledTileSet.Compile(CorridorTiles.Select((t, i) => i == PitfallTile ? new TileDefinition(t.Sockets, TileRotation.None, t.Weight)
            : new TileDefinition(t.Sockets.Select(s => s?.Replace("corridor, pitfall", "corridor")).ToArray(), t.Rotation, t.Weight)).ToList(), Directions.All3D);
        var input = SolveInputs.Corridors(layout, plan, tiles, Paths, null, "pitfall");
        Assert.False(input.IsValid);
        Assert.Contains("'pitfall' on", input.Error);
        Assert.Contains("next to a door", input.Error);
    }

    private static (List<string> Errors, List<string> Warnings) Check(string paths, string doorways) =>
        DungeonTileRules.Check(DungeonTestTiles.Room(), DungeonTestTiles.RoomTiles.Select((_, i) => $"Tile {i}").ToList(), DungeonTestTiles.Door,
            DungeonTestTiles.Edge, Corridor, paths, doorways);

    [Theory]
    [InlineData("corridor, pitfall", "corridor")]
    [InlineData("corridor,pitfall", "")]
    [InlineData("pitfall,corridor", "corridor,pitfall")]
    public void Check_AcceptsListsAndDoorwaysAmongThePathSockets(string paths, string doorways)
    {
        var (errors, warnings) = Check(paths, doorways);
        Assert.Empty(errors);
        Assert.DoesNotContain(warnings, w => w.Contains("Socket"));
    }

    [Fact]
    public void Check_RejectsADoorwayThatIsNotAPathSocket()
    {
        var (errors, _) = Check("corridor", "pitfall");
        Assert.Contains(errors, e => e.Contains("Doorway Socket 'pitfall' is not in Path Socket"));
    }

    [Fact]
    public void Check_WarnsAboutListedNamesNoTileUses()
    {
        var (errors, warnings) = Check("corridor, pitfall, lava", "corridor, ice");
        Assert.Contains(errors, e => e.Contains("'ice' is not in Path Socket"));
        (errors, warnings) = Check("corridor, pitfall, lava", "corridor, lava");
        Assert.Empty(errors);
        Assert.Contains(warnings, w => w.Contains("'lava', a name in Path Socket"));
        Assert.Contains(warnings, w => w.Contains("'lava', a name in Doorway Socket"));
    }

    [Fact]
    public void Check_RejectsADoorwayNoTileUses()
    {
        var (errors, _) = Check("corridor, lava", "lava");
        Assert.Contains(errors, e => e.Contains("so no corridor can reach a door"));
    }
}
