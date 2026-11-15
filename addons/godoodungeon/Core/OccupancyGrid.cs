using System;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Which cells of the layout grid are taken: by rooms, or by hand-placed cells the dungeon must
/// avoid (brief godoodungeon §4.7). Cells outside the grid count as taken.
/// </summary>
public sealed class OccupancyGrid
{
    private readonly bool[] _taken;

    public Vector2I Size { get; }

    public OccupancyGrid(Vector2I size)
    {
        if (size.X <= 0 || size.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "The layout grid needs at least one cell on each axis.");
        Size = size;
        _taken = new bool[size.X * size.Y];
    }

    public bool InBounds(Vector2I cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Size.X && cell.Y < Size.Y;

    public bool IsTaken(Vector2I cell) => !InBounds(cell) || _taken[cell.Y * Size.X + cell.X];

    public void Take(Vector2I cell)
    {
        if (InBounds(cell))
            _taken[cell.Y * Size.X + cell.X] = true;
    }

    /// <summary>Takes every cell of <paramref name="rect"/> inside the grid.</summary>
    public void Take(RoomRect rect)
    {
        foreach (var cell in rect.Cells())
            Take(cell);
    }

    /// <summary>True when every cell of <paramref name="rect"/> is inside the grid and free.</summary>
    public bool IsFree(RoomRect rect)
    {
        foreach (var cell in rect.Cells())
            if (IsTaken(cell))
                return false;
        return true;
    }

    public int TakenCount
    {
        get
        {
            int count = 0;
            foreach (bool taken in _taken)
                if (taken)
                    count++;
            return count;
        }
    }
}
