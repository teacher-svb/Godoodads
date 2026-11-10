using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// Finds triangles that lie on a boundary face of their grid cell (brief §10.1). Such a triangle
/// is hidden when the neighbouring cell on that side is occupied.
/// </summary>
public static class GridFaces
{
    /// <summary>Share of the cell size within which a vertex counts as on a boundary plane.</summary>
    public const float Tolerance = 1e-3f;

    /// <summary>Unit offsets of the six faces, in the order +X, -X, +Y, -Y, +Z, -Z.</summary>
    public static readonly Vector3I[] Offsets =
    {
        new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1),
    };

    /// <summary>
    /// The face (index into <see cref="Offsets"/>) of the cell from <paramref name="cellMin"/> to
    /// <paramref name="cellMin"/> + <paramref name="cellSize"/> that all three points lie on, or -1.
    /// Points are in the grid's space.
    /// </summary>
    public static int BoundaryFace(Vector3 a, Vector3 b, Vector3 c, Vector3 cellMin, Vector3 cellSize)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            float tolerance = Tolerance * cellSize[axis];
            float low = cellMin[axis], high = cellMin[axis] + cellSize[axis];
            if (On(a[axis], high, tolerance) && On(b[axis], high, tolerance) && On(c[axis], high, tolerance))
                return axis * 2;
            if (On(a[axis], low, tolerance) && On(b[axis], low, tolerance) && On(c[axis], low, tolerance))
                return axis * 2 + 1;
        }
        return -1;
    }

    private static bool On(float value, float plane, float tolerance) => Mathf.Abs(value - plane) <= tolerance;
}
