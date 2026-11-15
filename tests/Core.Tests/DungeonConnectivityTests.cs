using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonConnectivityTests
{
    private static readonly CompiledTileSet Corridor = DungeonTestTiles.Corridor();

    private static DungeonResult Generate(long seed) => new DungeonJob(new DungeonRequest
    {
        Layout = new LayoutSettings { Bounds = new Vector2I(40, 40), RoomCount = 8 },
        Origin = new Vector3I(0, 0, 0),
        RoomTiles = DungeonTestTiles.Room(),
        DoorTile = DungeonTestTiles.Door,
        CorridorTiles = Corridor,
        Seed = seed,
    }).RunNow();

    private static int EmptyVariant => DungeonTileRules.EmptyVariant(Corridor, DungeonTestTiles.Path);

    [Fact]
    public void Solved_dungeons_are_connected()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var result = Generate(seed);
            Assert.Equal(DungeonStatus.Solved, result.Status);
            Assert.Empty(result.UnreachableRooms);
            var corridors = result.Solves[^1];
            Assert.Empty(DungeonConnectivity.UnreachableRooms(result.Layout, corridors.Input, corridors.Cells, Corridor, DungeonTestTiles.Path));
        }
    }

    [Fact]
    public void Cutting_the_corridor_outside_a_single_door_room_names_that_room()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var result = Generate(seed);
            var layout = result.Layout;
            int room = Enumerable.Range(1, layout.Rooms.Count - 1).FirstOrDefault(r => layout.Doors.Count(d => d.Room == r) == 1);
            if (room == 0)
                continue;
            var door = layout.Doors.Single(d => d.Room == room);
            var corridors = result.Solves[^1];
            var cells = (int[])corridors.Cells.Clone();
            cells[corridors.Input.Grid.Index(new Vector3I(door.Outside.X, 0, door.Outside.Y))] = EmptyVariant;

            var unreachable = DungeonConnectivity.UnreachableRooms(layout, corridors.Input, cells, Corridor, DungeonTestTiles.Path);
            Assert.Equal(new[] { room }, unreachable);
            string message = DungeonConnectivity.Describe(layout, unreachable);
            Assert.Equal($"Internal error: the dungeon was generated, but 1 room(s) cannot be reached from room 1: room {room + 1} at "
                + $"{layout.GridCell(layout.Rooms[room].Position)}. Nothing was written. Please report this with the seed and settings.", message);
            return;
        }
        Assert.Fail("no layout had a single-door room besides room 1");
    }

    [Fact]
    public void Removing_every_corridor_leaves_only_room_one()
    {
        var result = Generate(4);
        var corridors = result.Solves[^1];
        var lanes = result.Layout.Doors.Select(d => corridors.Input.Grid.Index(new Vector3I(d.Outside.X, 0, d.Outside.Y))).ToHashSet();
        var cells = corridors.Cells.Select((variant, cell) =>
            variant >= 0 && !lanes.Contains(cell) && DungeonTileRules.PathFaces(Corridor.Variants[variant], DungeonTestTiles.Path) != 0 ? EmptyVariant : variant).ToArray();
        var unreachable = DungeonConnectivity.UnreachableRooms(result.Layout, corridors.Input, cells, Corridor, DungeonTestTiles.Path);
        Assert.Equal(Enumerable.Range(1, result.Layout.Rooms.Count - 1), unreachable);
    }
}
