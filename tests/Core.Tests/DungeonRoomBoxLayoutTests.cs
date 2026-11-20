using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

/// <summary>Room boxes in the layout (brief godoodungeon §13.2; card dgn-1-2-layout).</summary>
public class DungeonRoomBoxLayoutTests
{
    private readonly ITestOutputHelper _output;

    public DungeonRoomBoxLayoutTests(ITestOutputHelper output) => _output = output;

    private static readonly LayoutSettings Settings = new() { Bounds = new Vector2I(48, 48), RoomCount = 8, BoundsHeight = 3 };

    private static RoomBoxSpec Entrance => new("Entrance", new RoomRect(new Vector2I(2, 2), new Vector2I(6, 5)), 0, 1, RoomBoxMode.Generated);

    // A Built boss room in the far corner, with a door on its top side (NegY) and one on its left side (NegX).
    private static RoomBoxSpec Boss(params Vector2I[] doors) =>
        new("Boss", new RoomRect(new Vector2I(28, 28), new Vector2I(8, 8)), 0, 2, RoomBoxMode.Built,
            doors.Length > 0 ? doors : new[] { new Vector2I(32, 28), new Vector2I(28, 31) });

    private static DungeonLayout Layout(long seed, LayoutSettings settings = null, OccupancyGrid occupied = null, params RoomBoxSpec[] boxes) =>
        LayoutGenerator.Generate(settings ?? Settings, seed, 0, Vector3I.Zero, occupied, boxes);

    [Fact]
    public void Boxes_are_the_first_rooms_where_they_are_and_generated_rooms_fill_the_rest()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var layout = Layout(seed, null, null, Entrance, Boss());
            Assert.True(layout.IsValid, string.Join(" ", layout.Errors));
            Assert.Equal(8, layout.Rooms.Count);
            Assert.Equal(Entrance.Rect, layout.Rooms[0]);
            Assert.Equal(Boss().Rect, layout.Rooms[1]);
            Assert.Equal(2, layout.Boxes.Count);
            Assert.True(layout.IsBuilt(1) && !layout.IsBuilt(0) && !layout.IsBuilt(2));
            Assert.Equal(2, layout.Height(1));
            Assert.Equal(1, layout.Height(4));
            for (int room = 2; room < layout.Rooms.Count; room++)
                Assert.False(layout.Rooms[room].Overlaps(layout.Rooms[0], Settings.Gap) || layout.Rooms[room].Overlaps(layout.Rooms[1], Settings.Gap));
        }
        _output.WriteLine(LayoutText.Render(Layout(3, null, null, Entrance, Boss()), Settings.Bounds));
    }

    [Fact]
    public void Every_built_door_is_used_by_an_edge_and_built_rooms_get_no_other_doors()
    {
        var withFour = Boss(new Vector2I(32, 28), new Vector2I(28, 31), new Vector2I(35, 30), new Vector2I(31, 35));
        for (long seed = 0; seed < 30; seed++)
            foreach (var boss in new[] { Boss(), withFour })
            {
                var settings = Settings;
                var layout = Layout(seed, settings, null, Entrance, boss);
                Assert.True(layout.IsValid, string.Join(" ", layout.Errors));
                var doors = Enumerable.Range(0, layout.Doors.Count).Where(d => layout.Doors[d].Room == 1).ToList();
                Assert.Equal(RoomBoxes.BuiltDoors(boss).Select(d => d.Cell), doors.Select(d => layout.Doors[d].Cell));
                foreach (int door in doors)
                    Assert.Contains(layout.EdgeDoors, e => e.A == door || e.B == door);
                // Each edge's doors belong to its rooms.
                for (int e = 0; e < layout.Edges.Count; e++)
                {
                    Assert.Equal(layout.Edges[e].A, layout.Doors[layout.EdgeDoors[e].A].Room);
                    Assert.Equal(layout.Edges[e].B, layout.Doors[layout.EdgeDoors[e].B].Room);
                }
                // The generated box gets doors from the door placer, on its ring.
                Assert.All(layout.Doors.Where(d => d.Room == 0), d => Assert.True(layout.Rooms[0].IsOnEdge(d.Cell) && !layout.Rooms[0].IsCorner(d.Cell)));
                Assert.Contains(layout.Doors, d => d.Room == 0);
            }
    }

    [Fact]
    public void Built_doors_face_out_of_their_side_and_corner_or_inner_cells_are_not_doors()
    {
        var boss = Boss(new Vector2I(32, 28), new Vector2I(28, 31), new Vector2I(35, 30), new Vector2I(31, 35),
            new Vector2I(28, 28), new Vector2I(31, 31));
        var doors = RoomBoxes.BuiltDoors(boss);
        Assert.Equal(new[] { DoorSide.NegY, DoorSide.NegX, DoorSide.PosX, DoorSide.PosY }, doors.Select(d => d.Side));
    }

    [Fact]
    public void Room_count_includes_the_boxes()
    {
        var two = Layout(1, Settings with { RoomCount = 2 }, null, Entrance, Boss());
        Assert.Equal(2, two.Rooms.Count);
        // With only two rooms, the boss's second door has no room left to lead to.
        Assert.Equal("Room box `Boss` (room 2): the door at (28, 0, 31) gets no corridor, because the room is already joined to every other room.",
            Assert.Single(two.Warnings));

        var more = Layout(1, Settings with { RoomCount = 1 }, null, Entrance, Boss());
        Assert.True(more.IsValid);
        Assert.Equal(2, more.Rooms.Count);
        Assert.Contains(more.Warnings, w => w.Contains("2 room boxes but Room Count is 1"));

        Assert.Equal(5, Layout(1, Settings with { RoomCount = 5 }, null, Entrance, Boss()).Rooms.Count);

        // One box alone is not a dungeon.
        var alone = Layout(1, Settings with { RoomCount = 1 }, null, Entrance);
        Assert.False(alone.IsValid);
    }

    [Fact]
    public void Every_check_names_the_box()
    {
        string Error(LayoutSettings settings, OccupancyGrid occupied, params RoomBoxSpec[] boxes)
        {
            var layout = Layout(1, settings, occupied, boxes);
            Assert.False(layout.IsValid);
            return string.Join(" ", layout.Errors);
        }
        var small = Entrance with { Rect = new RoomRect(new Vector2I(2, 2), new Vector2I(2, 5)) };
        Assert.Contains("Room box `Entrance` (room 1) is 2 × 5 cells", Error(Settings, null, small));
        var outside = Entrance with { Rect = new RoomRect(new Vector2I(36, 2), new Vector2I(6, 5)) };
        Assert.Contains("Room box `Entrance` (room 1) is outside the bounds or within the Margin", Error(Settings, null, outside with { Rect = new RoomRect(new Vector2I(44, 2), new Vector2I(6, 5)) }));
        var atEdge = Entrance with { Rect = new RoomRect(new Vector2I(0, 2), new Vector2I(6, 5)) };
        Assert.Contains("within the Margin of 1", Error(Settings, null, atEdge));
        Assert.Contains("starts 1 layer(s) above the bottom", Error(Settings, null, Entrance with { Layer = 1 }));
        Assert.Contains("is 4 layer(s) high, but the bounds have 3", Error(Settings, null, Entrance with { Height = 4 }));
        var close = Boss() with { Rect = new RoomRect(new Vector2I(9, 2), new Vector2I(8, 8)), DoorCells = new[] { new Vector2I(12, 9) } };
        Assert.Contains("Room box `Boss` (room 2) is closer than the Gap of 3 cells to Room box `Entrance` (room 1)", Error(Settings, null, Entrance, close));
        var occupied = new OccupancyGrid(Settings.Bounds);
        occupied.Take(new Vector2I(4, 4));
        Assert.Contains("Room box `Entrance` (room 1) is Generated, but covers cells you placed", Error(Settings, occupied, Entrance));
        Assert.Contains("Room box `Boss` (room 2) is Built but has no door", Error(Settings, null, Entrance, Boss(new Vector2I(28, 28))));

        // A Built box may cover cells you placed: they are its cells.
        var built = new OccupancyGrid(Settings.Bounds);
        foreach (var cell in Boss().Rect.Cells())
            built.Take(cell);
        Assert.True(Layout(1, Settings, built, Entrance, Boss()).IsValid);
    }

    [Fact]
    public void A_built_door_whose_lane_leaves_the_bounds_is_an_error_naming_the_box_and_the_door()
    {
        // The box sits at the Margin of 1; a door on its left wall would need 2 cells outside it.
        var boss = new RoomBoxSpec("Boss", new RoomRect(new Vector2I(1, 20), new Vector2I(8, 8)), 0, 1, RoomBoxMode.Built,
            new[] { new Vector2I(1, 23), new Vector2I(4, 20) });
        var layout = Layout(1, Settings, null, Entrance, boss);
        Assert.False(layout.IsValid);
        Assert.Contains(layout.Errors, e => e.StartsWith("Room box `Boss` (room 2): the door at (1, 0, 23) has no free way out"));
    }

    [Fact]
    public void Boxes_keep_their_numbers_and_the_same_seed_gives_the_same_layout()
    {
        var first = Layout(7, null, null, Entrance, Boss());
        var again = Layout(7, null, null, Entrance, Boss());
        Assert.Equal(first.Rooms, again.Rooms);
        Assert.Equal(first.Edges, again.Edges);
        Assert.Equal(first.Doors, again.Doors);
        Assert.Equal(first.EdgeDoors, again.EdgeDoors);
        for (long seed = 0; seed < 10; seed++)
        {
            var layout = Layout(seed, null, null, Entrance, Boss());
            Assert.Equal("Room box `Boss` (room 2)", layout.Describe(1));
            Assert.Equal("room 3", layout.Describe(2));
        }
    }

    [Fact]
    public void Layouts_without_boxes_are_the_same_as_before()
    {
        // The same random stream as 1.1: no boxes, no change. Compared with the room placer alone.
        for (long seed = 0; seed < 10; seed++)
        {
            var withEmpty = LayoutGenerator.Generate(Settings, seed, 0, Vector3I.Zero, null, new List<RoomBoxSpec>());
            var without = LayoutGenerator.Generate(Settings, seed);
            Assert.Equal(without.Rooms, withEmpty.Rooms);
            var placed = new DungeonLayout();
            RoomPlacer.Place(placed, Settings, LayoutSeeds.Layout(seed, 0));
            Assert.Equal(placed.Rooms, without.Rooms);
        }
    }
}
