using System.Collections.Generic;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>The side of a room a door faces out of, on the layout grid (Y is the GridMap's Z).</summary>
public enum DoorSide
{
    PosX,
    NegX,
    PosY,
    NegY,
}

public static class DoorSides
{
    public static readonly DoorSide[] All = { DoorSide.PosX, DoorSide.NegX, DoorSide.PosY, DoorSide.NegY };

    /// <summary>One step out of the room through this side.</summary>
    public static Vector2I Offset(this DoorSide side) => side switch
    {
        DoorSide.PosX => new Vector2I(1, 0),
        DoorSide.NegX => new Vector2I(-1, 0),
        DoorSide.PosY => new Vector2I(0, 1),
        _ => new Vector2I(0, -1),
    };
}

/// <summary>A door: an edge cell of a room and the side it opens to. The corridor starts at <see cref="Outside"/>.</summary>
public sealed record Door(int Room, Vector2I Cell, DoorSide Side)
{
    public Vector2I Outside => Cell + Side.Offset();
}

/// <summary>A connection in the room graph. Tree edges come from the minimum spanning tree; the others are extra connections (loops).</summary>
public sealed record RoomEdge(int A, int B, bool IsTreeEdge);

/// <summary>
/// The result of the layout stage: rooms, the room graph and the doors. Warnings (such as a room
/// shortfall) still give a usable layout; errors (such as a room without a door) do not.
/// </summary>
public sealed class DungeonLayout
{
    public List<RoomRect> Rooms { get; } = new();
    public List<RoomEdge> Edges { get; } = new();
    public List<Door> Doors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();

    public bool IsValid => Errors.Count == 0;

    /// <summary>The GridMap cell of the layout grid's (0, 0): the bounds origin. Layout Y is GridMap Z.</summary>
    public Vector3I Origin { get; init; }

    /// <summary>The GridMap cell of a layout cell, on the bottom layer of the bounds, for messages.</summary>
    public Vector3I GridCell(Vector2I cell) => new(Origin.X + cell.X, Origin.Y, Origin.Z + cell.Y);
}
