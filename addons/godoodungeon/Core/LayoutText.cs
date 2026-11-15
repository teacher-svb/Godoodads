using System.Text;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// A top-down text picture of a layout, for tests, debugging and the milestone review. +X runs to
/// the right and +Y (the GridMap's Z) downwards. <c>#</c> wall, <c>.</c> floor, <c>D</c> door,
/// <c>X</c> occupied cell, <c>space</c> free.
/// </summary>
public static class LayoutText
{
    public static string Render(DungeonLayout layout, Vector2I bounds, OccupancyGrid occupied = null)
    {
        var grid = new char[bounds.X * bounds.Y];
        System.Array.Fill(grid, ' ');
        void Set(Vector2I cell, char symbol)
        {
            if (cell.X >= 0 && cell.Y >= 0 && cell.X < bounds.X && cell.Y < bounds.Y)
                grid[cell.Y * bounds.X + cell.X] = symbol;
        }

        if (occupied != null)
            for (int y = 0; y < bounds.Y; y++)
                for (int x = 0; x < bounds.X; x++)
                    if (occupied.InBounds(new Vector2I(x, y)) && occupied.IsTaken(new Vector2I(x, y)))
                        Set(new Vector2I(x, y), 'X');
        foreach (var room in layout.Rooms)
            foreach (var cell in room.Cells())
                Set(cell, room.IsOnEdge(cell) ? '#' : '.');
        foreach (var door in layout.Doors)
            Set(door.Cell, 'D');

        var text = new StringBuilder();
        for (int y = 0; y < bounds.Y; y++)
        {
            text.Append(grid, y * bounds.X, bounds.X);
            text.Append('\n');
        }
        return text.ToString();
    }
}
