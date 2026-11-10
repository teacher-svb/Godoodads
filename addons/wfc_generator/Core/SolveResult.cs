using System.Collections.Generic;
using Godot;

namespace Godoodads.WfcGenerator.Core;

public enum SolveStatus
{
    Solved,

    /// <summary>Proven to have no solution with these tiles and fixed cells.</summary>
    Impossible,

    /// <summary>The backtrack and restart limits ran out. A solution may still exist.</summary>
    OutOfAttempts,

    Cancelled,
}

/// <summary>Limits are counted, never timed, so that results stay deterministic.</summary>
public sealed record SolveOptions
{
    public long Seed { get; init; }

    /// <summary>Backtracks allowed in one attempt before restarting.</summary>
    public int MaxBacktracks { get; init; } = 2000;

    /// <summary>Restarts allowed after the first attempt.</summary>
    public int MaxRestarts { get; init; } = 20;

    /// <summary>
    /// Per direction (see <see cref="Directions"/>), the socket of a fixed neighbour imagined
    /// outside that face of the grid. Null, or a null entry, leaves the face unconstrained.
    /// </summary>
    public IReadOnlyList<Socket?> Boundary { get; init; }
}

/// <summary>Receives every change to the solved state. Replaying the calls reproduces the final grid.</summary>
public interface ISolveObserver
{
    void OnCellResolved(int cell, int variant);
    void OnCellReset(int cell);

    /// <summary>The solver is starting over: forget every cell.</summary>
    void OnRestart();
}

/// <summary>A neighbour of the failing cell and the sockets it presents towards that cell.</summary>
public sealed record FailureNeighbour(int Direction, Vector3I Cell, int OptionCount, IReadOnlyList<string> Sockets);

public sealed record FailureReport(Vector3I Cell, IReadOnlyList<FailureNeighbour> Neighbours, bool HasFixedCells);

/// <summary>Two adjacent fixed cells whose facing sockets do not match.</summary>
public sealed record FixedConflict(Vector3I CellA, Vector3I CellB, int Direction, string SocketA, string SocketB);

public sealed record SolveResult
{
    private static readonly FixedConflict[] NoConflicts = new FixedConflict[0];

    public SolveStatus Status { get; init; }

    /// <summary>Variant per cell when solved, with -1 for excluded cells. Otherwise null.</summary>
    public int[] Cells { get; init; }

    public long Backtracks { get; init; }
    public int Restarts { get; init; }
    public FailureReport Failure { get; init; }
    public IReadOnlyList<FixedConflict> FixedConflicts { get; init; } = NoConflicts;
}
