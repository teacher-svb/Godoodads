using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

public class DungeonDoorTests
{
    private readonly ITestOutputHelper _output;

    public DungeonDoorTests(ITestOutputHelper output) => _output = output;

    private static DungeonLayout Layout(long seed, LayoutSettings settings = null, OccupancyGrid occupied = null)
    {
        settings ??= new LayoutSettings();
        var layout = new DungeonLayout();
        RoomPlacer.Place(layout, settings, seed, occupied);
        RoomGraph.Build(layout, settings.ExtraConnections, seed);
        DoorPlacer.Place(layout, settings, occupied);
        return layout;
    }

    private static bool OnSide(RoomRect room, Door door) => door.Side switch
    {
        DoorSide.PosX => door.Cell.X == room.End.X - 1,
        DoorSide.NegX => door.Cell.X == room.Position.X,
        DoorSide.PosY => door.Cell.Y == room.End.Y - 1,
        _ => door.Cell.Y == room.Position.Y,
    };

    private static void AssertRules(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied, string context)
    {
        Assert.True(layout.IsValid, $"{context}: {string.Join("; ", layout.Errors)}");
        int lane = DoorPlacer.LaneLength(settings) + 1;
        for (int r = 0; r < layout.Rooms.Count; r++)
        {
            var room = layout.Rooms[r];
            var doors = layout.Doors.Where(d => d.Room == r).ToList();
            Assert.True(doors.Count >= 1, $"{context}: room {r} has no door");
            Assert.True(doors.Count <= settings.MaxDoorsPerRoom, $"{context}: room {r} has {doors.Count} doors");
            Assert.Equal(doors.Count, doors.Select(d => d.Side).Distinct().Count());
            foreach (var door in doors)
            {
                Assert.True(room.IsOnEdge(door.Cell) && !room.IsCorner(door.Cell), $"{context}: door {door} not on a wall or on a corner");
                Assert.True(OnSide(room, door), $"{context}: door {door} not on its side");
                for (int step = 1; step <= lane; step++)
                {
                    var cell = door.Cell + door.Side.Offset() * step;
                    Assert.True(cell.X >= 0 && cell.Y >= 0 && cell.X < settings.Bounds.X && cell.Y < settings.Bounds.Y, $"{context}: door {door} faces the bounds edge");
                    Assert.False(occupied?.IsTaken(cell) ?? false, $"{context}: door {door} faces an occupied cell");
                    for (int o = 0; o < layout.Rooms.Count; o++)
                        if (o != r)
                            Assert.False(layout.Rooms[o].Grow(settings.WallClearance).Contains(cell), $"{context}: door {door} runs into room {o}");
                }
            }
        }
    }

    [Fact]
    public void Every_rule_holds_and_every_room_has_a_door_over_200_seeds()
    {
        var settings = new LayoutSettings();
        for (long seed = 0; seed < 200; seed++)
            AssertRules(Layout(seed, settings), settings, null, $"seed {seed}");
    }

    [Fact]
    public void Rules_hold_with_more_clearance_fewer_doors_and_hand_placed_cells()
    {
        var occupied = new OccupancyGrid(new Vector2I(64, 64));
        occupied.Take(new RoomRect(new Vector2I(30, 10), new Vector2I(3, 40)));
        var variants = new[]
        {
            new LayoutSettings { Bounds = new Vector2I(64, 64), WallClearance = 2, Gap = 5 },
            new LayoutSettings { Bounds = new Vector2I(64, 64), WallClearance = 0, Gap = 2, ExtraConnections = 1 },
            new LayoutSettings { Bounds = new Vector2I(64, 64), MaxDoorsPerRoom = 1 },
            new LayoutSettings { Bounds = new Vector2I(64, 64), MaxDoorsPerRoom = 2, ExtraConnections = 0.5 },
            new LayoutSettings { Bounds = new Vector2I(64, 64), MinRoomSize = new Vector2I(3, 3), MaxRoomSize = new Vector2I(4, 4), RoomCount = 30 },
        };
        foreach (var settings in variants)
            for (long seed = 0; seed < 50; seed++)
                AssertRules(Layout(seed, settings, occupied), settings, occupied, $"{settings} seed {seed}");
    }

    [Fact]
    public void One_door_per_room_when_capped_at_one()
    {
        var settings = new LayoutSettings { MaxDoorsPerRoom = 1, ExtraConnections = 1 };
        var layout = Layout(3, settings);
        Assert.All(Enumerable.Range(0, layout.Rooms.Count), r => Assert.Single(layout.Doors, d => d.Room == r));
    }

    [Fact]
    public void Doors_face_each_other_across_the_gap()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(30, 20) };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(2, 4), new Vector2I(7, 7)));   // centre (5.5, 7.5)
        layout.Rooms.Add(new RoomRect(new Vector2I(16, 6), new Vector2I(6, 9)));  // centre (19, 10.5)
        RoomGraph.Build(layout, 0, 1);
        DoorPlacer.Place(layout, settings);

        Assert.True(layout.IsValid);
        Assert.Equal(new Door(0, new Vector2I(8, 9), DoorSide.PosX), layout.Doors.Single(d => d.Room == 0));
        Assert.Equal(new Door(1, new Vector2I(16, 7), DoorSide.NegX), layout.Doors.Single(d => d.Room == 1));
        Assert.Equal(new Vector2I(9, 9), layout.Doors[0].Outside);
    }

    [Fact]
    public void Edges_through_the_same_side_share_its_door()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(40, 40) };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(2, 10), new Vector2I(6, 12)));  // tall room on the left
        layout.Rooms.Add(new RoomRect(new Vector2I(20, 2), new Vector2I(5, 5)));   // up right
        layout.Rooms.Add(new RoomRect(new Vector2I(20, 25), new Vector2I(5, 5)));  // down right
        RoomGraph.Build(layout, 1, 1);
        DoorPlacer.Place(layout, settings);

        Assert.True(layout.IsValid);
        var left = layout.Doors.Where(d => d.Room == 0).ToList();
        Assert.Single(left);
        Assert.Equal(DoorSide.PosX, left[0].Side);
    }

    [Fact]
    public void A_door_that_already_faces_the_neighbour_is_reused()
    {
        // B lies to the right of A, C mostly above it. A's door towards B (+X) also faces C, so A keeps one door.
        var settings = new LayoutSettings { Bounds = new Vector2I(48, 40) };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(10, 20), new Vector2I(6, 6)));
        layout.Rooms.Add(new RoomRect(new Vector2I(30, 20), new Vector2I(5, 5)));
        layout.Rooms.Add(new RoomRect(new Vector2I(18, 2), new Vector2I(5, 5)));
        RoomGraph.Build(layout, 0, 1);
        Assert.Equal(new[] { (0, 1), (0, 2) }, layout.Edges.Select(e => (e.A, e.B)));
        DoorPlacer.Place(layout, settings);

        Assert.True(layout.IsValid);
        Assert.Equal(DoorSide.PosX, Assert.Single(layout.Doors, d => d.Room == 0).Side);
    }

    [Fact]
    public void A_room_with_no_way_out_is_an_error()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(20, 9), Margin = 0 };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(0, 0), new Vector2I(5, 9)));
        layout.Rooms.Add(new RoomRect(new Vector2I(12, 0), new Vector2I(5, 9)));
        var occupied = new OccupancyGrid(settings.Bounds);
        for (int y = 0; y < 9; y++)
            occupied.Take(new Vector2I(6, y)); // a wall of hand-placed cells right outside room 1
        RoomGraph.Build(layout, 0, 1);
        DoorPlacer.Place(layout, settings, occupied);

        Assert.False(layout.IsValid);
        Assert.StartsWith("Room 1 ", Assert.Single(layout.Errors));
        Assert.DoesNotContain(layout.Doors, d => d.Room == 0);
        Assert.Single(layout.Doors, d => d.Room == 1);
    }

    [Fact]
    public void Doors_are_fixed_for_a_seed()
    {
        Assert.Equal(Layout(9).Doors, Layout(9).Doors);
    }

    [Fact]
    public void Prints_layouts_for_the_review()
    {
        foreach (long seed in new long[] { 1, 2 })
        {
            var settings = new LayoutSettings();
            var layout = Layout(seed, settings);
            _output.WriteLine($"seed {seed}: {layout.Rooms.Count} rooms, {layout.Edges.Count} edges ({layout.Edges.Count(e => !e.IsTreeEdge)} loops), {layout.Doors.Count} doors");
            _output.WriteLine(LayoutText.Render(layout, settings.Bounds));
            Assert.Equal(layout.Doors.Count, LayoutText.Render(layout, settings.Bounds).Count(c => c == 'D'));
        }
    }
}
