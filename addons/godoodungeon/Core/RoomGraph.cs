using System;
using System.Collections.Generic;
using System.Linq;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Connects the rooms (brief godoodungeon §4.3): a minimum spanning tree over the room centres,
/// built with Prim's algorithm on the complete graph, plus <see cref="LayoutSettings.ExtraConnections"/>
/// of the remaining edges, shortest first, which give loops. Equal distances are ordered by a
/// seeded tiebreak, so the result is fixed for a seed.
/// </summary>
public static class RoomGraph
{
    // The graph draws from its own stream of the layout seed; placement uses stream 0.
    private const int GraphStream = 1;

    /// <summary>Fills <see cref="DungeonLayout.Edges"/> from <see cref="DungeonLayout.Rooms"/>. Each edge has A &lt; B; tree edges come first.</summary>
    public static void Build(DungeonLayout layout, double extraConnections, long seed)
    {
        layout.Edges.Clear();
        var rooms = layout.Rooms;
        int count = rooms.Count;
        if (count < 2)
            return;

        // Weight of every pair: squared centre distance, then a random tiebreak.
        var random = new WfcRandom(seed, GraphStream);
        var distance = new double[count, count];
        var tiebreak = new double[count, count];
        for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
            {
                distance[a, b] = distance[b, a] = rooms[a].Centre.DistanceSquaredTo(rooms[b].Centre);
                tiebreak[a, b] = tiebreak[b, a] = random.NextDouble();
            }
        bool Lighter(int a1, int b1, int a2, int b2) =>
            distance[a1, b1] < distance[a2, b2] || (distance[a1, b1] == distance[a2, b2] && tiebreak[a1, b1] < tiebreak[a2, b2]);

        // Prim's: grow the tree from room 0, always taking the lightest edge out of it.
        var inTree = new bool[count];
        var bestFrom = new int[count];
        inTree[0] = true;
        for (int room = 1; room < count; room++)
            bestFrom[room] = 0;
        var isTreeEdge = new bool[count, count];
        for (int added = 1; added < count; added++)
        {
            int next = -1;
            for (int room = 0; room < count; room++)
                if (!inTree[room] && (next < 0 || Lighter(room, bestFrom[room], next, bestFrom[next])))
                    next = room;
            int from = bestFrom[next];
            inTree[next] = true;
            isTreeEdge[from, next] = isTreeEdge[next, from] = true;
            layout.Edges.Add(new RoomEdge(Math.Min(from, next), Math.Max(from, next), true));
            for (int room = 0; room < count; room++)
                if (!inTree[room] && Lighter(room, next, room, bestFrom[room]))
                    bestFrom[room] = next;
        }

        // Extra connections: the shortest share of the edges the tree left out.
        var remaining = new List<(int A, int B)>();
        for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
                if (!isTreeEdge[a, b])
                    remaining.Add((a, b));
        remaining.Sort((x, y) => Lighter(x.A, x.B, y.A, y.B) ? -1 : Lighter(y.A, y.B, x.A, x.B) ? 1 : 0);
        int extra = (int)Math.Round(Math.Clamp(extraConnections, 0, 1) * remaining.Count, MidpointRounding.AwayFromZero);
        layout.Edges.AddRange(remaining.Take(extra).Select(edge => new RoomEdge(edge.A, edge.B, false)));
    }
}
