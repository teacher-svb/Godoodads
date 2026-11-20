using System.Collections.Generic;
using Godot;

namespace Godoodads.Common.Wfc.Core;

/// <summary>
/// A box of grid cells from <see cref="Origin"/> (inclusive) to Origin + <see cref="Size"/>
/// (exclusive), such as a pin box (WFC brief §16). A box with a side of 0 or less holds no cell.
/// </summary>
public readonly record struct CellBox(Vector3I Origin, Vector3I Size)
{
    public bool Contains(Vector3I cell) =>
        cell.X >= Origin.X && cell.Y >= Origin.Y && cell.Z >= Origin.Z
        && cell.X < Origin.X + Size.X && cell.Y < Origin.Y + Size.Y && cell.Z < Origin.Z + Size.Z;

    /// <summary>True when any box in <paramref name="boxes"/> holds the cell. Null means no boxes.</summary>
    public static bool AnyContains(IReadOnlyList<CellBox> boxes, Vector3I cell)
    {
        if (boxes == null)
            return false;
        foreach (var box in boxes)
            if (box.Contains(cell))
                return true;
        return false;
    }
}
