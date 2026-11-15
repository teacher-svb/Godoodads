using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Puts doors in the room walls for the room graph (brief godoodungeon §4.3). For each edge, a room
/// that already has a door on a side facing the other room uses it; otherwise it gets a door on the
/// side that faces the other room best, at the non-corner wall cell closest to it. A side has at
/// most one door: edges that leave through the same side share it, and the corridors branch outside. A room at <see cref="LayoutSettings.MaxDoorsPerRoom"/> gets no new door; the edge
/// uses the room's door that faces closest to the other room instead. Outside every door, a lane of
/// max(Wall Clearance, 1) cells plus one more cell must be inside the bounds, free of occupied cells
/// and clear of the other rooms and their clearance rings, so the corridor can leave the room.
/// </summary>
public static class DoorPlacer
{
    /// <summary>
    /// Fills <see cref="DungeonLayout.Doors"/> from the rooms and edges. A room that gets no door
    /// adds an error to <see cref="DungeonLayout.Errors"/>.
    /// </summary>
    public static void Place(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied = null)
    {
        layout.Doors.Clear();
        var bySide = new Dictionary<(int Room, DoorSide Side), int>();
        foreach (var edge in layout.Edges)
        {
            DoorFor(layout, settings, occupied, bySide, edge.A, edge.B);
            DoorFor(layout, settings, occupied, bySide, edge.B, edge.A);
        }
        for (int room = 0; room < layout.Rooms.Count; room++)
            if (!layout.Doors.Any(d => d.Room == room))
                layout.Errors.Add($"Room {room + 1} at {layout.GridCell(layout.Rooms[room].Position)} has no wall where a corridor can leave it: "
                    + "every side faces the bounds edge, a hand-placed cell or another room too closely.");
    }

    /// <summary>Cells outside a door that must be free: the lane through the Wall Clearance, then one more.</summary>
    public static int LaneLength(LayoutSettings settings) => Math.Max(settings.WallClearance, 1);

    private static int DoorFor(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied,
        Dictionary<(int, DoorSide), int> bySide, int room, int other)
    {
        var rect = layout.Rooms[room];
        var towards = layout.Rooms[other].Centre - rect.Centre;
        // Sides facing the other room, best aligned first; X wins ties, so the order is fixed.
        var facing = DoorSides.All.Select(side => (Side: side, Score: Score(side, towards)))
            .Where(s => s.Score > 0).OrderByDescending(s => s.Score).Select(s => s.Side).ToList();
        int doors = layout.Doors.Count(d => d.Room == room);

        // A door on any side facing the other room serves this edge too (decided at the M1 review).
        foreach (var side in facing)
            if (bySide.TryGetValue((room, side), out int existing))
                return existing;

        foreach (var side in facing)
        {
            if (doors >= settings.MaxDoorsPerRoom)
                break;
            if (BestCell(layout, settings, occupied, room, side, layout.Rooms[other].Centre) is { } cell)
                return Add(layout, bySide, room, side, cell);
        }

        // At the cap, or no facing side works: reuse the door that faces closest to the other room.
        var own = layout.Doors.Select((door, index) => (door, index)).Where(d => d.door.Room == room).ToList();
        if (own.Count > 0)
            return own.OrderByDescending(d => Score(d.door.Side, towards)).First().index;

        // A room without any door yet tries the remaining sides, so it is never cut off.
        foreach (var side in DoorSides.All.OrderByDescending(side => Score(side, towards)))
            if (BestCell(layout, settings, occupied, room, side, layout.Rooms[other].Centre) is { } cell)
                return Add(layout, bySide, room, side, cell);
        return -1;
    }

    private static int Add(DungeonLayout layout, Dictionary<(int, DoorSide), int> bySide, int room, DoorSide side, Vector2I cell)
    {
        layout.Doors.Add(new Door(room, cell, side));
        bySide[(room, side)] = layout.Doors.Count - 1;
        return layout.Doors.Count - 1;
    }

    private static float Score(DoorSide side, Vector2 towards)
    {
        var offset = side.Offset();
        return offset.X * towards.X + offset.Y * towards.Y;
    }

    /// <summary>The non-corner cell of a side closest to <paramref name="target"/> whose lane is free, or null.</summary>
    private static Vector2I? BestCell(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied, int room, DoorSide side, Vector2 target)
    {
        var rect = layout.Rooms[room];
        bool alongX = side is DoorSide.PosY or DoorSide.NegY;
        int fixedCoord = side switch
        {
            DoorSide.PosX => rect.End.X - 1,
            DoorSide.NegX => rect.Position.X,
            DoorSide.PosY => rect.End.Y - 1,
            _ => rect.Position.Y,
        };
        int from = (alongX ? rect.Position.X : rect.Position.Y) + 1;
        int to = (alongX ? rect.End.X : rect.End.Y) - 2;
        float aim = (alongX ? target.X : target.Y) - 0.5f; // cell centres sit at whole coordinates + 0.5
        var candidates = Enumerable.Range(from, Math.Max(to - from + 1, 0))
            .OrderBy(position => Math.Abs(position - aim)).ThenBy(position => position);
        foreach (int position in candidates)
        {
            var cell = alongX ? new Vector2I(position, fixedCoord) : new Vector2I(fixedCoord, position);
            if (LaneIsFree(layout, settings, occupied, room, cell, side))
                return cell;
        }
        return null;
    }

    private static bool LaneIsFree(DungeonLayout layout, LayoutSettings settings, OccupancyGrid occupied, int room, Vector2I cell, DoorSide side)
    {
        int length = LaneLength(settings) + 1;
        for (int step = 1; step <= length; step++)
        {
            var lane = cell + side.Offset() * step;
            if (lane.X < 0 || lane.Y < 0 || lane.X >= settings.Bounds.X || lane.Y >= settings.Bounds.Y)
                return false;
            if (occupied != null && occupied.IsTaken(lane))
                return false;
            for (int other = 0; other < layout.Rooms.Count; other++)
                if (other != room && layout.Rooms[other].Grow(settings.WallClearance).Contains(lane))
                    return false;
        }
        return true;
    }
}
