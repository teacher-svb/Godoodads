using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonLayoutValidationTests
{
    private static string SingleError(LayoutSettings settings) => Assert.Single(LayoutValidator.Validate(settings));

    [Fact]
    public void Default_settings_are_valid()
    {
        Assert.Empty(LayoutValidator.Validate(new LayoutSettings()));
    }

    [Theory]
    [InlineData(1, "Room Count is 1. A dungeon needs at least 2 rooms.")]
    [InlineData(0, "Room Count is 0. A dungeon needs at least 2 rooms.")]
    public void Room_count_below_two(int count, string message) =>
        Assert.Equal(message, SingleError(new LayoutSettings { RoomCount = count }));

    [Fact]
    public void Rooms_too_small_for_a_door()
    {
        string error = SingleError(new LayoutSettings { MinRoomSize = new Vector2I(2, 5) });
        Assert.StartsWith("Min Room Size is 2 × 5. Rooms need at least 3 cells", error);
    }

    [Theory]
    [InlineData(10, 5, "X")]
    [InlineData(5, 10, "Z")]
    public void Minimum_above_maximum_names_the_axis(int minX, int minY, string axis)
    {
        string error = SingleError(new LayoutSettings { MinRoomSize = new Vector2I(minX, minY), MaxRoomSize = new Vector2I(9, 9) });
        Assert.EndsWith($"on {axis}.", error);
        Assert.StartsWith("Min Room Size", error);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(2, 4, 5)]
    [InlineData(0, 1, 2)]
    public void Gap_below_the_minimum_for_the_clearance(int clearance, int gap, int needed)
    {
        string error = SingleError(new LayoutSettings { WallClearance = clearance, Gap = gap });
        Assert.StartsWith($"Gap is {gap}, but Wall Clearance {clearance} needs a gap of at least {needed}", error);
    }

    [Fact]
    public void Other_numbers_out_of_range()
    {
        Assert.StartsWith("Wall Clearance is -1", SingleError(new LayoutSettings { WallClearance = -1 }));
        Assert.StartsWith("Margin is -2", SingleError(new LayoutSettings { Margin = -2 }));
        Assert.StartsWith("Extra Connections is 1.5", SingleError(new LayoutSettings { ExtraConnections = 1.5 }));
        Assert.StartsWith("Max Doors Per Room is 0", SingleError(new LayoutSettings { MaxDoorsPerRoom = 0 }));
        Assert.StartsWith("Attempts Per Room is 0", SingleError(new LayoutSettings { AttemptsPerRoom = 0 }));
        Assert.StartsWith("Room Height is 0", SingleError(new LayoutSettings { RoomHeight = 0 }));
        Assert.StartsWith("The bounds are empty", Assert.Single(LayoutValidator.Validate(new LayoutSettings { Bounds = new Vector2I(0, 10) }),
            e => e.StartsWith("The bounds")));
    }

    [Fact]
    public void A_room_that_does_not_fit_the_bounds()
    {
        string error = SingleError(new LayoutSettings { Bounds = new Vector2I(20, 6), MinRoomSize = new Vector2I(5, 5), Margin = 1 });
        Assert.Equal("The smallest room (5 × 5) with a margin of 1 needs 7 × 7 cells, but the bounds are 20 × 6 on X and Z. "
            + "Enlarge the bounds or lower Min Room Size or Margin.", error);
    }

    [Fact]
    public void Several_problems_are_all_reported()
    {
        var errors = LayoutValidator.Validate(new LayoutSettings { RoomCount = 1, Gap = 0, MaxDoorsPerRoom = 0 });
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void The_generator_stops_on_invalid_settings()
    {
        var layout = LayoutGenerator.Generate(new LayoutSettings { RoomCount = 1 }, 1);
        Assert.False(layout.IsValid);
        Assert.Empty(layout.Rooms);
    }

    [Fact]
    public void The_generator_gives_rooms_edges_and_doors()
    {
        var settings = new LayoutSettings();
        for (long seed = 0; seed < 50; seed++)
        {
            var layout = LayoutGenerator.Generate(settings, seed);
            Assert.True(layout.IsValid, string.Join("; ", layout.Errors));
            Assert.True(layout.Rooms.Count >= 2);
            Assert.Equal(layout.Rooms.Count - 1, layout.Edges.Count(e => e.IsTreeEdge));
            Assert.All(Enumerable.Range(0, layout.Rooms.Count), r => Assert.Contains(layout.Doors, d => d.Room == r));
        }
    }

    [Fact]
    public void Attempts_give_different_layouts_and_the_same_attempt_repeats()
    {
        var settings = new LayoutSettings();
        Assert.Equal(LayoutGenerator.Generate(settings, 5, 0).Rooms, LayoutGenerator.Generate(settings, 5, 0).Rooms);
        Assert.NotEqual(LayoutGenerator.Generate(settings, 5, 0).Rooms, LayoutGenerator.Generate(settings, 5, 1).Rooms);
    }

    [Fact]
    public void Fewer_than_two_rooms_is_an_error_after_the_shortfall_warning()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(12, 12), MinRoomSize = new Vector2I(5, 5), MaxRoomSize = new Vector2I(5, 5) };
        var layout = LayoutGenerator.Generate(settings, 3);
        Assert.Single(layout.Rooms);
        Assert.StartsWith("Placed 1 of 12 rooms", Assert.Single(layout.Warnings));
        Assert.StartsWith("Only 1 room(s) fit in the bounds; a dungeon needs at least 2.", Assert.Single(layout.Errors));
    }

    [Fact]
    public void Layout_errors_name_gridmap_cells_from_the_origin()
    {
        var layout = new DungeonLayout { Origin = new Vector3I(10, 2, -4) };
        Assert.Equal(new Vector3I(13, 2, 1), layout.GridCell(new Vector2I(3, 5)));

        // A room boxed in by hand-placed cells: the error names its corner in GridMap cells.
        var settings = new LayoutSettings { Bounds = new Vector2I(30, 30), RoomCount = 2, MinRoomSize = new Vector2I(5, 5), MaxRoomSize = new Vector2I(5, 5), Gap = 3 };
        var occupied = new OccupancyGrid(settings.Bounds);
        var generated = LayoutGenerator.Generate(settings, 1, 0, new Vector3I(100, 0, 200), occupied);
        Assert.True(generated.IsValid);
        var first = generated.Rooms[0];
        foreach (var cell in first.Grow(settings.WallClearance + 1).Cells())
            if (!first.Grow(settings.WallClearance).Contains(cell))
                occupied.Take(cell);
        var boxed = new DungeonLayout { Origin = new Vector3I(100, 0, 200) };
        boxed.Rooms.AddRange(generated.Rooms);
        RoomGraph.Build(boxed, 0, 1);
        DoorPlacer.Place(boxed, settings, occupied);
        string error = Assert.Single(boxed.Errors);
        Assert.StartsWith($"Room 1 at {boxed.GridCell(first.Position)} has no wall where a corridor can leave it", error);
        Assert.Equal(new Vector3I(100 + first.Position.X, 0, 200 + first.Position.Y), boxed.GridCell(first.Position));
    }
}
