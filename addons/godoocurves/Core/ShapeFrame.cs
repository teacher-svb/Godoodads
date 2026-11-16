using Godot;

namespace Godoodads.Curves.Core;

/// <summary>
/// Where a shape goes (brief godoocurves §4.2): its start point and orthonormal forward, up and side
/// axes. Shapes are built in local coordinates, X along forward, Y along up and Z along side, and
/// mapped to the path's space with <see cref="ToPath"/>.
/// </summary>
public readonly record struct ShapeFrame(Vector3 Origin, Vector3 Forward, Vector3 Up)
{
    /// <summary>The right-hand side of a traveller heading forward with this up: forward × up.</summary>
    public Vector3 Side => Forward.Cross(Up);

    /// <summary>The default frame: the origin, heading +X with +Y up.</summary>
    public static ShapeFrame Default => new(Vector3.Zero, Vector3.Right, Vector3.Up);

    /// <summary>
    /// Builds a frame from a start point, a direction and an up vector. Up is made perpendicular to
    /// the direction. Fails with a message when either is zero or they are parallel.
    /// </summary>
    public static bool TryCreate(Vector3 start, Vector3 direction, Vector3 up, out ShapeFrame frame, out string error)
    {
        frame = default;
        if (direction.LengthSquared() < 1e-12f)
        {
            error = "Direction is zero. Give the direction the shape starts in, for example (1, 0, 0).";
            return false;
        }
        var forward = direction.Normalized();
        var lifted = up - forward * up.Dot(forward);
        if (up.LengthSquared() < 1e-12f || lifted.LengthSquared() < 1e-8f * up.LengthSquared())
        {
            error = "Up is zero or parallel to Direction. Give an up direction across the start direction, for example (0, 1, 0).";
            return false;
        }
        frame = new ShapeFrame(start, forward, lifted.Normalized());
        error = null;
        return true;
    }

    /// <summary>A point from local coordinates (forward, up, side) to the path's space.</summary>
    public Vector3 ToPath(Vector3 local) => Origin + ToPathDirection(local);

    /// <summary>A direction or handle from local coordinates to the path's space, without the origin.</summary>
    public Vector3 ToPathDirection(Vector3 local) => Forward * local.X + Up * local.Y + Side * local.Z;
}

/// <summary>Which way a flat shape turns, seen by a traveller heading forward: its plane is forward and this direction.</summary>
public enum Bend
{
    Left,
    Right,
    Up,
    Down,
}

public static class BendExtensions
{
    /// <summary>The bend direction in local coordinates (X forward, Y up, Z side = right).</summary>
    public static Vector3 Local(this Bend bend) => bend switch
    {
        Bend.Left => new Vector3(0, 0, -1),
        Bend.Right => new Vector3(0, 0, 1),
        Bend.Up => new Vector3(0, 1, 0),
        _ => new Vector3(0, -1, 0),
    };
}
