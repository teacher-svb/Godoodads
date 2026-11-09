using Godot;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// The six axis directions, used as indices into per-face data. A 2D grid uses the X and Z
/// directions, so rotation around Y is an in-plane rotation for both kinds of grid.
/// </summary>
public static class Directions
{
    public const int PosX = 0;
    public const int NegX = 1;
    public const int PosY = 2;
    public const int NegY = 3;
    public const int PosZ = 4;
    public const int NegZ = 5;
    public const int Count = 6;

    public static readonly int[] All3D = { PosX, NegX, PosY, NegY, PosZ, NegZ };
    public static readonly int[] All2D = { PosX, NegX, PosZ, NegZ };

    private static readonly Vector3I[] Offsets =
    {
        new(1, 0, 0), new(-1, 0, 0), new(0, 1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1),
    };

    private static readonly string[] Names = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };

    // Where each direction ends up after +90 degrees around +Y, matching Godot's Basis.
    private static readonly int[] QuarterTurnY = { NegZ, PosZ, PosY, NegY, PosX, NegX };

    public static int Opposite(int direction) => direction ^ 1;

    public static bool IsVertical(int direction) => direction is PosY or NegY;

    public static Vector3I Offset(int direction) => Offsets[direction];

    public static string Name(int direction) => Names[direction];

    /// <summary>The direction a face points to after <paramref name="quarterTurns"/> × 90 degrees around +Y.</summary>
    public static int RotateY(int direction, int quarterTurns)
    {
        int turns = ((quarterTurns % 4) + 4) % 4;
        for (int i = 0; i < turns; i++)
            direction = QuarterTurnY[direction];
        return direction;
    }
}
