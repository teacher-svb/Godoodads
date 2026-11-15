using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

public class DungeonRoomPlacementTests
{
    private readonly ITestOutputHelper _output;

    public DungeonRoomPlacementTests(ITestOutputHelper output) => _output = output;

    private static DungeonLayout Place(LayoutSettings settings, long seed, OccupancyGrid occupied = null)
    {
        var layout = new DungeonLayout();
        RoomPlacer.Place(layout, settings, seed, occupied);
        return layout;
    }

    [Fact]
    public void Rooms_never_overlap_keep_the_gap_and_margin_and_sizes_over_200_seeds()
    {
        var settings = new LayoutSettings();
        int total = 0;
        for (long seed = 0; seed < 200; seed++)
        {
            var rooms = Place(settings, seed).Rooms;
            total += rooms.Count;
            Assert.True(rooms.Count >= 2, $"seed {seed}: only {rooms.Count} rooms");
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                Assert.True(room.IsInside(settings.Bounds, settings.Margin), $"seed {seed}: room {i} breaks the margin");
                Assert.InRange(room.Size.X, settings.MinRoomSize.X, settings.MaxRoomSize.X);
                Assert.InRange(room.Size.Y, settings.MinRoomSize.Y, settings.MaxRoomSize.Y);
                for (int j = i + 1; j < rooms.Count; j++)
                    Assert.False(room.Overlaps(rooms[j], settings.Gap), $"seed {seed}: rooms {i} and {j} are closer than the gap");
            }
        }
        // 48×48 with the defaults fits the 12 rooms nearly always.
        Assert.True(total > 200 * 11, $"only {total} rooms over 200 seeds");
    }

    [Fact]
    public void Occupied_cells_are_avoided_by_the_gap()
    {
        var settings = new LayoutSettings();
        var occupied = new OccupancyGrid(settings.Bounds);
        for (int y = 10; y < 38; y++)
            occupied.Take(new Vector2I(24, y)); // a wall of hand-placed cells across the middle
        occupied.Take(new RoomRect(new Vector2I(5, 5), new Vector2I(4, 4)));

        for (long seed = 0; seed < 100; seed++)
            foreach (var room in Place(settings, seed, occupied).Rooms)
                Assert.DoesNotContain(room.Grow(settings.Gap).Cells(), cell => occupied.InBounds(cell) && occupied.IsTaken(cell));
        Assert.Equal(28 + 16, occupied.TakenCount); // placement does not change the grid
    }

    [Fact]
    public void The_same_seed_gives_the_same_rooms_and_other_seeds_differ()
    {
        var settings = new LayoutSettings();
        var first = Place(settings, 1234).Rooms;
        Assert.Equal(first, Place(settings, 1234).Rooms);
        Assert.NotEqual(first, Place(settings, 1235).Rooms);
    }

    [Fact]
    public void A_crowded_setup_reports_the_shortfall()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(24, 24), RoomCount = 20 };
        var layout = Place(settings, 7);
        Assert.InRange(layout.Rooms.Count, 2, 19);
        var warning = Assert.Single(layout.Warnings);
        Assert.StartsWith($"Placed {layout.Rooms.Count} of 20 rooms", warning);

        var roomy = Place(new LayoutSettings { Bounds = new Vector2I(96, 96) }, 7);
        Assert.Equal(12, roomy.Rooms.Count);
        Assert.Empty(roomy.Warnings);
    }

    [Fact]
    public void Rooms_too_big_for_the_bounds_place_nothing()
    {
        var layout = Place(new LayoutSettings { Bounds = new Vector2I(8, 8), MinRoomSize = new Vector2I(9, 9) }, 1);
        Assert.Empty(layout.Rooms);
        Assert.StartsWith("Placed 0 of 12 rooms", Assert.Single(layout.Warnings));
    }

    [Fact]
    public void Fixed_room_sizes_are_used_as_given()
    {
        var settings = new LayoutSettings { MinRoomSize = new Vector2I(6, 4), MaxRoomSize = new Vector2I(6, 4) };
        Assert.All(Place(settings, 3).Rooms, room => Assert.Equal(new Vector2I(6, 4), room.Size));
    }

    [Fact]
    public void Prints_a_layout_for_the_review()
    {
        var settings = new LayoutSettings();
        var occupied = new OccupancyGrid(settings.Bounds);
        occupied.Take(new RoomRect(new Vector2I(22, 22), new Vector2I(4, 4)));
        var layout = Place(settings, 1, occupied);
        string text = LayoutText.Render(layout, settings.Bounds, occupied);
        _output.WriteLine(text);
        Assert.Equal(settings.Bounds.Y, text.Count(c => c == '\n'));
        Assert.Equal(16, text.Count(c => c == 'X'));
        Assert.Equal(layout.Rooms.Sum(r => r.Area), text.Count(c => c == '#' || c == '.'));
    }
}
