using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

/// <summary>
/// The automated acceptance criteria of brief godoodungeon §8 (card dgn-m5-acceptance-pass), at the
/// target size: 64 × 64 cells and 20 rooms, with the M0 prototype's tile rules.
/// </summary>
public class DungeonAcceptanceTests
{
    private const int Seeds = 50;
    private static readonly CompiledTileSet Room = DungeonTestTiles.Room();
    private static readonly CompiledTileSet Corridor = DungeonTestTiles.Corridor();

    private static DungeonRequest Request(long seed, IReadOnlySet<Vector3I> handPlaced = null) => new()
    {
        Layout = new LayoutSettings { Bounds = new Vector2I(64, 64), RoomCount = 20 },
        Origin = new Vector3I(-10, 0, 4),
        RoomTiles = Room,
        DoorTile = DungeonTestTiles.Door,
        CorridorTiles = Corridor,
        Seed = seed,
        HandPlaced = handPlaced ?? new HashSet<Vector3I>(),
    };

    private static DungeonResult Generate(long seed, IReadOnlySet<Vector3I> handPlaced = null)
    {
        var result = new DungeonJob(Request(seed, handPlaced)).RunNow();
        Assert.True(result.Status == DungeonStatus.Solved, $"seed {seed}: {result.Status} {string.Join(" | ", result.Attempts.Select(a => a.Message))}");
        return result;
    }

    [Fact]
    public void Determinism_same_seed_settings_and_hand_placed_cells_give_identical_output()
    {
        var handPlaced = new HashSet<Vector3I>();
        for (int x = 28; x < 36; x++)
            handPlaced.Add(new Vector3I(-10 + x, 0, 4 + 30));
        for (long seed = 0; seed < 10; seed++)
        {
            var a = Generate(seed, handPlaced);
            var b = Generate(seed, handPlaced);
            Assert.Equal(a.Layout.Rooms, b.Layout.Rooms);
            Assert.Equal(a.Layout.Doors, b.Layout.Doors);
            Assert.Equal(a.Solves.Count, b.Solves.Count);
            for (int i = 0; i < a.Solves.Count; i++)
                Assert.Equal(a.Solves[i].Cells, b.Solves[i].Cells);
        }
    }

    [Fact]
    public void Connectivity_every_room_is_reachable_over_50_seeds()
    {
        for (long seed = 0; seed < Seeds; seed++)
        {
            var result = Generate(seed);
            var corridors = result.Solves[^1];
            Assert.Empty(DungeonConnectivity.UnreachableRooms(result.Layout, corridors.Input, corridors.Cells, Corridor, DungeonTestTiles.Path));
        }
    }

    [Fact]
    public void Closed_rooms_outer_faces_are_walls_or_doors_and_every_door_leads_to_a_corridor()
    {
        for (long seed = 0; seed < Seeds; seed++)
        {
            var result = Generate(seed);
            foreach (var solve in result.Solves.Where(s => !s.IsCorridor))
                for (int cell = 0; cell < solve.Input.Grid.CellCount; cell++)
                {
                    var sockets = Room.Variants[solve.Cells[cell]].Sockets;
                    foreach (int direction in DungeonTileRules.HorizontalDirections)
                    {
                        bool outward = solve.Input.Grid.Neighbour(cell, direction) < 0;
                        bool edge = sockets[direction].Name == DungeonTestTiles.Edge && sockets[direction].Kind == SocketKind.HorizontalPlus;
                        Assert.True(outward == edge, $"seed {seed}: room cell {solve.Input.GridMapCell(cell)} faces {Directions.Name(direction)} with '{sockets[direction]}'");
                    }
                }

            var corridor = result.Solves[^1];
            foreach (var door in result.Layout.Doors)
            {
                int outside = corridor.Input.Grid.Index(new Vector3I(door.Outside.X, 0, door.Outside.Y));
                int towardsDoor = Directions.Opposite(SolveInputs.Direction(door.Side));
                Assert.True((DungeonTileRules.PathFaces(Corridor.Variants[corridor.Cells[outside]], DungeonTestTiles.Path) & (1 << towardsDoor)) != 0,
                    $"seed {seed}: the door at {result.Layout.GridCell(door.Cell)} does not lead into a corridor");
            }
        }
    }

    [Fact]
    public void Layout_rules_no_overlap_gap_kept_doors_off_corners_and_away_from_the_bounds_edge()
    {
        var settings = Request(0).Layout;
        for (long seed = 0; seed < Seeds; seed++)
        {
            var layout = Generate(seed).Layout;
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                Assert.True(layout.Rooms[i].IsInside(settings.Bounds, settings.Margin));
                for (int j = i + 1; j < layout.Rooms.Count; j++)
                    Assert.False(layout.Rooms[i].Overlaps(layout.Rooms[j], settings.Gap), $"seed {seed}: rooms {i} and {j}");
            }
            foreach (var door in layout.Doors)
            {
                var room = layout.Rooms[door.Room];
                Assert.True(room.IsOnEdge(door.Cell) && !room.IsCorner(door.Cell), $"seed {seed}: door {door}");
                var entry = DoorPlacer.EntryCell(door, settings);
                Assert.True(entry.X >= 0 && entry.Y >= 0 && entry.X < settings.Bounds.X && entry.Y < settings.Bounds.Y, $"seed {seed}: door {door} faces the bounds edge");
            }
        }
    }
}

/// <summary>Brief godoodungeon §8 performance: 64 × 64 with 20 rooms in under 10 s. Skip with --filter "Category!=Performance".</summary>
[Trait("Category", "Performance")]
public class DungeonPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public DungeonPerformanceTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(64, 20)]
    [InlineData(128, 60)]
    public void Timings_per_stage(int size, int rooms)
    {
        var times = new List<StageTimes>();
        var clock = Stopwatch.StartNew();
        int solved = 0;
        const int Runs = 10;
        for (long seed = 1; seed <= Runs; seed++)
        {
            var result = new DungeonJob(new DungeonRequest
            {
                Layout = new LayoutSettings { Bounds = new Vector2I(size, size), RoomCount = rooms, MaxRoomSize = new Vector2I(11, 11) },
                RoomTiles = DungeonTestTiles.Room(), DoorTile = DungeonTestTiles.Door, CorridorTiles = DungeonTestTiles.Corridor(), Seed = seed,
            }).RunNow();
            if (result.Status == DungeonStatus.Solved)
            {
                solved++;
                times.Add(result.Times);
            }
        }
        double total = clock.Elapsed.TotalMilliseconds / Runs;
        _output.WriteLine($"{size}×{size}, {rooms} rooms: {solved} of {Runs} solved, {total:F0} ms per dungeon on average; per stage: "
            + $"layout {times.Average(t => t.Layout):F1} ms, rooms {times.Average(t => t.Rooms):F1} ms, routing {times.Average(t => t.Routing):F1} ms, "
            + $"corridors {times.Average(t => t.Corridors):F1} ms, check {times.Average(t => t.Check):F1} ms; slowest {times.Max(t => t.Total):F0} ms.");
        Assert.True(solved >= Runs - 1);
        if (size == 64)
            Assert.True(total < 10_000);
    }
}
