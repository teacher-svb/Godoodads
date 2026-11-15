using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonLayoutModelTests
{
    private static RoomRect Rect(int x, int y, int w, int h) => new(new Vector2I(x, y), new Vector2I(w, h));

    [Fact]
    public void Rooms_side_by_side_overlap_only_when_a_gap_is_required()
    {
        var a = Rect(0, 0, 5, 5);
        var touching = Rect(5, 0, 5, 5);
        Assert.False(a.Overlaps(touching));
        Assert.False(a.Overlaps(touching, 0));
        Assert.True(a.Overlaps(touching, 1));
    }

    [Theory]
    [InlineData(3, 3, false)] // exactly three free cells between them
    [InlineData(3, 4, true)]
    [InlineData(2, 3, true)]
    [InlineData(0, 0, false)]
    public void The_gap_counts_free_cells_between_rooms(int freeCells, int gap, bool overlaps)
    {
        var a = Rect(0, 0, 5, 5);
        var b = Rect(5 + freeCells, 2, 4, 4);
        Assert.Equal(overlaps, a.Overlaps(b, gap));
        Assert.Equal(overlaps, b.Overlaps(a, gap));
    }

    [Fact]
    public void Diagonal_neighbours_respect_the_gap_on_both_axes()
    {
        var a = Rect(0, 0, 5, 5);
        Assert.False(a.Overlaps(Rect(8, 8, 3, 3), 3));
        Assert.False(a.Overlaps(Rect(7, 8, 3, 3), 3)); // two free cells on X, but three on Y: far enough apart
        Assert.True(a.Overlaps(Rect(7, 7, 3, 3), 3)); // two free cells on both axes
        Assert.False(a.Overlaps(Rect(8, 0, 3, 3), 3));
    }

    [Fact]
    public void Edges_corners_and_rings()
    {
        var room = Rect(2, 3, 5, 4);
        Assert.Equal(new Vector2I(7, 7), room.End);
        Assert.Equal(20, room.Area);
        Assert.True(room.IsCorner(new Vector2I(2, 3)));
        Assert.True(room.IsCorner(new Vector2I(6, 6)));
        Assert.False(room.IsCorner(new Vector2I(4, 3)));
        Assert.True(room.IsOnEdge(new Vector2I(4, 3)));
        Assert.False(room.IsOnEdge(new Vector2I(4, 5)));
        Assert.False(room.IsOnEdge(new Vector2I(7, 3)));
        Assert.Equal(14, room.Cells().Count(room.IsOnEdge));

        var ring = room.Ring(1).ToList();
        Assert.Equal(7 * 6 - 20, ring.Count);
        Assert.DoesNotContain(ring, room.Contains);
        Assert.Contains(new Vector2I(1, 2), ring);
        Assert.Equal(9 * 8 - 20, room.Ring(2).Count());
        Assert.Empty(room.Ring(0));
    }

    [Fact]
    public void Rooms_inside_the_bounds_keep_the_margin()
    {
        var bounds = new Vector2I(20, 10);
        Assert.True(Rect(1, 1, 18, 8).IsInside(bounds, 1));
        Assert.False(Rect(0, 1, 5, 5).IsInside(bounds, 1));
        Assert.False(Rect(15, 1, 5, 5).IsInside(bounds, 1));
        Assert.True(Rect(0, 0, 20, 10).IsInside(bounds, 0));
    }

    [Fact]
    public void Occupancy_marks_rooms_and_cells_and_treats_outside_as_taken()
    {
        var grid = new OccupancyGrid(new Vector2I(10, 8));
        Assert.True(grid.IsFree(Rect(0, 0, 10, 8)));

        grid.Take(Rect(2, 2, 3, 2));
        grid.Take(new Vector2I(9, 7));
        Assert.Equal(7, grid.TakenCount);
        Assert.True(grid.IsTaken(new Vector2I(4, 3)));
        Assert.False(grid.IsTaken(new Vector2I(5, 3)));
        Assert.False(grid.IsFree(Rect(4, 0, 3, 3)));
        Assert.True(grid.IsFree(Rect(5, 0, 4, 4)));

        Assert.True(grid.IsTaken(new Vector2I(-1, 0)));
        Assert.True(grid.IsTaken(new Vector2I(10, 0)));
        Assert.False(grid.IsFree(Rect(8, 0, 3, 3)));

        grid.Take(Rect(-2, -2, 3, 3)); // clipped to the grid
        Assert.Equal(8, grid.TakenCount);
    }

    [Fact]
    public void Derived_seeds_are_stable_and_distinct()
    {
        Assert.Equal(WfcRandom.Derive(42, 7), WfcRandom.Derive(42, 7));
        Assert.NotEqual(WfcRandom.Derive(42, 7), WfcRandom.Derive(42, 8));
        Assert.NotEqual(WfcRandom.Derive(42, 7), WfcRandom.Derive(43, 7));

        var seeds = new HashSet<long>();
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Assert.True(seeds.Add(LayoutSeeds.Layout(1, attempt)));
            Assert.True(seeds.Add(LayoutSeeds.Corridors(1, attempt)));
            for (int room = 0; room < 200; room++)
                Assert.True(seeds.Add(LayoutSeeds.Room(1, attempt, room)));
        }
        // Neighbouring Generate seeds give unrelated streams, not shifted copies.
        Assert.NotEqual(LayoutSeeds.Room(1, 0, 1), LayoutSeeds.Room(2, 0, 0));
        Assert.DoesNotContain(LayoutSeeds.Layout(2, 0), seeds);
    }

    [Fact]
    public void Default_settings_keep_a_corridor_between_clearance_rings()
    {
        var settings = new LayoutSettings();
        Assert.Equal(3, settings.MinimumGap);
        Assert.True(settings.Gap >= settings.MinimumGap);
        Assert.Equal(5, (settings with { WallClearance = 2 }).MinimumGap);
        Assert.Equal(2, (settings with { WallClearance = 0 }).MinimumGap);
    }
}
