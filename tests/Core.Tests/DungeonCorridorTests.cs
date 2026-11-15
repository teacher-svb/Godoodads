using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

/// <summary>Corridors routed room to room along the room graph (card dgn-m3-edge-router), then solved.</summary>
public class DungeonCorridorTests
{
    private readonly ITestOutputHelper _output;

    public DungeonCorridorTests(ITestOutputHelper output) => _output = output;

    private static readonly CompiledTileSet Tiles = DungeonTestTiles.Corridor();

    private static readonly LayoutSettings Settings = new() { Bounds = new Vector2I(40, 40), RoomCount = 8 };

    private sealed record Run(DungeonLayout Layout, CorridorPlan Plan, SolveInput Input, int[] Cells, LayoutSettings Settings);

    private static Run Solve(long seed, LayoutSettings settings = null, OccupancyGrid occupied = null, Func<Vector3I, bool> handPlaced = null)
    {
        settings ??= Settings;
        string last = "";
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var layout = LayoutGenerator.Generate(settings, seed, attempt, new Vector3I(-5, 2, 9), occupied);
            Assert.True(layout.IsValid, string.Join("; ", layout.Errors));
            var plan = CorridorRouter.Route(layout, settings, occupied, 0.3, LayoutSeeds.Corridors(seed, attempt));
            if (!plan.IsValid)
            {
                last = plan.Error;
                continue;
            }
            var input = SolveInputs.Corridors(layout, plan, Tiles, DungeonTestTiles.Path, handPlaced);
            Assert.True(input.IsValid, input.Error);
            var result = new WfcSolver(input.Grid, Tiles, input.Initial).Solve(new SolveOptions { Seed = seed, Restrictions = input.Restrictions });
            Assert.Equal(SolveStatus.Solved, result.Status);
            return new Run(layout, plan, input, result.Cells, settings);
        }
        Assert.Fail($"seed {seed}: no layout attempt could be routed. Last: {last}");
        return null;
    }

    private static int Faces(Run run, int cell) => run.Cells[cell] < 0 ? 0 : DungeonTileRules.PathFaces(Tiles.Variants[run.Cells[cell]], DungeonTestTiles.Path);

    private static int CellAt(Run run, Vector2I at) => run.Input.Grid.Index(new Vector3I(at.X, 0, at.Y));

    private static HashSet<int> Reach(Run run, int start)
    {
        var seen = new HashSet<int> { start };
        var queue = new Queue<int>(seen);
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            foreach (int direction in DungeonTileRules.HorizontalDirections)
            {
                int next = run.Input.Grid.Neighbour(cell, direction);
                if ((Faces(run, cell) & (1 << direction)) != 0 && next >= 0 && run.Input.Initial[next] != WfcSolver.Excluded
                    && (Faces(run, next) & (1 << Directions.Opposite(direction))) != 0 && seen.Add(next))
                    queue.Enqueue(next);
            }
        }
        return seen;
    }

    [Fact]
    public void Every_edge_joins_its_two_doors_and_nothing_dangles()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var run = Solve(seed);
            var doorCells = run.Layout.Doors.Select(d => d.Cell).ToHashSet();
            int pathCells = 0;
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                int faces = Faces(run, cell);
                var at = run.Input.Grid.Coordinates(cell);
                Assert.Equal(run.Plan.Faces[at.Z * run.Plan.Size.X + at.X], run.Input.Initial[cell] == WfcSolver.Excluded ? run.Plan.Faces[at.Z * run.Plan.Size.X + at.X] : faces);
                if (faces == 0)
                    continue;
                pathCells++;
                foreach (int direction in DungeonTileRules.HorizontalDirections)
                {
                    if ((faces & (1 << direction)) == 0)
                        continue;
                    int next = run.Input.Grid.Neighbour(cell, direction);
                    Assert.True(next >= 0, $"seed {seed}: a path face leaves the bounds");
                    if (run.Input.Initial[next] == WfcSolver.Excluded)
                        Assert.Contains(new Vector2I(run.Input.Grid.Coordinates(next).X, run.Input.Grid.Coordinates(next).Z), doorCells);
                    else
                        Assert.True((Faces(run, next) & (1 << Directions.Opposite(direction))) != 0, $"seed {seed}: a path face at {at} meets no path");
                }
            }

            for (int e = 0; e < run.Layout.Edges.Count; e++)
            {
                var (a, b) = run.Layout.EdgeDoors[e];
                var reached = Reach(run, CellAt(run, run.Layout.Doors[a].Outside));
                Assert.Contains(CellAt(run, run.Layout.Doors[b].Outside), reached);
            }
            // Rooms join the corridors into one network; every corridor cell leads to a door (no stray pieces).
            Assert.Empty(DungeonConnectivity.UnreachableRooms(run.Layout, run.Input, run.Cells, Tiles, DungeonTestTiles.Path));
            var fromDoors = run.Layout.Doors.SelectMany(d => Reach(run, CellAt(run, d.Outside))).ToHashSet();
            Assert.Equal(pathCells, fromDoors.Count);
            Assert.Equal(run.Layout.Edges.Count, run.Plan.RouteCount);
        }
    }

    [Fact]
    public void With_clearance_1_no_corridor_enters_a_rooms_ring_except_its_lanes()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var run = Solve(seed);
            var lanes = run.Layout.Doors.Select(d => d.Outside).ToHashSet();
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                var at3 = run.Input.Grid.Coordinates(cell);
                var at = new Vector2I(at3.X, at3.Z);
                if (Faces(run, cell) == 0 || run.Input.Initial[cell] == WfcSolver.Excluded || lanes.Contains(at))
                    continue;
                Assert.DoesNotContain(run.Layout.Rooms, room => room.Grow(1).Contains(at));
            }
        }
    }

    [Fact]
    public void With_clearance_0_corridors_may_run_along_walls()
    {
        var settings = Settings with { WallClearance = 0, Gap = 2 };
        int alongWalls = 0;
        for (long seed = 0; seed < 30; seed++)
        {
            var run = Solve(seed, settings);
            var lanes = run.Layout.Doors.Select(d => d.Outside).ToHashSet();
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                var at3 = run.Input.Grid.Coordinates(cell);
                var at = new Vector2I(at3.X, at3.Z);
                if (Faces(run, cell) != 0 && run.Input.Initial[cell] != WfcSolver.Excluded && !lanes.Contains(at) && run.Layout.Rooms.Any(r => r.Grow(1).Contains(at)))
                    alongWalls++;
            }
        }
        Assert.True(alongWalls > 0);
    }

    [Fact]
    public void Corridors_seldom_run_side_by_side_without_joining()
    {
        int sideBySide = 0, corridorCells = 0;
        for (long seed = 0; seed < 50; seed++)
        {
            var run = Solve(seed);
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                if (Faces(run, cell) == 0)
                    continue;
                corridorCells++;
                foreach (int direction in new[] { Directions.PosX, Directions.PosZ })
                {
                    int next = run.Input.Grid.Neighbour(cell, direction);
                    if (next >= 0 && Faces(run, next) != 0 && (Faces(run, cell) & (1 << direction)) == 0)
                        sideBySide++;
                }
            }
        }
        _output.WriteLine($"{sideBySide} side-by-side pairs over {corridorCells} corridor cells in 50 dungeons.");
        Assert.True(sideBySide * 100 < corridorCells, $"{sideBySide} side-by-side pairs over {corridorCells} corridor cells");
    }

    [Fact]
    public void Routes_are_fixed_for_a_seed_and_wander_changes_them()
    {
        var layout = LayoutGenerator.Generate(Settings, 7);
        var first = CorridorRouter.Route(layout, Settings, null, 0.3, 99);
        Assert.Equal(first.Faces, CorridorRouter.Route(layout, Settings, null, 0.3, 99).Faces);
        Assert.NotEqual(first.Faces, CorridorRouter.Route(layout, Settings, null, 1, 99).Faces);
    }

    [Fact]
    public void Hand_placed_cells_are_avoided_and_excluded()
    {
        var occupied = new OccupancyGrid(Settings.Bounds);
        var block = new RoomRect(new Vector2I(18, 18), new Vector2I(4, 4));
        occupied.Take(block);
        var origin = new Vector3I(-5, 2, 9);
        bool HandPlaced(Vector3I c) => block.Contains(new Vector2I(c.X - origin.X, c.Z - origin.Z));
        var run = Solve(4, occupied: occupied, handPlaced: HandPlaced);
        foreach (var cell in block.Cells())
        {
            Assert.Equal(0, run.Plan.Faces[cell.Y * run.Plan.Size.X + cell.X]);
            Assert.Equal(WfcSolver.Excluded, run.Input.Initial[CellAt(run, cell)]);
        }
    }

    [Fact]
    public void A_wall_of_hand_placed_cells_that_cuts_the_bounds_is_reported()
    {
        var settings = new LayoutSettings { Bounds = new Vector2I(40, 20), RoomCount = 2, MinRoomSize = new Vector2I(5, 5), MaxRoomSize = new Vector2I(5, 5) };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(3, 7), new Vector2I(5, 5)));
        layout.Rooms.Add(new RoomRect(new Vector2I(30, 7), new Vector2I(5, 5)));
        RoomGraph.Build(layout, 0, 1);
        DoorPlacer.Place(layout, settings);
        var occupied = new OccupancyGrid(settings.Bounds);
        for (int y = 0; y < 20; y++)
            occupied.Take(new Vector2I(20, y));
        var plan = CorridorRouter.Route(layout, settings, occupied, 0.3, 1);
        Assert.False(plan.IsValid);
        Assert.StartsWith("No corridor could join room 1's door at ", plan.Error);
    }

    [Fact]
    public void A_corridor_tile_set_without_the_needed_piece_is_an_error()
    {
        var noCross = CompiledTileSet.Compile(DungeonTestTiles.CorridorTiles.Where((_, i) => i != DungeonTestTiles.Cross).ToList(), Directions.All3D);
        var layout = new DungeonLayout();
        int all = DungeonTileRules.HorizontalDirections.Aggregate(0, (m, d) => m | 1 << d);
        var faces = new int[9];
        faces[4] = all;
        var plan = new CorridorPlan(new Vector2I(3, 3), faces, 1, 1);
        var input = SolveInputs.Corridors(layout, plan, noCross, DungeonTestTiles.Path);
        Assert.Equal("The corridor tile set has no piece with 'path' faces on +X and -X and +Z and -Z, which the corridors need.", input.Error);
    }

    [Fact]
    public void Prints_corridors_for_the_review()
    {
        var run = Solve(1, new LayoutSettings());
        _output.WriteLine(LayoutText.Render(run.Layout, run.Settings.Bounds, null, run.Plan.Faces));
    }
}
