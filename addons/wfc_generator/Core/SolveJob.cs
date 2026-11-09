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

/// <summary>Runs one solve on a background thread. The owner polls it and applies the result on its own thread.</summary>
public sealed class SolveJob : ISolveObserver
{
    private readonly GridTopology _grid;
    private readonly CompiledTileSet _tiles;
    private readonly int[] _initial;
    private readonly SolveOptions _options;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private readonly ConcurrentQueue<SolveEvent> _events;

    private int _resolved;
    private int _restarts;
    private volatile bool _finished;

    public SolveResult Result { get; private set; }

    /// <summary>Set when the solver threw instead of returning a result.</summary>
    public Exception Error { get; private set; }

    public bool IsFinished => _finished;
    public int ResolvedCells => Volatile.Read(ref _resolved);
    public int Restarts => Volatile.Read(ref _restarts);

    /// <param name="recordEvents">Queue every change for <see cref="TryTakeEvent"/>, for showing the solve as it runs.</param>
    public SolveJob(GridTopology grid, CompiledTileSet tiles, int[] initial, SolveOptions options, bool recordEvents = false)
    {
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
            Result = new WfcSolver(_grid, _tiles, _initial).Solve(_options, this, _cancellation.Token);
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
