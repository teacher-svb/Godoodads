using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

/// <summary>The dungeon graph of godoodungeon 1.1 (brief §12, card dgn-1-1-graph-core).</summary>
public class DungeonGraphTests
{
    /// <summary>A hand-built graph: room r is a 3 × 3 room at x = 10 r; each corridor gets <c>length</c> cells on row z = 20.</summary>
    private static DungeonGraphData Graph(int rooms, params (int A, int B, int Length)[] corridors)
    {
        var list = corridors.Select((c, i) => new GraphCorridor(i, c.A, c.B, -1, -1, true,
            Enumerable.Range(0, c.Length).Select(x => new Vector3I(x, 0, 20 + i)).ToArray())).ToList();
        var roomList = Enumerable.Range(0, rooms).Select(r => new GraphRoom(r, new Vector3I(10 * r, 0, 0), new Vector3I(3, 1, 3),
            Array.Empty<int>(), list.Where(c => c.RoomA == r || c.RoomB == r).Select(c => c.Index).ToArray())).ToList();
        return new DungeonGraphData(Vector3I.Zero, new Vector2I(10 * rooms, 40), roomList, Array.Empty<GraphDoor>(), list);
    }

    // 0 -5- 1 -5- 2 -1- 3 -2- 4, plus loops 0-2 (12) and 0-4 (30).
    private static readonly DungeonGraphData Chain = Graph(5, (0, 1, 5), (1, 2, 5), (0, 2, 12), (2, 3, 1), (3, 4, 2), (0, 4, 30));

    [Fact]
    public void Hop_distances_count_corridors_and_length_distances_add_corridor_lengths()
    {
        Assert.Equal(new[] { 0, 1, 1, 2, 1 }, Chain.HopDistances(0));
        Assert.Equal(new[] { 0, 5, 10, 11, 13 }, Chain.Distances(0));
        Assert.Equal(new[] { 13, 8, 3, 2, 0 }, Chain.Distances(4));
    }

    [Fact]
    public void The_shortest_path_follows_lengths_not_hops()
    {
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, Chain.ShortestPath(0, 4));
        Assert.Equal(new[] { 0, 1, 3, 4 }, Chain.ShortestPathCorridors(0, 4));
        Assert.Equal(new[] { 4, 3, 2, 1, 0 }, Chain.ShortestPath(4, 0));
        Assert.Equal(new[] { 2 }, Chain.ShortestPath(2, 2));
        Assert.Empty(Chain.ShortestPathCorridors(2, 2));
    }

    [Fact]
    public void Farthest_room_and_diameter_use_lengths()
    {
        Assert.Equal(4, Chain.FarthestRoom(0));
        Assert.Equal(0, Chain.FarthestRoom(3));
        Assert.Equal((0, 4), Chain.Diameter());
    }

    [Fact]
    public void Ties_go_to_fewer_corridors_then_lower_indices()
    {
        // A square of equal corridors: 0-1-3 and 0-2-3 tie; the way through room 1 wins.
        var square = Graph(4, (0, 1, 2), (1, 3, 2), (0, 2, 2), (2, 3, 2));
        Assert.Equal(new[] { 0, 1, 3 }, square.ShortestPath(0, 3));
        Assert.Equal(new[] { 0, 1 }, square.ShortestPathCorridors(0, 3));
        Assert.Equal(new[] { 3, 1, 0 }, square.ShortestPath(3, 0));

        // A direct corridor as long as the two-step way wins on hops.
        var direct = Graph(4, (0, 1, 2), (1, 3, 2), (0, 2, 2), (2, 3, 2), (0, 3, 4));
        Assert.Equal(new[] { 0, 3 }, direct.ShortestPath(0, 3));
        Assert.Equal(new[] { 4 }, direct.ShortestPathCorridors(0, 3));

        // Diameter ties: every opposite pair is 4 apart; the lowest pair wins.
        Assert.Equal((0, 3), square.Diameter());
        Assert.Equal(3, square.FarthestRoom(0));
        Assert.Equal(1, square.FarthestRoom(2));
    }

    [Fact]
    public void Unreachable_and_unknown_rooms_give_empty_answers()
    {
        var split = Graph(4, (0, 1, 3), (2, 3, 3));
        Assert.Equal(new[] { 0, 1, -1, -1 }, split.HopDistances(0));
        Assert.Equal(new[] { 0, 3, -1, -1 }, split.Distances(0));
        Assert.Empty(split.ShortestPath(0, 3));
        Assert.Equal(1, split.FarthestRoom(0));

        Assert.Empty(Chain.HopDistances(-1));
        Assert.Empty(Chain.Distances(5));
        Assert.Empty(Chain.ShortestPath(0, 9));
        Assert.Equal(-1, Chain.FarthestRoom(7));
        Assert.Empty(Chain.Neighbours(5));
        Assert.Equal(-1, Chain.CorridorBetween(0, 9));
        Assert.Equal((-1, -1), Graph(1).Diameter());
    }

    [Fact]
    public void Neighbours_and_corridor_between()
    {
        Assert.Equal(new[] { 1, 2, 4 }, Chain.Neighbours(0));
        Assert.Equal(new[] { 1, 0, 3 }, Chain.Neighbours(2));
        Assert.Equal(2, Chain.CorridorBetween(2, 0));
        Assert.Equal(2, Chain.CorridorBetween(0, 2));
        Assert.Equal(-1, Chain.CorridorBetween(1, 3));
        Assert.Equal(0, Chain.Corridors[5].Other(4));
        Assert.Equal(30, Chain.Corridors[5].Length);
    }

    // ---- Generated dungeons ------------------------------------------------------------------

    private static DungeonResult Generate(long seed) => new DungeonJob(new DungeonRequest
    {
        Layout = new LayoutSettings { Bounds = new Vector2I(40, 40), RoomCount = 8 },
        Origin = new Vector3I(3, 1, -2),
        RoomTiles = DungeonTestTiles.Room(),
        DoorTile = DungeonTestTiles.Door,
        CorridorTiles = DungeonTestTiles.Corridor(),
        Seed = seed,
    }).RunNow();

    [Fact]
    public void A_generated_graph_has_every_room_and_one_corridor_per_edge_from_door_to_door()
    {
        for (long seed = 0; seed < 6; seed++)
        {
            var result = Generate(seed);
            Assert.Equal(DungeonStatus.Solved, result.Status);
            var layout = result.Layout;
            var graph = result.Graph;
            Assert.NotNull(graph);
            Assert.Equal(layout.Rooms.Count, graph.Rooms.Count);
            Assert.Equal(layout.Edges.Count, graph.Corridors.Count);
            Assert.Equal(layout.Origin, graph.Origin);

            for (int r = 0; r < graph.Rooms.Count; r++)
            {
                var room = graph.Rooms[r];
                Assert.Equal(layout.GridCell(layout.Rooms[r].Position), room.Position);
                Assert.Equal(new Vector3I(layout.Rooms[r].Size.X, 1, layout.Rooms[r].Size.Y), room.Size);
                Assert.All(room.Doors, d => Assert.Equal(r, graph.Doors[d].Room));
                Assert.Equal(layout.Doors.Count(d => d.Room == r), room.Doors.Count);
                Assert.Equal(layout.Edges.Count(e => e.A == r || e.B == r), room.Corridors.Count);
            }

            int floor = layout.Origin.Y;
            for (int e = 0; e < graph.Corridors.Count; e++)
            {
                var corridor = graph.Corridors[e];
                Assert.Equal(layout.Edges[e].A, corridor.RoomA);
                Assert.Equal(layout.Edges[e].B, corridor.RoomB);
                Assert.Equal(layout.Edges[e].IsTreeEdge, corridor.IsTreeEdge);
                Assert.NotEmpty(corridor.Cells);
                var doorA = graph.Doors[corridor.DoorA];
                var doorB = graph.Doors[corridor.DoorB];
                Assert.Equal(corridor.RoomA, doorA.Room);
                Assert.Equal(corridor.RoomB, doorB.Room);
                Assert.Equal(doorA.Cell + Offset(doorA.Side), corridor.Cells[0]);
                Assert.Equal(doorB.Cell + Offset(doorB.Side), corridor.Cells[^1]);
                for (int i = 1; i < corridor.Cells.Count; i++)
                    Assert.Equal(1, Manhattan(corridor.Cells[i], corridor.Cells[i - 1]));
                foreach (var cell in corridor.Cells)
                {
                    Assert.Equal(floor, cell.Y);
                    var at = new Vector2I(cell.X - layout.Origin.X, cell.Z - layout.Origin.Z);
                    Assert.NotEqual(0, result.CorridorFaces[at.Y * 40 + at.X]);
                    Assert.Equal(-1, graph.RoomAtCell(cell));
                }
            }
        }
    }

    [Fact]
    public void Cell_lookups_match_the_layout_on_every_cell()
    {
        bool sawJunction = false;
        for (long seed = 0; seed < 6; seed++)
        {
            var result = Generate(seed);
            var layout = result.Layout;
            var graph = result.Graph;
            for (int z = -1; z <= 40; z++)
                for (int x = -1; x <= 40; x++)
                {
                    var at = new Vector2I(x, z);
                    int expected = layout.Rooms.FindIndex(room => room.Contains(at));
                    var cell = layout.GridCell(at);
                    Assert.Equal(expected, graph.RoomAtCell(cell));
                    Assert.Equal(expected, graph.RoomAtCell(cell + new Vector3I(0, 3, 0)));

                    var corridors = graph.Corridors.Where(c => c.Cells.Contains(cell)).Select(c => c.Index).ToArray();
                    Assert.Equal(corridors, graph.CorridorsAtCell(cell));
                    sawJunction |= corridors.Length > 1;
                }
        }
        Assert.True(sawJunction, "No corridor cell was shared by two corridors in 6 dungeons.");
    }

    [Fact]
    public void The_same_seed_gives_the_same_graph()
    {
        var first = Generate(21).Graph;
        var second = Generate(21).Graph;
        Assert.Equal(first.Rooms.Select(r => (r.Position, r.Size)), second.Rooms.Select(r => (r.Position, r.Size)));
        Assert.Equal(first.Doors, second.Doors);
        Assert.Equal(first.Corridors.Count, second.Corridors.Count);
        for (int i = 0; i < first.Corridors.Count; i++)
        {
            Assert.Equal(first.Corridors[i].Cells, second.Corridors[i].Cells);
            Assert.Equal((first.Corridors[i].DoorA, first.Corridors[i].DoorB), (second.Corridors[i].DoorA, second.Corridors[i].DoorB));
        }
        Assert.Equal(first.Diameter(), second.Diameter());
    }

    private static Vector3I Offset(DoorSide side)
    {
        var step = side.Offset();
        return new Vector3I(step.X, 0, step.Y);
    }

    private static int Manhattan(Vector3I a, Vector3I b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
}
