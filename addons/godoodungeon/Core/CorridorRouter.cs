using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// The corridors of a layout: per layout cell, a bit mask of its path faces in WFC directions
/// (<see cref="Directions.PosX"/>, NegX, PosZ, NegZ; layout Y is GridMap Z). <see cref="Error"/> is
/// set when an edge could not be routed.
/// </summary>
public sealed record CorridorPlan(Vector2I Size, int[] Faces, int RouteCount, int RouteCells, string Error = null, Vector2I? ErrorCell = null)
{
    public bool IsValid => Error == null;

    /// <summary>
    /// Per room graph edge (same index as <see cref="DungeonLayout.Edges"/>), the layout cells of its
    /// corridor in walking order: from the cell outside room A's door, through both lanes, to the cell
    /// outside room B's door. Null for an edge that was not routed. Cells where corridors meet appear
    /// in the route of each.
    /// </summary>
    public IReadOnlyList<Vector2I[]> Routes { get; init; } = Array.Empty<Vector2I[]>();
}

/// <summary>
/// Routes the corridors of a dungeon along its room graph (card dgn-m3-edge-router). Rooms are nodes
/// of the path network: each graph edge gets one corridor, from the lane outside one room's door to
/// the lane outside the neighbour's door, so no door is routed to whichever corridor is nearest.
/// Corridors that meet share cells and become T or cross pieces. A corridor avoids running right
/// next to another one it does not join, so parallel corridors are rare.
/// </summary>
public static class CorridorRouter
{
    private const double SharedCost = 0.3;
    private const double BesideCost = 4;
    private const double TurnCost = 0.2;

    private static readonly int[] Steps = { Directions.PosX, Directions.NegX, Directions.PosZ, Directions.NegZ };

    private static Vector2I Offset(int direction) => direction switch
    {
        Directions.PosX => new Vector2I(1, 0),
        Directions.NegX => new Vector2I(-1, 0),
        Directions.PosZ => new Vector2I(0, 1),
        _ => new Vector2I(0, -1),
    };

    /// <param name="occupied">Hand-placed cells on the layout grid; null means none.</param>
    /// <param name="wander">0 to 1: seeded noise on the cell costs, so corridors meander more.</param>
    public static CorridorPlan Route(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied, double wander, long seed)
    {
        var size = settings.Bounds;
        int count = size.X * size.Y;
        var faces = new int[count];
        var blocked = new bool[count];
        bool Inside(Vector2I c) => c.X >= 0 && c.Y >= 0 && c.X < size.X && c.Y < size.Y;
        int Index(Vector2I c) => c.Y * size.X + c.X;

        foreach (var room in layout.Rooms)
            foreach (var cell in room.Grow(settings.WallClearance).Cells())
                if (Inside(cell))
                    blocked[Index(cell)] = true;
        if (occupied != null)
            for (int i = 0; i < count; i++)
                if (occupied.IsTaken(new Vector2I(i % size.X, i / size.X)))
                    blocked[i] = true;

        // Lanes: straight pieces from the door out through the clearance. They stay blocked for
        // other routes; a route starts and ends at the entry cell past a lane.
        int lane = DoorPlacer.LaneLength(settings);
        foreach (var door in layout.Doors)
        {
            int axis = SolveInputs.Direction(door.Side);
            for (int step = 0; step < lane; step++)
            {
                var cell = door.Outside + door.Side.Offset() * step;
                if (!Inside(cell))
                    continue;
                blocked[Index(cell)] = true;
                faces[Index(cell)] |= (1 << axis) | (1 << Directions.Opposite(axis));
            }
        }

        var noise = new double[count];
        var random = new WfcRandom(seed, 0);
        for (int i = 0; i < count; i++)
            noise[i] = random.NextDouble() * Math.Clamp(wander, 0, 1) * 2;

        int routes = 0;
        var routeCells = new Vector2I[layout.Edges.Count][];
        for (int e = 0; e < layout.Edges.Count; e++)
        {
            var (doorA, doorB) = layout.EdgeDoors[e];
            if (doorA < 0 || doorB < 0 || doorA == doorB)
                continue;
            var a = layout.Doors[doorA];
            var b = layout.Doors[doorB];
            var from = DoorPlacer.EntryCell(a, settings);
            var to = DoorPlacer.EntryCell(b, settings);
            if (!Inside(from) || !Inside(to) || blocked[Index(from)] || blocked[Index(to)])
                return Failed(size, faces, routes, a, b, layout);

            var path = Search(from, to, size, blocked, faces, noise);
            if (path == null)
                return Failed(size, faces, routes, a, b, layout);

            Connect(faces, Index(from), Index(from - a.Side.Offset()), SolveInputs.Direction(a.Side) ^ 1);
            Connect(faces, Index(to), Index(to - b.Side.Offset()), SolveInputs.Direction(b.Side) ^ 1);
            for (int i = 1; i < path.Count; i++)
            {
                var step = path[i] - path[i - 1];
                int direction = Steps.First(d => Offset(d) == step);
                Connect(faces, Index(path[i - 1]), Index(path[i]), direction);
            }
            routeCells[e] = Lane(a, lane).Concat(path).Concat(Lane(b, lane).Reverse()).ToArray();
            routes++;
        }
        return new CorridorPlan(size, faces, routes, faces.Count(f => f != 0)) { Routes = routeCells };
    }

    /// <summary>The lane cells outside a door, from the door outwards, ending next to its entry cell.</summary>
    private static IEnumerable<Vector2I> Lane(Door door, int length) =>
        Enumerable.Range(0, length).Select(step => door.Outside + door.Side.Offset() * step);

    /// <summary>Adds path faces between two neighbouring cells; <paramref name="direction"/> points from the first to the second.</summary>
    private static void Connect(int[] faces, int from, int to, int direction)
    {
        faces[from] |= 1 << direction;
        faces[to] |= 1 << Directions.Opposite(direction);
    }

    private static CorridorPlan Failed(Vector2I size, int[] faces, int routes, Door a, Door b, DungeonLayout layout) =>
        new(size, faces, routes, faces.Count(f => f != 0),
            $"No corridor could join room {a.Room + 1}'s door at {layout.GridCell(a.Cell)} to room {b.Room + 1}'s door at {layout.GridCell(b.Cell)}: "
            + "rooms, their clearance and hand-placed cells close every way.", a.Cell);

    /// <summary>A* over free cells, with the direction of travel in the state so turns can cost extra. Null when no way exists.</summary>
    private static List<Vector2I> Search(Vector2I from, Vector2I to, Vector2I size, bool[] blocked, int[] faces, double[] noise)
    {
        int Index(Vector2I c) => c.Y * size.X + c.X;
        int count = size.X * size.Y;
        var cost = new Dictionary<int, double>();
        var previous = new Dictionary<int, int>();
        var queue = new PriorityQueue<int, (double, long)>();
        long order = 0;
        double Estimate(Vector2I c) => SharedCost * (Math.Abs(c.X - to.X) + Math.Abs(c.Y - to.Y));
        int State(int cell, int direction) => cell * 4 + direction;

        // The first move may go any way; direction slot 0..3 stands for the last step.
        for (int d = 0; d < 4; d++)
        {
            int state = State(Index(from), d);
            cost[state] = 0;
            queue.Enqueue(state, (Estimate(from), order++));
        }
        while (queue.TryDequeue(out int state, out var priority))
        {
            int cell = state / 4;
            int last = state % 4;
            double here = cost[state];
            if (priority.Item1 > here + Estimate(new Vector2I(cell % size.X, cell / size.X)) + 1e-9)
                continue;
            var at = new Vector2I(cell % size.X, cell / size.X);
            if (at == to)
            {
                var path = new List<Vector2I> { at };
                while (previous.TryGetValue(state, out int back))
                {
                    state = back;
                    path.Add(new Vector2I(state / 4 % size.X, state / 4 / size.X));
                }
                path.Reverse();
                return path;
            }
            for (int d = 0; d < 4; d++)
            {
                var next = at + Offset(Steps[d]);
                if (next.X < 0 || next.Y < 0 || next.X >= size.X || next.Y >= size.Y)
                    continue;
                int n = Index(next);
                if (blocked[n])
                    continue;
                double step = faces[n] != 0 ? SharedCost : 1 + noise[n] + (BesideCorridor(next, at, size, faces) ? BesideCost : 0);
                if (d != last && cell != Index(from))
                    step += TurnCost;
                int nextState = State(n, d);
                double total = here + step;
                if (cost.TryGetValue(nextState, out double known) && known <= total)
                    continue;
                cost[nextState] = total;
                previous[nextState] = state;
                queue.Enqueue(nextState, (total + Estimate(next), order++));
            }
        }
        return null;
    }

    /// <summary>True when a free cell lies next to a corridor cell other than the one the route comes from.</summary>
    private static bool BesideCorridor(Vector2I cell, Vector2I comingFrom, Vector2I size, int[] faces)
    {
        foreach (int direction in Steps)
        {
            var side = cell + Offset(direction);
            if (side == comingFrom || side.X < 0 || side.Y < 0 || side.X >= size.X || side.Y >= size.Y)
                continue;
            if (faces[side.Y * size.X + side.X] != 0)
                return true;
        }
        return false;
    }
}
