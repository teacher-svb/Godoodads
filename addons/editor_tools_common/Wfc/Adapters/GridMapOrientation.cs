using System;

namespace Godoodads.WfcGenerator.Adapters;

/// <summary>Converts between quarter turns around +Y and GridMap orthogonal orientation indices.</summary>
public static class GridMapOrientation
{
    private static readonly int[] QuarterTurns = { 0, 16, 10, 22 };

    public static int FromQuarterTurns(int turns) => QuarterTurns[((turns % 4) + 4) % 4];

    /// <summary>The quarter turns of an orientation, or -1 if it is not a pure rotation around +Y.</summary>
    public static int ToQuarterTurns(int orientation) => Array.IndexOf(QuarterTurns, orientation);
}
