using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// A room box as the layout sees it (brief godoodungeon §13): <see cref="Rect"/> on the layout grid
/// (walls included), <see cref="Layer"/> its bottom layer relative to the bounds' bottom layer,
/// <see cref="Height"/> in layers. <see cref="DoorCells"/> are the layout cells of a Built box's
/// bottom layer that hold the Door item; only those on the wall ring, not in a corner, are doors.
/// <see cref="Name"/> is the node's name, for messages and the graph.
/// </summary>
public sealed record RoomBoxSpec(string Name, RoomRect Rect, int Layer, int Height, RoomBoxMode Mode, IReadOnlyList<Vector2I> DoorCells = null)
{
    public bool IsBuilt => Mode == RoomBoxMode.Built;
}

/// <summary>The checks and door sides of room boxes (brief godoodungeon §13.2).</summary>
public static class RoomBoxes
{
    /// <summary>How messages name a box: "Room box `BossRoom` (room 2)".</summary>
    public static string Label(RoomBoxSpec box, int index) => $"Room box `{box.Name}` (room {index + 1})";

    /// <summary>
    /// The doors of a Built box: its Door cells on the wall ring, not in a corner, in the order
    /// given, each facing out of the side it is on.
    /// </summary>
    public static List<(Vector2I Cell, DoorSide Side)> BuiltDoors(RoomBoxSpec box)
    {
        var doors = new List<(Vector2I, DoorSide)>();
        if (box.DoorCells == null)
            return doors;
        var rect = box.Rect;
        foreach (var cell in box.DoorCells.Distinct())
        {
            if (!rect.IsOnEdge(cell) || rect.IsCorner(cell))
                continue;
            var side = cell.X == rect.Position.X ? DoorSide.NegX
                : cell.X == rect.End.X - 1 ? DoorSide.PosX
                : cell.Y == rect.Position.Y ? DoorSide.NegY
                : DoorSide.PosY;
            doors.Add((cell, side));
        }
        return doors;
    }

    /// <summary>
    /// The errors of §13.2 that do not depend on the generated rooms: a box outside the bounds or
    /// within the Margin, not on the bounds' bottom layer or taller than the bounds, smaller than
    /// 3 × 3, closer than the Gap to another box, a Generated box over hand-placed cells
    /// (<paramref name="occupied"/>), and a Built box without a door.
    /// </summary>
    public static List<string> Validate(LayoutSettings settings, IReadOnlyList<RoomBoxSpec> boxes, OccupancyGrid occupied = null)
    {
        var errors = new List<string>();
        if (boxes == null)
            return errors;
        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            var rect = box.Rect;
            string label = Label(box, i);
            if (rect.Size.X < LayoutValidator.MinimumRoomSide || rect.Size.Y < LayoutValidator.MinimumRoomSide)
                errors.Add($"{label} is {rect.Size.X} × {rect.Size.Y} cells on X and Z. A room needs at least {LayoutValidator.MinimumRoomSide} × {LayoutValidator.MinimumRoomSide}, walls included.");
            if (!rect.IsInside(settings.Bounds, settings.Margin))
                errors.Add($"{label} is outside the bounds or within the Margin of {settings.Margin} cell(s). Move it inside, or enlarge the bounds.");
            if (box.Layer != 0)
                errors.Add($"{label} starts {box.Layer} layer(s) {(box.Layer > 0 ? "above" : "below")} the bottom of the bounds. Rooms stand on the bottom layer: set its Origin Y to the bounds' Origin Y.");
            if (box.Height < 1 || (settings.BoundsHeight > 0 && box.Layer + box.Height > settings.BoundsHeight))
                errors.Add($"{label} is {box.Height} layer(s) high, but the bounds have {settings.BoundsHeight}.");
            for (int j = 0; j < i; j++)
                if (rect.Overlaps(boxes[j].Rect, settings.Gap))
                    errors.Add($"{label} is closer than the Gap of {settings.Gap} cells to {Label(boxes[j], j)}. Move them apart.");
            if (!box.IsBuilt && occupied != null && rect.Cells().Any(c => occupied.InBounds(c) && occupied.IsTaken(c)))
                errors.Add($"{label} is Generated, but covers cells you placed. Make it Built to keep them, or clear them.");
            if (box.IsBuilt && BuiltDoors(box).Count == 0)
                errors.Add($"{label} is Built but has no door: place the Door tile on its wall ring, not in a corner, on its bottom layer.");
        }
        return errors;
    }
}
