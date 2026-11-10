using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Godoodads.WfcGenerator.Core;

/// <summary>A change to the solved state: a cell resolved, a cell reset (Variant -1) or a restart (Cell -1).</summary>
public readonly record struct SolveEvent(int Cell, int Variant)
{
    public static readonly SolveEvent Restart = new(-1, -1);
    public bool IsRestart => Cell < 0;
}

/// <summary>
/// Guided paths to lay out before solving (brief §13.3). Coordinates in messages are offset by
/// <paramref name="Origin"/>. When the tiles around a route cannot be filled in, up to
/// <paramref name="Attempts"/> routes are tried, each from a seed derived from the solve seed.
/// </summary>
public sealed record PathRequest(string Socket, double Wander, Godot.Vector3I Origin = default, int Attempts = PathRequest.DefaultAttempts)
{
    public const int DefaultAttempts = 8;
}

/// <summary>
/// Runs one solve on a background thread, after laying out paths when asked. The owner polls it
/// and applies the result on its own thread.
/// </summary>
public sealed class SolveJob : ISolveObserver
{
    private readonly GridTopology _grid;
    private readonly CompiledTileSet _tiles;
    private readonly int[] _initial;
    private readonly SolveOptions _options;
    private readonly PathRequest _paths;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private readonly ConcurrentQueue<SolveEvent> _events;

    private int _resolved;
    private int _restarts;
    private volatile bool _finished;

    /// <summary>Null when the job finished without a result: cancelled before solving, or the paths have errors.</summary>
    public SolveResult Result { get; private set; }

    /// <summary>The path layout used, when paths were asked for. Check <see cref="PathPlan.IsValid"/>.</summary>
    public PathPlan Plan { get; private set; }

    /// <summary>How many routes were laid out, counting the one used.</summary>
    public int RouteAttempts { get; private set; }

    /// <summary>Set when the solver threw instead of returning a result.</summary>
    public Exception Error { get; private set; }

    public bool IsFinished => _finished;
    public int ResolvedCells => Volatile.Read(ref _resolved);
    public int Restarts => Volatile.Read(ref _restarts);

    /// <param name="recordEvents">Queue every change for <see cref="TryTakeEvent"/>, for showing the solve as it runs.</param>
    public SolveJob(GridTopology grid, CompiledTileSet tiles, int[] initial, SolveOptions options, bool recordEvents = false, PathRequest paths = null)
    {
        _paths = paths;
        _events = recordEvents ? new ConcurrentQueue<SolveEvent>() : null;
        _grid = grid;
        _tiles = tiles;
        _initial = initial;
        _options = options;
        _thread = new Thread(Run) { IsBackground = true, Name = "WFC solve" };
    }

    public void Start() => _thread.Start();

    /// <summary>Takes the oldest recorded change. Always false when events are not recorded.</summary>
    public bool TryTakeEvent(out SolveEvent change)
    {
        change = default;
        return _events != null && _events.TryDequeue(out change);
    }

    public bool HasPendingEvents => _events != null && !_events.IsEmpty;

    public void Cancel() => _cancellation.Cancel();

    /// <summary>Waits for the thread to end. Returns false if it is still running after the timeout.</summary>
    public bool Wait(int milliseconds) => _thread.Join(milliseconds);

    private void Run()
    {
        try
        {
            if (_paths == null)
            {
                Result = new WfcSolver(_grid, _tiles, _initial).Solve(_options, this, _cancellation.Token);
                return;
            }

            // A route can leave the tiles around it no way to fit. That shows up as Impossible,
            // usually before any choice, so another route is tried.
            var solver = new WfcSolver(_grid, _tiles, _initial);
            for (int attempt = 0; attempt < Math.Max(1, _paths.Attempts); attempt++)
            {
                long routeSeed = attempt == 0 ? _options.Seed : unchecked(_options.Seed + attempt * 0x5851F42D4C957F2DL);
                Plan = PathRouter.Plan(_grid, _tiles, _initial, _paths.Socket, _paths.Wander, routeSeed, _paths.Origin);
                RouteAttempts = attempt + 1;
                if (!Plan.IsValid)
                    return;
                if (attempt > 0)
                    ((ISolveObserver)this).OnRestart();
                Result = solver.Solve(_options with { Restrictions = Plan.Restrictions }, this, _cancellation.Token);
                if (Result.Status != SolveStatus.Impossible || Plan.RouteCount == 0)
                    return;
            }
        }
        catch (Exception e)
        {
            Error = e;
        }
        finally
        {
            _finished = true;
        }
    }

    void ISolveObserver.OnCellResolved(int cell, int variant)
    {
        Volatile.Write(ref _resolved, _resolved + 1);
        _events?.Enqueue(new SolveEvent(cell, variant));
    }

    void ISolveObserver.OnCellReset(int cell)
    {
        Volatile.Write(ref _resolved, _resolved - 1);
        _events?.Enqueue(new SolveEvent(cell, -1));
    }

    void ISolveObserver.OnRestart()
    {
        _events?.Enqueue(SolveEvent.Restart);
        Volatile.Write(ref _resolved, 0);
        Volatile.Write(ref _restarts, _restarts + 1);
    }
}
