using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>A door of the graph: the room's edge cell it sits in, as a GridMap cell on the bottom layer, and the side it opens to.</summary>
public sealed record GraphDoor(int Room, Vector3I Cell, DoorSide Side);

/// <summary>
/// A room of the graph. <see cref="Position"/> is the GridMap cell of its lowest corner and
/// <see cref="Size"/> its extent in cells, walls included (Y is the room height in layers).
/// </summary>
public sealed record GraphRoom(int Index, Vector3I Position, Vector3I Size, IReadOnlyList<int> Doors, IReadOnlyList<int> Corridors)
{
    /// <summary>The cell nearest the room's centre on its bottom layer (rounded down on even sizes).</summary>
    public Vector3I CentreCell => new(Position.X + Size.X / 2, Position.Y, Position.Z + Size.Z / 2);

    /// <summary>True when the cell lies in the room's footprint on X and Z. The layer is ignored: 1.1 has one floor.</summary>
    public bool HoldsColumn(Vector3I cell) =>
        cell.X >= Position.X && cell.X < Position.X + Size.X && cell.Z >= Position.Z && cell.Z < Position.Z + Size.Z;
}

/// <summary>
/// A corridor of the graph: one room graph edge (brief godoodungeon §12.1), with the door each room
/// uses and its route in walking order from room A's door to room B's door, lanes included.
/// </summary>
public sealed record GraphCorridor(int Index, int RoomA, int RoomB, int DoorA, int DoorB, bool IsTreeEdge, IReadOnlyList<Vector3I> Cells)
{
    /// <summary>The number of corridor cells from door to door.</summary>
    public int Length => Cells.Count;

    /// <summary>The room at the other end from <paramref name="room"/>.</summary>
    public int Other(int room) => room == RoomA ? RoomB : RoomA;
}

/// <summary>
/// The dungeon graph (brief godoodungeon §12): rooms are vertices, the corridors routed along the
/// room graph are edges. Pure data plus the queries of §12.3, so gameplay code can be tested without
/// the engine; the <c>DungeonGraph</c> resource wraps it. Cells are GridMap cells.
/// Corridors that meet share cells, but a junction is not an edge: the graph is the designed layout.
/// </summary>
public sealed class DungeonGraphData
{
    private readonly List<(int Corridor, int Room)>[] _adjacent;
    private readonly int[] _roomAt;
    private readonly Dictionary<int, List<int>> _corridorsAt = new();

    /// <param name="origin">The GridMap cell of the bounds origin.</param>
    /// <param name="size">The bounds on X and Z (Y is the GridMap's Z), for the cell lookups.</param>
    public DungeonGraphData(Vector3I origin, Vector2I size, IReadOnlyList<GraphRoom> rooms, IReadOnlyList<GraphDoor> doors, IReadOnlyList<GraphCorridor> corridors)
    {
        Origin = origin;
        Size = size;
        Rooms = rooms;
        Doors = doors;
        Corridors = corridors;

        _adjacent = rooms.Select(_ => new List<(int, int)>()).ToArray();
        foreach (var corridor in corridors)
        {
            _adjacent[corridor.RoomA].Add((corridor.Index, corridor.RoomB));
            _adjacent[corridor.RoomB].Add((corridor.Index, corridor.RoomA));
        }

        _roomAt = new int[Math.Max(size.X, 0) * Math.Max(size.Y, 0)];
        Array.Fill(_roomAt, -1);
        foreach (var room in rooms)
            for (int z = room.Position.Z; z < room.Position.Z + room.Size.Z; z++)
                for (int x = room.Position.X; x < room.Position.X + room.Size.X; x++)
                {
                    int column = Column(new Vector3I(x, 0, z));
                    if (column >= 0)
                        _roomAt[column] = room.Index;
                }
        foreach (var corridor in corridors)
            foreach (var cell in corridor.Cells)
            {
                int column = Column(cell);
                if (column < 0)
                    continue;
                if (!_corridorsAt.TryGetValue(column, out var list))
                    _corridorsAt[column] = list = new List<int>();
                if (!list.Contains(corridor.Index))
                    list.Add(corridor.Index);
            }
    }

    public Vector3I Origin { get; }
    public Vector2I Size { get; }
    public IReadOnlyList<GraphRoom> Rooms { get; }
    public IReadOnlyList<GraphDoor> Doors { get; }
    public IReadOnlyList<GraphCorridor> Corridors { get; }

    /// <summary>
    /// The graph of a solved layout: one room per layout room, one corridor per room graph edge with
    /// its route from <paramref name="plan"/>. Layout cells become GridMap cells: X and Z offset by the
    /// origin, rooms and corridors on the bounds' bottom layer.
    /// </summary>
    public static DungeonGraphData Build(DungeonLayout layout, CorridorPlan plan, int roomHeight)
    {
        var origin = layout.Origin;
        Vector3I Cell(Vector2I at) => layout.GridCell(at);

        var doors = layout.Doors.Select(d => new GraphDoor(d.Room, Cell(d.Cell), d.Side)).ToList();
        var corridors = new List<GraphCorridor>();
        for (int e = 0; e < layout.Edges.Count; e++)
        {
            var edge = layout.Edges[e];
            var (doorA, doorB) = e < layout.EdgeDoors.Count ? layout.EdgeDoors[e] : (-1, -1);
            var route = e < plan.Routes.Count ? plan.Routes[e] : null;
            corridors.Add(new GraphCorridor(e, edge.A, edge.B, doorA, doorB, edge.IsTreeEdge,
                route == null ? Array.Empty<Vector3I>() : route.Select(Cell).ToArray()));
        }

        var rooms = new List<GraphRoom>();
        for (int r = 0; r < layout.Rooms.Count; r++)
        {
            var rect = layout.Rooms[r];
            rooms.Add(new GraphRoom(r, Cell(rect.Position), new Vector3I(rect.Size.X, Math.Max(roomHeight, 1), rect.Size.Y),
                Enumerable.Range(0, doors.Count).Where(d => doors[d].Room == r).ToArray(),
                corridors.Where(c => c.RoomA == r || c.RoomB == r).Select(c => c.Index).ToArray()));
        }
        return new DungeonGraphData(origin, plan.Size, rooms, doors, corridors);
    }

    private int Column(Vector3I cell)
    {
        int x = cell.X - Origin.X, z = cell.Z - Origin.Z;
        return x < 0 || z < 0 || x >= Size.X || z >= Size.Y ? -1 : z * Size.X + x;
    }

    private bool IsRoom(int room) => room >= 0 && room < Rooms.Count;

    // ---- Spatial lookup -------------------------------------------------------------------

    /// <summary>The room whose footprint (walls included) holds the cell on X and Z, or -1.</summary>
    public int RoomAtCell(Vector3I cell)
    {
        int column = Column(cell);
        return column < 0 ? -1 : _roomAt[column];
    }

    /// <summary>The corridors whose route holds the cell on X and Z, in index order; more than one where corridors meet.</summary>
    public int[] CorridorsAtCell(Vector3I cell) =>
        _corridorsAt.TryGetValue(Column(cell), out var list) ? list.OrderBy(c => c).ToArray() : Array.Empty<int>();

    // ---- Neighbours -----------------------------------------------------------------------

    /// <summary>The rooms one corridor away, in corridor order, each once.</summary>
    public int[] Neighbours(int room) =>
        IsRoom(room) ? _adjacent[room].Select(a => a.Room).Distinct().ToArray() : Array.Empty<int>();

    /// <summary>The lowest-index corridor joining the two rooms, or -1.</summary>
    public int CorridorBetween(int a, int b)
    {
        if (!IsRoom(a) || !IsRoom(b))
            return -1;
        foreach (var (corridor, room) in _adjacent[a])
            if (room == b)
                return corridor;
        return -1;
    }

    // ---- Distances and paths ----------------------------------------------------------------

    /// <summary>Per room, the fewest corridors from <paramref name="room"/> (breadth-first); -1 when unreachable. Empty for an unknown room.</summary>
    public int[] HopDistances(int room)
    {
        if (!IsRoom(room))
            return Array.Empty<int>();
        var hops = Enumerable.Repeat(-1, Rooms.Count).ToArray();
        var queue = new Queue<int>();
        hops[room] = 0;
        queue.Enqueue(room);
        while (queue.Count > 0)
        {
            int at = queue.Dequeue();
            foreach (var (_, next) in _adjacent[at])
                if (hops[next] < 0)
                {
                    hops[next] = hops[at] + 1;
                    queue.Enqueue(next);
                }
        }
        return hops;
    }

    /// <summary>Per room, the shortest total corridor length from <paramref name="room"/>; -1 when unreachable. Walking through a room costs nothing.</summary>
    public int[] Distances(int room) => IsRoom(room) ? Search(room).Length : Array.Empty<int>();

    /// <summary>
    /// The rooms of the shortest way from <paramref name="from"/> to <paramref name="to"/> by corridor
    /// length, both ends included. Ties go to fewer corridors, then to the way whose room list, read
    /// from <paramref name="from"/>, is lowest (then the lower corridor index). Empty when either room
    /// is unknown or unreachable.
    /// </summary>
    public int[] ShortestPath(int from, int to) => Path(from, to).Rooms;

    /// <summary>The corridors of <see cref="ShortestPath"/>, in walking order.</summary>
    public int[] ShortestPathCorridors(int from, int to) => Path(from, to).Corridors;

    /// <summary>The room farthest from <paramref name="room"/> by corridor length (the lowest index on ties), or -1.</summary>
    public int FarthestRoom(int room)
    {
        if (!IsRoom(room))
            return -1;
        var length = Search(room).Length;
        int best = -1;
        for (int r = 0; r < length.Length; r++)
            if (length[r] >= 0 && (best < 0 || length[r] > length[best]))
                best = r;
        return best;
    }

    /// <summary>
    /// The two rooms farthest apart by corridor length, lower index first (the lowest pair on ties),
    /// for a start and an end. (-1, -1) with fewer than 2 rooms.
    /// </summary>
    public (int A, int B) Diameter()
    {
        (int A, int B) best = (-1, -1);
        int bestLength = -1;
        for (int a = 0; a < Rooms.Count; a++)
        {
            var length = Search(a).Length;
            for (int b = a + 1; b < Rooms.Count; b++)
                if (length[b] > bestLength)
                {
                    bestLength = length[b];
                    best = (a, b);
                }
        }
        return best;
    }

    /// <summary>
    /// Walks from <paramref name="from"/> towards <paramref name="to"/> over the shortest ways only,
    /// taking the lowest next room (then the lowest corridor) at every step. All shortest ways have
    /// the same number of corridors, so this gives the lowest room list.
    /// </summary>
    private (int[] Rooms, int[] Corridors) Path(int from, int to)
    {
        if (!IsRoom(from) || !IsRoom(to))
            return (Array.Empty<int>(), Array.Empty<int>());
        var back = Search(to);
        if (back.Length[from] < 0)
            return (Array.Empty<int>(), Array.Empty<int>());
        var rooms = new List<int> { from };
        var corridors = new List<int>();
        for (int at = from; at != to;)
        {
            int bestRoom = -1, bestCorridor = -1;
            foreach (var (corridor, next) in _adjacent[at])
            {
                bool onShortestWay = back.Length[next] >= 0
                    && back.Length[next] + Corridors[corridor].Length == back.Length[at]
                    && back.Hops[next] + 1 == back.Hops[at];
                if (onShortestWay && (bestRoom < 0 || next < bestRoom || (next == bestRoom && corridor < bestCorridor)))
                    (bestRoom, bestCorridor) = (next, corridor);
            }
            rooms.Add(bestRoom);
            corridors.Add(bestCorridor);
            at = bestRoom;
        }
        return (rooms.ToArray(), corridors.ToArray());
    }

    /// <summary>
    /// Dijkstra by (length, hops): per room, the shortest corridor length from <paramref name="source"/>
    /// (-1 when unreachable) and the fewest corridors among the ways of that length.
    /// </summary>
    private (int[] Length, int[] Hops) Search(int source)
    {
        int count = Rooms.Count;
        var length = Enumerable.Repeat(-1, count).ToArray();
        var hops = new int[count];
        var done = new bool[count];
        var queue = new PriorityQueue<int, (int Length, int Hops)>();
        length[source] = 0;
        queue.Enqueue(source, (0, 0));
        while (queue.TryDequeue(out int at, out _))
        {
            if (done[at])
                continue;
            done[at] = true;
            foreach (var (corridor, next) in _adjacent[at])
            {
                if (done[next])
                    continue;
                int nextLength = length[at] + Corridors[corridor].Length;
                int nextHops = hops[at] + 1;
                if (length[next] >= 0 && (length[next], hops[next]).CompareTo((nextLength, nextHops)) <= 0)
                    continue;
                length[next] = nextLength;
                hops[next] = nextHops;
                queue.Enqueue(next, (nextLength, nextHops));
            }
        }
        return (length, hops);
    }
}
