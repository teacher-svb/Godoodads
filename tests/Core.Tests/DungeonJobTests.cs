using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonJobTests
{
    private static DungeonRequest Request(long seed, LayoutSettings layout = null) => new()
    {
        Layout = layout ?? new LayoutSettings { Bounds = new Vector2I(40, 40), RoomCount = 8 },
        Origin = new Vector3I(3, 1, -2),
        RoomTiles = DungeonTestTiles.Room(),
        DoorTile = DungeonTestTiles.Door,
        CorridorTiles = DungeonTestTiles.Corridor(),
        Seed = seed,
    };

    [Fact]
    public void The_same_seed_gives_the_same_cells()
    {
        var first = new DungeonJob(Request(11)).RunNow();
        var second = new DungeonJob(Request(11)).RunNow();
        Assert.Equal(DungeonStatus.Solved, first.Status);
        Assert.Equal(first.Layout.Rooms, second.Layout.Rooms);
        Assert.Equal(first.Solves.Count, second.Solves.Count);
        for (int i = 0; i < first.Solves.Count; i++)
            Assert.Equal(first.Solves[i].Cells, second.Solves[i].Cells);

        var other = new DungeonJob(Request(12)).RunNow();
        Assert.NotEqual(first.Layout.Rooms, other.Layout.Rooms);
    }

    [Fact]
    public void A_solved_dungeon_has_one_solve_per_room_then_the_corridors()
    {
        var result = new DungeonJob(Request(5)).RunNow();
        Assert.Equal(DungeonStatus.Solved, result.Status);
        Assert.Equal(result.Layout.Rooms.Count + 1, result.Solves.Count);
        Assert.All(result.Solves.Take(result.Solves.Count - 1), s => Assert.False(s.IsCorridor));
        Assert.True(result.Solves[^1].IsCorridor);
        Assert.Equal(result.Layout.GridCell(result.Layout.Rooms[0].Position), result.Solves[0].Input.Origin);
        Assert.True(result.RouteCount > 0);
    }

    [Fact]
    public void A_room_that_cannot_be_solved_retries_every_layout_attempt_then_fails()
    {
        // Corners with weight 0 are never generated, so no room can close.
        var tiles = DungeonTestTiles.RoomTiles.Select((t, i) => i == DungeonTestTiles.Corner ? t with { Weight = 0 } : t).ToList();
        var request = Request(3) with { RoomTiles = CompiledTileSet.Compile(tiles, Directions.All3D), LayoutAttempts = 3, MaxRestarts = 1 };
        var result = new DungeonJob(request).RunNow();
        Assert.Equal(DungeonStatus.Failed, result.Status);
        Assert.Equal(new[] { 0, 1, 2 }, result.Attempts.Select(a => a.Attempt));
        Assert.All(result.Attempts, a => Assert.StartsWith("Room 1 at ", a.Message));
        Assert.All(result.Attempts, a => Assert.Contains("could not be solved", a.Message));
        Assert.All(result.Attempts, a => Assert.NotNull(a.Cell));
    }

    [Fact]
    public void Corridors_that_cannot_be_routed_move_to_the_next_layout_and_are_reported()
    {
        // A wall of hand-placed cells across the bounds: rooms fit on both sides, corridors never join them.
        var request = Request(2, new LayoutSettings { Bounds = new Vector2I(40, 40), RoomCount = 6 }) with { LayoutAttempts = 3 };
        var wall = new HashSet<Vector3I>();
        for (int z = 0; z < 40; z++)
            wall.Add(request.Origin + new Vector3I(20, 0, z));
        var result = new DungeonJob(request with { HandPlaced = wall }).RunNow();
        Assert.Equal(DungeonStatus.Failed, result.Status);
        Assert.Equal(new[] { 0, 1, 2 }, result.Attempts.Select(a => a.Attempt));
        Assert.All(result.Attempts, a => Assert.StartsWith("Corridors: No corridor could join room ", a.Message));
        Assert.All(result.Attempts, a => Assert.NotNull(a.Cell));
    }

    [Fact]
    public void Cancelling_stops_within_one_solve()
    {
        var big = Request(1, new LayoutSettings { Bounds = new Vector2I(200, 200), RoomCount = 120 }) with { MaxRestarts = 1000 };
        var job = new DungeonJob(big);
        var clock = Stopwatch.StartNew();
        job.Start();
        System.Threading.Thread.Sleep(30);
        job.Cancel();
        Assert.True(job.Wait(5000), "the job did not stop");
        Assert.Null(job.Error);
        Assert.Equal(DungeonStatus.Cancelled, job.Result.Status);
        Assert.True(clock.ElapsedMilliseconds < 5000);
    }

    [Fact]
    public void Invalid_settings_stop_before_any_layout()
    {
        var result = new DungeonJob(Request(1, new LayoutSettings { RoomCount = 1 })).RunNow();
        Assert.Equal(DungeonStatus.Invalid, result.Status);
        Assert.StartsWith("Room Count is 1", Assert.Single(result.Errors));
        Assert.Null(result.Layout);

        var noTiles = new DungeonJob(Request(1) with { CorridorTiles = null }).RunNow();
        Assert.Equal(DungeonStatus.Invalid, noTiles.Status);

        var cornerDoor = new DungeonJob(Request(1) with { DoorTile = DungeonTestTiles.Corner }).RunNow();
        Assert.Equal(DungeonStatus.Invalid, cornerDoor.Status);
        Assert.StartsWith("The Door Tile has no rotation", Assert.Single(cornerDoor.Errors));
    }

    [Fact]
    public void Hand_placed_cells_are_avoided_by_rooms_and_corridors()
    {
        var request = Request(8);
        var handPlaced = new HashSet<Vector3I>();
        for (int x = 15; x < 25; x++)
            for (int z = 15; z < 18; z++)
                handPlaced.Add(request.Origin + new Vector3I(x, 0, z));
        handPlaced.Add(request.Origin + new Vector3I(2, 5, 2)); // high above the dungeon layers: ignored
        var result = new DungeonJob(request with { HandPlaced = handPlaced }).RunNow();
        Assert.Equal(DungeonStatus.Solved, result.Status);

        foreach (var cell in handPlaced.Where(c => c.Y == request.Origin.Y))
        {
            var local = new Vector2I(cell.X - request.Origin.X, cell.Z - request.Origin.Z);
            Assert.DoesNotContain(result.Layout.Rooms, room => room.Contains(local));
            var corridor = result.Solves[^1].Input;
            Assert.Equal(WfcSolver.Excluded, corridor.Initial[corridor.Grid.Index(new Vector3I(local.X, 0, local.Y))]);
        }
    }
}
