using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Checks a solved dungeon (brief godoodungeon §4.5, §8): a walk from the first room, through room
/// interiors, doors and corridor path faces, must reach every room. Each room counts as one open
/// space; a door leads to the corridor cell outside it when that cell has a path face towards the
/// door, and corridor cells lead to each other where both have path faces towards each other
/// (vertical ones too, so climbing corridors count). A failure is a bug, not a user error.
/// </summary>
public static class DungeonConnectivity
{
    /// <summary>The indices of the rooms the walk from room 0 does not reach. Empty when the dungeon is connected.</summary>
    public static List<int> UnreachableRooms(DungeonLayout layout, SolveInput corridors, int[] corridorCells, CompiledTileSet corridorTiles, string pathSocket)
    {
        string path = pathSocket.Trim();
        var grid = corridors.Grid;
        int PathFaces(int cell)
        {
            int variant = corridorCells[cell];
            if (variant < 0)
                return 0;
            int mask = 0;
            var sockets = corridorTiles.Variants[variant].Sockets;
            foreach (int direction in corridorTiles.ActiveDirections)
                if (sockets[direction].HasName(path))
                    mask |= 1 << direction;
            return mask;
        }

        // Nodes: rooms are 0..rooms-1, corridor cells follow.
        int rooms = layout.Rooms.Count;
        var doorsAt = new Dictionary<int, List<int>>(); // corridor cell -> rooms whose door it serves
        var cellsOfRoom = Enumerable.Range(0, rooms).Select(_ => new List<int>()).ToList();
        foreach (var door in layout.Doors)
        {
            var outside = door.Outside;
            if (outside.X < 0 || outside.Y < 0 || outside.X >= grid.Size.X || outside.Y >= grid.Size.Z)
                continue;
            int cell = grid.Index(new Vector3I(outside.X, 0, outside.Y));
            int towardsDoor = Directions.Opposite(SolveInputs.Direction(door.Side));
            if ((PathFaces(cell) & (1 << towardsDoor)) == 0)
                continue;
            cellsOfRoom[door.Room].Add(cell);
            if (!doorsAt.TryGetValue(cell, out var list))
                doorsAt[cell] = list = new List<int>();
            list.Add(door.Room);
        }

        var reachedRoom = new bool[rooms];
        var reachedCell = new bool[grid.CellCount];
        var queue = new Queue<int>();
        void VisitRoom(int room)
        {
            if (reachedRoom[room])
                return;
            reachedRoom[room] = true;
            foreach (int cell in cellsOfRoom[room])
                VisitCell(cell);
        }
        void VisitCell(int cell)
        {
            if (reachedCell[cell])
                return;
            reachedCell[cell] = true;
            queue.Enqueue(cell);
        }

        if (rooms > 0)
            VisitRoom(0);
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            if (doorsAt.TryGetValue(cell, out var served))
                foreach (int room in served)
                    VisitRoom(room);
            int faces = PathFaces(cell);
            foreach (int direction in corridorTiles.ActiveDirections)
            {
                if ((faces & (1 << direction)) == 0)
                    continue;
                int next = grid.Neighbour(cell, direction);
                if (next >= 0 && (PathFaces(next) & (1 << Directions.Opposite(direction))) != 0)
                    VisitCell(next);
            }
        }
        return Enumerable.Range(0, rooms).Where(room => !reachedRoom[room]).ToList();
    }

    /// <summary>The message for unreachable rooms, naming each by number and GridMap cell.</summary>
    public static string Describe(DungeonLayout layout, IReadOnlyList<int> unreachable) =>
        $"Internal error: the dungeon was generated, but {unreachable.Count} room(s) cannot be reached from room 1: "
        + string.Join(", ", unreachable.Select(r => $"room {r + 1} at {layout.GridCell(layout.Rooms[r].Position)}"))
        + ". Nothing was written. Please report this with the seed and settings.";
}
