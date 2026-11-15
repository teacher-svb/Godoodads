using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// A room's footprint on the layout grid, walls included. The layout grid is the dungeon bounds
/// seen from above: X is the GridMap's X and Y is the GridMap's Z.
/// </summary>
public readonly record struct RoomRect(Vector2I Position, Vector2I Size)
{
    /// <summary>The first cell past the room on each axis.</summary>
    public Vector2I End => Position + Size;

    public Vector2 Centre => new(Position.X + Size.X * 0.5f, Position.Y + Size.Y * 0.5f);

    public int Area => Size.X * Size.Y;

    public bool Contains(Vector2I cell) =>
        cell.X >= Position.X && cell.X < End.X && cell.Y >= Position.Y && cell.Y < End.Y;

    /// <summary>The rectangle grown by <paramref name="cells"/> on every side.</summary>
    public RoomRect Grow(int cells) => new(Position - new Vector2I(cells, cells), Size + new Vector2I(2 * cells, 2 * cells));

    public bool Overlaps(RoomRect other) =>
        Position.X < other.End.X && other.Position.X < End.X && Position.Y < other.End.Y && other.Position.Y < End.Y;

    /// <summary>True when fewer than <paramref name="gap"/> free cells separate the two rooms on either axis.</summary>
    public bool Overlaps(RoomRect other, int gap) => Grow(gap).Overlaps(other);

    public bool IsOnEdge(Vector2I cell) =>
        Contains(cell) && (cell.X == Position.X || cell.X == End.X - 1 || cell.Y == Position.Y || cell.Y == End.Y - 1);

    public bool IsCorner(Vector2I cell) =>
        Contains(cell) && (cell.X == Position.X || cell.X == End.X - 1) && (cell.Y == Position.Y || cell.Y == End.Y - 1);

    /// <summary>True when the room lies inside a grid of <paramref name="bounds"/>, at least <paramref name="margin"/> cells from its edges.</summary>
    public bool IsInside(Vector2I bounds, int margin) =>
        Position.X >= margin && Position.Y >= margin && End.X <= bounds.X - margin && End.Y <= bounds.Y - margin;

    public IEnumerable<Vector2I> Cells()
    {
        for (int y = Position.Y; y < End.Y; y++)
            for (int x = Position.X; x < End.X; x++)
                yield return new Vector2I(x, y);
    }

    /// <summary>The cells of the ring <paramref name="width"/> cells wide around the room, such as its Wall Clearance.</summary>
    public IEnumerable<Vector2I> Ring(int width)
    {
        foreach (var cell in Grow(Math.Max(width, 0)).Cells())
            if (!Contains(cell))
                yield return cell;
    }
}
