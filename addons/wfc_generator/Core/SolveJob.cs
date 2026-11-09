using System;
using System.Threading;

namespace Godoodads.WfcGenerator.Core;

/// <summary>Runs one solve on a background thread. The owner polls it and applies the result on its own thread.</summary>
public sealed class SolveJob : ISolveObserver
{
    private readonly GridTopology _grid;
    private readonly CompiledTileSet _tiles;
    private readonly int[] _initial;
    private readonly SolveOptions _options;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;

    private int _resolved;
    private int _restarts;
    private volatile bool _finished;

    public SolveResult Result { get; private set; }

    /// <summary>Set when the solver threw instead of returning a result.</summary>
    public Exception Error { get; private set; }

    public bool IsFinished => _finished;
    public int ResolvedCells => Volatile.Read(ref _resolved);
    public int Restarts => Volatile.Read(ref _restarts);

    public SolveJob(GridTopology grid, CompiledTileSet tiles, int[] initial, SolveOptions options)
    {
        _grid = grid;
        _tiles = tiles;
        _initial = initial;
        _options = options;
        _thread = new Thread(Run) { IsBackground = true, Name = "WFC solve" };
    }

    public void Start() => _thread.Start();

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

    void ISolveObserver.OnCellResolved(int cell, int variant) => Volatile.Write(ref _resolved, _resolved + 1);

    void ISolveObserver.OnCellReset(int cell) => Volatile.Write(ref _resolved, _resolved - 1);

    void ISolveObserver.OnRestart()
    {
        Volatile.Write(ref _resolved, 0);
        Volatile.Write(ref _restarts, _restarts + 1);
    }
}
