using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonRoomGraphTests
{
    private static DungeonLayout Layout(long seed, LayoutSettings settings = null)
    {
        settings ??= new LayoutSettings();
        var layout = new DungeonLayout();
        RoomPlacer.Place(layout, settings, seed);
        RoomGraph.Build(layout, settings.ExtraConnections, seed);
        return layout;
    }

    private static bool Connected(int rooms, IEnumerable<RoomEdge> edges)
    {
        var parent = Enumerable.Range(0, rooms).ToArray();
        int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
        foreach (var edge in edges)
            parent[Find(edge.A)] = Find(edge.B);
        return Enumerable.Range(0, rooms).Select(Find).Distinct().Count() == 1;
    }

    private static double Length(DungeonLayout layout, RoomEdge edge) =>
        layout.Rooms[edge.A].Centre.DistanceSquaredTo(layout.Rooms[edge.B].Centre);

    [Fact]
    public void The_graph_is_connected_with_a_spanning_tree_first_over_200_seeds()
    {
        for (long seed = 0; seed < 200; seed++)
        {
            var layout = Layout(seed);
            int rooms = layout.Rooms.Count;
            var tree = layout.Edges.Where(e => e.IsTreeEdge).ToList();
            Assert.Equal(rooms - 1, tree.Count);
            Assert.True(Connected(rooms, tree), $"seed {seed}: the tree is not connected");
            Assert.True(layout.Edges.TakeWhile(e => e.IsTreeEdge).Count() == tree.Count, $"seed {seed}: tree edges are not first");
            Assert.All(layout.Edges, e => Assert.True(e.A < e.B));
            Assert.Equal(layout.Edges.Count, layout.Edges.Select(e => (e.A, e.B)).Distinct().Count());
        }
    }

    [Fact]
    public void The_tree_is_a_minimum_spanning_tree()
    {
        for (long seed = 0; seed < 50; seed++)
        {
            var layout = Layout(seed);
            double prim = layout.Edges.Where(e => e.IsTreeEdge).Sum(e => Length(layout, e));

            // Kruskal over every pair as a reference.
            int count = layout.Rooms.Count;
            var pairs = new List<RoomEdge>();
            for (int a = 0; a < count; a++)
                for (int b = a + 1; b < count; b++)
                    pairs.Add(new RoomEdge(a, b, false));
            var parent = Enumerable.Range(0, count).ToArray();
            int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
            double kruskal = 0;
            foreach (var edge in pairs.OrderBy(e => Length(layout, e)))
                if (Find(edge.A) != Find(edge.B))
                {
                    parent[Find(edge.A)] = Find(edge.B);
                    kruskal += Length(layout, edge);
                }
            Assert.Equal(kruskal, prim, 6);
        }
    }

    [Fact]
    public void No_extra_connections_gives_a_tree_and_all_gives_every_pair()
    {
        var tree = Layout(5, new LayoutSettings { ExtraConnections = 0 });
        int rooms = tree.Rooms.Count;
        Assert.Equal(rooms - 1, tree.Edges.Count);

        var full = Layout(5, new LayoutSettings { ExtraConnections = 1 });
        Assert.Equal(rooms * (rooms - 1) / 2, full.Edges.Count);
        Assert.Equal(rooms - 1, full.Edges.Count(e => e.IsTreeEdge));
    }

    [Fact]
    public void Extra_connections_are_the_shortest_left_out_edges_and_round_to_the_share()
    {
        var layout = Layout(11, new LayoutSettings { ExtraConnections = 0.25 });
        int rooms = layout.Rooms.Count;
        int remaining = rooms * (rooms - 1) / 2 - (rooms - 1);
        var extras = layout.Edges.Where(e => !e.IsTreeEdge).ToList();
        Assert.Equal((int)Math.Round(0.25 * remaining, MidpointRounding.AwayFromZero), extras.Count);

        var used = layout.Edges.Select(e => (e.A, e.B)).ToHashSet();
        double longestExtra = extras.Max(e => Length(layout, e));
        for (int a = 0; a < rooms; a++)
            for (int b = a + 1; b < rooms; b++)
                if (!used.Contains((a, b)))
                    Assert.True(Length(layout, new RoomEdge(a, b, false)) >= longestExtra);
    }

    [Fact]
    public void The_graph_is_fixed_for_a_seed()
    {
        Assert.Equal(Layout(42).Edges, Layout(42).Edges);
    }

    [Fact]
    public void Equal_distances_are_ordered_by_the_seed()
    {
        // Four rooms on the corners of a square: every side is equally long, so the tree
        // is three of the four sides, and which side is left out depends on the seed.
        var square = new[] { new Vector2I(0, 0), new Vector2I(20, 0), new Vector2I(0, 20), new Vector2I(20, 20) };
        var leftOut = new HashSet<(int, int)>();
        for (long seed = 0; seed < 40; seed++)
        {
            var layout = new DungeonLayout();
            layout.Rooms.AddRange(square.Select(p => new RoomRect(p, new Vector2I(5, 5))));
            RoomGraph.Build(layout, 0, seed);
            Assert.Equal(3, layout.Edges.Count);
            Assert.DoesNotContain(layout.Edges, e => (e.A, e.B) is (0, 3) or (1, 2)); // never a diagonal
            var sides = new[] { (0, 1), (0, 2), (1, 3), (2, 3) };
            leftOut.Add(sides.Single(s => !layout.Edges.Any(e => (e.A, e.B) == s)));

            var again = new DungeonLayout();
            again.Rooms.AddRange(layout.Rooms);
            RoomGraph.Build(again, 0, seed);
            Assert.Equal(layout.Edges, again.Edges);
        }
        Assert.True(leftOut.Count > 1, "every seed left out the same side");
    }

    [Fact]
    public void Fewer_than_two_rooms_have_no_edges()
    {
        var layout = new DungeonLayout();
        RoomGraph.Build(layout, 1, 1);
        Assert.Empty(layout.Edges);
        layout.Rooms.Add(new RoomRect(new Vector2I(1, 1), new Vector2I(5, 5)));
        RoomGraph.Build(layout, 1, 1);
        Assert.Empty(layout.Edges);
    }
}
