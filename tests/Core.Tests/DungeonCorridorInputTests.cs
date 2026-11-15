using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonCorridorInputTests
{
    private static readonly CompiledTileSet Tiles = DungeonTestTiles.Corridor();

    private static readonly LayoutSettings Settings = new() { Bounds = new Vector2I(40, 40), RoomCount = 8 };

    private sealed record Run(DungeonLayout Layout, SolveInput Input, SolveJob Job, LayoutSettings Settings);

    /// <summary>
    /// Lays out and solves the corridors, trying up to 4 layout attempts like the dungeon run
    /// (brief §4.5): about 1 in 50 layouts has a door whose corridor cannot be routed.
    /// </summary>
    private static Run Solve(long seed, LayoutSettings settings = null, Func<Vector3I, bool> handPlaced = null, OccupancyGrid occupied = null)
    {
        settings ??= Settings;
        string last = "";
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var layout = LayoutGenerator.Generate(settings, seed, attempt, new Vector3I(-5, 2, 9), occupied);
            Assert.True(layout.IsValid, string.Join("; ", layout.Errors));
            var input = SolveInputs.Corridors(layout, settings.Bounds, 1, settings.WallClearance, Tiles, DungeonTestTiles.Path, 0.3f, handPlaced);
            Assert.True(input.IsValid, input.Error);
            var job = new SolveJob(input.Grid, Tiles, input.Initial, new SolveOptions { Seed = LayoutSeeds.Corridors(seed, attempt) }, paths: input.Paths);
            job.Start();
            job.Wait(-1);
            Assert.Null(job.Error);
            if (!job.Plan.IsValid)
            {
                last = string.Join("; ", job.Plan.Errors.Select(e => e.Message));
                continue;
            }
            Assert.Equal(SolveStatus.Solved, job.Result.Status);
            return new Run(layout, input, job, settings);
        }
        Assert.Fail($"seed {seed}: no layout attempt could be routed. Last: {last}");
        return null;
    }

    private static Vector2I Cell2(SolveInput input, int cell) { var c = input.Grid.Coordinates(cell); return new Vector2I(c.X, c.Z); }

    private static int PathFaces(int variant) => DungeonTileRules.PathFaces(Tiles.Variants[variant], DungeonTestTiles.Path);

    private static HashSet<Vector2I> LaneCells(Run run)
    {
        var lanes = new HashSet<Vector2I>();
        int length = Math.Max(run.Settings.WallClearance, 1);
        foreach (var door in run.Layout.Doors)
            for (int step = 0; step < length; step++)
                lanes.Add(door.Outside + door.Side.Offset() * step);
        return lanes;
    }

    [Fact]
    public void Guided_paths_joins_every_door_and_no_path_tile_is_left_dangling()
    {
        for (long seed = 0; seed < 25; seed++)
        {
            var run = Solve(seed);
            var input = run.Input;
            var cells = run.Job.Result.Cells;
            var doorCells = run.Layout.Doors.Select(d => d.Cell).ToHashSet();

            // Every path face meets a path face, or (only for the cell outside a door) the door.
            var pathCells = new List<int>();
            for (int cell = 0; cell < input.Grid.CellCount; cell++)
            {
                if (input.Initial[cell] == WfcSolver.Excluded)
                    continue;
                int faces = PathFaces(cells[cell]);
                if (faces == 0)
                    continue;
                pathCells.Add(cell);
                foreach (int direction in DungeonTileRules.HorizontalDirections)
                {
                    if ((faces & (1 << direction)) == 0)
                        continue;
                    int next = input.Grid.Neighbour(cell, direction);
                    Assert.True(next >= 0, $"seed {seed}: a path face leaves the bounds at {input.GridMapCell(cell)}");
                    if (input.Initial[next] == WfcSolver.Excluded)
                        Assert.Contains(Cell2(input, next), doorCells);
                    else
                        Assert.True((PathFaces(cells[next]) & (1 << Directions.Opposite(direction))) != 0,
                            $"seed {seed}: the path face of {input.GridMapCell(cell)} towards {Directions.Name(direction)} meets no path");
                }
            }

            // One network: a walk over path faces from the first door's lane reaches every lane.
            var start = input.Grid.Index(new Vector3I(run.Layout.Doors[0].Outside.X, 0, run.Layout.Doors[0].Outside.Y));
            var seen = new HashSet<int> { start };
            var queue = new Queue<int>(seen);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                foreach (int direction in DungeonTileRules.HorizontalDirections)
                {
                    int next = input.Grid.Neighbour(cell, direction);
                    if ((PathFaces(cells[cell]) & (1 << direction)) != 0 && next >= 0 && input.Initial[next] != WfcSolver.Excluded && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            foreach (var door in run.Layout.Doors)
                Assert.Contains(input.Grid.Index(new Vector3I(door.Outside.X, 0, door.Outside.Y)), seen);
            Assert.Equal(pathCells.Count, seen.Count); // no separate stretch of corridor anywhere
        }
    }

    [Fact]
    public void Lanes_and_the_clearance_ring_are_fixed_and_rooms_excluded()
    {
        var run = Solve(3);
        var input = run.Input;
        int empty = DungeonTileRules.EmptyVariant(Tiles, DungeonTestTiles.Path);
        var lanes = LaneCells(run);
        foreach (var room in run.Layout.Rooms)
        {
            foreach (var cell in room.Cells())
                Assert.Equal(WfcSolver.Excluded, input.Initial[input.Grid.Index(new Vector3I(cell.X, 0, cell.Y))]);
            foreach (var cell in room.Ring(1))
                if (!lanes.Contains(cell))
                    Assert.Equal(empty, input.Initial[input.Grid.Index(new Vector3I(cell.X, 0, cell.Y))]);
        }
        foreach (var door in run.Layout.Doors)
        {
            int lane = input.Initial[input.Grid.Index(new Vector3I(door.Outside.X, 0, door.Outside.Y))];
            int axis = door.Side is DoorSide.PosX or DoorSide.NegX
                ? (1 << Directions.PosX) | (1 << Directions.NegX)
                : (1 << Directions.PosZ) | (1 << Directions.NegZ);
            Assert.Equal(axis, PathFaces(lane));
        }
        Assert.Equal(new Vector3I(-5, 2, 9), input.Origin);
        Assert.Equal(new Vector3I(40, 1, 40), input.Grid.Size);
        Assert.Equal(DungeonTestTiles.Path, input.Paths.Socket);
    }

    [Fact]
    public void With_clearance_1_no_corridor_touches_a_room_wall()
    {
        for (long seed = 0; seed < 25; seed++)
        {
            var run = Solve(seed);
            var lanes = LaneCells(run);
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                if (run.Input.Initial[cell] == WfcSolver.Excluded || PathFaces(run.Job.Result.Cells[cell]) == 0)
                    continue;
                var at = Cell2(run.Input, cell);
                if (lanes.Contains(at))
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
        for (long seed = 0; seed < 25; seed++)
        {
            var run = Solve(seed, settings);
            var lanes = LaneCells(run);
            for (int cell = 0; cell < run.Input.Grid.CellCount; cell++)
            {
                var at = Cell2(run.Input, cell);
                if (run.Input.Initial[cell] != WfcSolver.Excluded && PathFaces(run.Job.Result.Cells[cell]) != 0 && !lanes.Contains(at)
                    && run.Layout.Rooms.Any(room => room.Grow(1).Contains(at)))
                    alongWalls++;
            }
        }
        Assert.True(alongWalls > 0, "no corridor ran along a room wall in 25 seeds");
    }

    [Fact]
    public void Hand_placed_cells_are_excluded_and_avoided()
    {
        var occupied = new OccupancyGrid(Settings.Bounds);
        var block = new RoomRect(new Vector2I(18, 18), new Vector2I(4, 4));
        occupied.Take(block);
        var origin = new Vector3I(-5, 2, 9);
        bool HandPlaced(Vector3I gridMapCell) => block.Contains(new Vector2I(gridMapCell.X - origin.X, gridMapCell.Z - origin.Z));
        var run = Solve(4, handPlaced: HandPlaced, occupied: occupied);
        foreach (var cell in block.Cells())
            Assert.Equal(WfcSolver.Excluded, run.Input.Initial[run.Input.Grid.Index(new Vector3I(cell.X, 0, cell.Y))]);
    }

    [Fact]
    public void Doors_facing_each_other_across_a_narrow_gap_are_offset_and_routed()
    {
        // Clearance 0, gap 2: lanes side by side would close into a pair with no open face, so the
        // second door moves one cell along its wall and both corridors are routed.
        var settings = new LayoutSettings { Bounds = new Vector2I(24, 12), WallClearance = 0, Gap = 2 };
        var layout = new DungeonLayout();
        layout.Rooms.Add(new RoomRect(new Vector2I(1, 2), new Vector2I(7, 7)));
        layout.Rooms.Add(new RoomRect(new Vector2I(10, 2), new Vector2I(7, 7)));
        RoomGraph.Build(layout, 0, 1);
        DoorPlacer.Place(layout, settings);
        Assert.True(layout.IsValid);
        var outside = layout.Doors.Select(d => d.Outside).OrderBy(c => c.X).ToList();
        Assert.Equal(new Vector2I(8, 5), outside[0]);
        Assert.Equal(9, outside[1].X);
        Assert.Equal(1, System.Math.Abs(outside[1].Y - 5));

        var input = SolveInputs.Corridors(layout, settings.Bounds, 1, 0, Tiles, DungeonTestTiles.Path, 0.3f);
        var job = new SolveJob(input.Grid, Tiles, input.Initial, new SolveOptions { Seed = 1 }, paths: input.Paths);
        job.Start();
        job.Wait(-1);
        Assert.True(job.Plan.IsValid, string.Join("; ", job.Plan.Errors.Select(e => e.Message)));
        Assert.True(job.Plan.RouteCount >= 1);
        Assert.Equal(SolveStatus.Solved, job.Result.Status);
    }

    [Fact]
    public void A_corridor_tile_set_without_straights_is_an_error()
    {
        var noStraights = CompiledTileSet.Compile(new[] { DungeonTestTiles.CorridorTiles[DungeonTestTiles.End], DungeonTestTiles.CorridorTiles[DungeonTestTiles.Empty] }, Directions.All3D);
        var layout = LayoutGenerator.Generate(Settings, 1);
        var input = SolveInputs.Corridors(layout, Settings.Bounds, 1, 1, noStraights, DungeonTestTiles.Path, 0.3f);
        Assert.False(input.IsValid);
    }
}
