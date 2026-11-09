using System;
using Godot;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// A box of cells and the directions that connect them. A neighbour outside the box does not
/// exist, so edges are unconstrained.
/// </summary>
public sealed class GridTopology
{
    private readonly int[] _neighbours;

    public Vector3I Size { get; }
    public int CellCount { get; }
    public int[] ActiveDirections { get; }

    public GridTopology(Vector3I size, int[] activeDirections)
    {
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Every grid dimension must be positive.");
        long cells = (long)size.X * size.Y * size.Z;
        if (cells > int.MaxValue / Directions.Count)
            throw new ArgumentOutOfRangeException(nameof(size), "The grid has too many cells.");

        Size = size;
        CellCount = (int)cells;
        ActiveDirections = activeDirections;

        _neighbours = new int[CellCount * Directions.Count];
        Array.Fill(_neighbours, -1);
        for (int cell = 0; cell < CellCount; cell++)
        {
            var position = Coordinates(cell);
            foreach (int direction in activeDirections)
            {
                var next = position + Directions.Offset(direction);
                if (Contains(next))
                    _neighbours[cell * Directions.Count + direction] = Index(next);
            }
        }
    }

    public static GridTopology Create3D(Vector3I size) => new(size, Directions.All3D);

    /// <summary>A flat grid on the X and Z axes.</summary>
    public static GridTopology Create2D(int width, int depth) => new(new Vector3I(width, 1, depth), Directions.All2D);

    public bool Contains(Vector3I p) =>
        p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < Size.X && p.Y < Size.Y && p.Z < Size.Z;

    public int Index(Vector3I p) => p.X + p.Z * Size.X + p.Y * Size.X * Size.Z;

    public Vector3I Coordinates(int cell)
    {
        int layer = Size.X * Size.Z;
        int rest = cell % layer;
        return new Vector3I(rest % Size.X, cell / layer, rest / Size.X);
    }

    /// <summary>The neighbouring cell, or -1 outside the grid or for an inactive direction.</summary>
    public int Neighbour(int cell, int direction) => _neighbours[cell * Directions.Count + direction];
}
