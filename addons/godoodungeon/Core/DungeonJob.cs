using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>Everything one dungeon generation needs, as plain data the background thread can read.</summary>
public sealed record DungeonRequest
{
    public LayoutSettings Layout { get; init; } = new();

    /// <summary>The GridMap cell of the bounds origin.</summary>
    public Vector3I Origin { get; init; }


    public CompiledTileSet RoomTiles { get; init; }
    public int DoorTile { get; init; } = -1;
    public string EdgeSocket { get; init; } = "outside";

    public CompiledTileSet CorridorTiles { get; init; }
    public string PathSocket { get; init; } = "path";
    public float PathWander { get; init; } = 0.3f;

    public long Seed { get; init; }
    public int LayoutAttempts { get; init; } = 4;
    public int MaxBacktracks { get; init; } = 2000;
    public int MaxRestarts { get; init; } = 20;

    /// <summary>GridMap cells holding hand-placed items. Rooms and corridors avoid them (brief §4.7).</summary>
    public IReadOnlySet<Vector3I> HandPlaced { get; init; } = new HashSet<Vector3I>();
}

public enum DungeonStatus
{
    Solved,

    /// <summary>The settings or tile sets cannot work; no layout was tried.</summary>
    Invalid,

    /// <summary>Every layout attempt failed. <see cref="DungeonResult.Attempts"/> lists why.</summary>
    Failed,

    Cancelled,

    /// <summary>
    /// Solved, but the connectivity check found rooms that cannot be reached: a bug, never a user
    /// error. <see cref="DungeonResult.Errors"/> names the rooms; nothing should be written.
    /// </summary>
    InternalError,
}

/// <summary>One finished WFC solve of the dungeon: its input and the variant per cell.</summary>
public sealed record DungeonSolve(SolveInput Input, int[] Cells, bool IsCorridor);

/// <summary>Why one layout attempt failed, for the status area.</summary>
public sealed record AttemptFailure(int Attempt, string Message, Vector3I? Cell = null);

/// <summary>The outcome of a <see cref="DungeonJob"/>.</summary>
public sealed record DungeonResult
{
    public DungeonStatus Status { get; init; }

    /// <summary>The layout that was solved, or the last one tried.</summary>
    public DungeonLayout Layout { get; init; }

    /// <summary>The room solves, then the corridor solve, when <see cref="Status"/> is Solved.</summary>
    public IReadOnlyList<DungeonSolve> Solves { get; init; } = Array.Empty<DungeonSolve>();

    /// <summary>The failed attempts before the result, or all of them when it failed.</summary>
    public IReadOnlyList<AttemptFailure> Attempts { get; init; } = Array.Empty<AttemptFailure>();

    /// <summary>Errors that stop generation before any layout (settings, tile sets).</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public int LayoutAttempt { get; init; }
    public long Backtracks { get; init; }
    public int Restarts { get; init; }
    public int RouteCount { get; init; }
    public int RouteCells { get; init; }

    /// <summary>The routed path faces per layout cell (see <see cref="CorridorPlan.Faces"/>), when solved.</summary>
    public int[] CorridorFaces { get; init; }

    /// <summary>The rooms the connectivity check could not reach, with <see cref="DungeonStatus.InternalError"/>.</summary>
    public IReadOnlyList<int> UnreachableRooms { get; init; } = Array.Empty<int>();
}

/// <summary>
/// Generates a dungeon on a background thread (brief godoodungeon §4.5): layout, then one solve per
/// room, then the corridors routed along the room graph (<see cref="CorridorRouter"/>) and solved,
/// each with a seed derived from the Generate seed. A room that still fails after the solver's
/// restarts, or corridors that cannot be routed or solved, start the next layout attempt. The owner polls <see cref="IsFinished"/> and writes the result on its own thread.
/// </summary>
public sealed class DungeonJob
{
    private readonly DungeonRequest _request;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private volatile bool _finished;
    private string _stage = "Waiting";
    private SolveJob _current;

    public DungeonJob(DungeonRequest request)
    {
        _request = request;
        _thread = new Thread(RunGuarded) { IsBackground = true, Name = "godoodungeon" };
    }

    public DungeonResult Result { get; private set; }

    /// <summary>Set when generation threw instead of returning a result.</summary>
    public Exception Error { get; private set; }

    public bool IsFinished => _finished;

    /// <summary>What the job is doing now, such as "Layout 1: room 3 of 12", for the status area.</summary>
    public string Stage => Volatile.Read(ref _stage);

    public void Start() => _thread.Start();

    /// <summary>Runs on the calling thread, for tests and tools.</summary>
    public DungeonResult RunNow()
    {
        RunGuarded();
        return Result;
    }

    public void Cancel()
    {
        _cancellation.Cancel();
        Volatile.Read(ref _current)?.Cancel();
    }

    public bool Wait(int milliseconds) => _thread.Join(milliseconds);

    private void RunGuarded()
    {
        try
        {
            Result = Run();
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

    private DungeonResult Run()
    {
        var request = _request;
        var settings = request.Layout;
        var errors = LayoutValidator.Validate(settings);
        if (request.RoomTiles == null || request.CorridorTiles == null)
            errors.Add("Both tile sets are needed.");
        if (errors.Count > 0)
            return new DungeonResult { Status = DungeonStatus.Invalid, Errors = errors };

        var occupied = Occupied(request);
        var failures = new List<AttemptFailure>();
        DungeonLayout layout = null;
        for (int attempt = 0; attempt < Math.Max(1, request.LayoutAttempts); attempt++)
        {
            if (_cancellation.IsCancellationRequested)
                return Cancelled(layout, failures);
            Volatile.Write(ref _stage, $"Layout {attempt + 1}: placing rooms");
            layout = LayoutGenerator.Generate(settings, request.Seed, attempt, request.Origin, occupied);
            if (!layout.IsValid)
            {
                failures.Add(new AttemptFailure(attempt, string.Join(" ", layout.Errors)));
                continue;
            }

            var solves = new List<DungeonSolve>();
            long backtracks = 0;
            int restarts = 0;
            string failed = null;
            Vector3I? failedCell = null;
            for (int room = 0; room < layout.Rooms.Count && failed == null; room++)
            {
                Volatile.Write(ref _stage, $"Layout {attempt + 1}: room {room + 1} of {layout.Rooms.Count}");
                var input = SolveInputs.Room(layout, room, settings.RoomHeight, request.RoomTiles, request.DoorTile, request.EdgeSocket);
                if (!input.IsValid)
                    return new DungeonResult { Status = DungeonStatus.Invalid, Layout = layout, Errors = new[] { input.Error } };
                var job = Solve(input, request.RoomTiles, LayoutSeeds.Room(request.Seed, attempt, room), request);
                if (job == null)
                    return Cancelled(layout, failures);
                backtracks += job.Result?.Backtracks ?? 0;
                restarts += job.Result?.Restarts ?? 0;
                if (job.Result?.Status == SolveStatus.Solved)
                    solves.Add(new DungeonSolve(input, job.Result.Cells, false));
                else
                {
                    failed = $"Room {room + 1} at {layout.GridCell(layout.Rooms[room].Position)} could not be solved ({Describe(job)}).";
                    failedCell = job.Result?.Failure is { } report ? input.Origin + report.Cell : null;
                }
            }
            if (failed != null)
            {
                failures.Add(new AttemptFailure(attempt, failed, failedCell));
                continue;
            }

            Volatile.Write(ref _stage, $"Layout {attempt + 1}: routing corridors");
            long corridorSeed = LayoutSeeds.Corridors(request.Seed, attempt);
            var plan = CorridorRouter.Route(layout, settings, occupied, request.PathWander, corridorSeed);
            if (!plan.IsValid)
            {
                failures.Add(new AttemptFailure(attempt, $"Corridors: {plan.Error}", plan.ErrorCell is { } at ? layout.GridCell(at) : null));
                continue;
            }

            Volatile.Write(ref _stage, $"Layout {attempt + 1}: corridors");
            bool HandPlaced(Vector3I cell) => request.HandPlaced.Contains(cell);
            var corridorInput = SolveInputs.Corridors(layout, plan, request.CorridorTiles, request.PathSocket, HandPlaced);
            if (!corridorInput.IsValid)
                return new DungeonResult { Status = DungeonStatus.Invalid, Layout = layout, Errors = new[] { corridorInput.Error } };
            var corridors = Solve(corridorInput, request.CorridorTiles, corridorSeed, request);
            if (corridors == null)
                return Cancelled(layout, failures);
            backtracks += corridors.Result?.Backtracks ?? 0;
            restarts += corridors.Result?.Restarts ?? 0;
            if (corridors.Result?.Status != SolveStatus.Solved)
            {
                failures.Add(new AttemptFailure(attempt, $"The corridors could not be solved ({Describe(corridors)}).",
                    corridors.Result?.Failure is { } report ? corridorInput.Origin + report.Cell : null));
                continue;
            }
            solves.Add(new DungeonSolve(corridorInput, corridors.Result.Cells, true));

            Volatile.Write(ref _stage, $"Layout {attempt + 1}: checking connections");
            var unreachable = DungeonConnectivity.UnreachableRooms(layout, corridorInput, corridors.Result.Cells, request.CorridorTiles, request.PathSocket);
            return new DungeonResult
            {
                Status = unreachable.Count == 0 ? DungeonStatus.Solved : DungeonStatus.InternalError,
                Errors = unreachable.Count == 0 ? Array.Empty<string>() : new[] { DungeonConnectivity.Describe(layout, unreachable) },
                UnreachableRooms = unreachable,
                Layout = layout, Solves = solves, Attempts = failures, LayoutAttempt = attempt,
                Backtracks = backtracks, Restarts = restarts,
                RouteCount = plan.RouteCount, RouteCells = plan.RouteCells, CorridorFaces = plan.Faces,
            };
        }
        return new DungeonResult { Status = DungeonStatus.Failed, Layout = layout, Attempts = failures };
    }

    /// <summary>Runs one solve as a <see cref="SolveJob"/>, so a cancel stops it mid-solve. Null when cancelled.</summary>
    private SolveJob Solve(SolveInput input, CompiledTileSet tiles, long seed, DungeonRequest request)
    {
        var options = new SolveOptions
        {
            Seed = seed, MaxBacktracks = request.MaxBacktracks, MaxRestarts = request.MaxRestarts,
            Boundary = input.Boundary, Restrictions = input.Restrictions,
        };
        var job = new SolveJob(input.Grid, tiles, input.Initial, options);
        Volatile.Write(ref _current, job);
        if (_cancellation.IsCancellationRequested)
            return null;
        job.Start();
        while (!job.Wait(20))
            if (_cancellation.IsCancellationRequested)
                job.Cancel();
        Volatile.Write(ref _current, null);
        if (job.Error != null)
            throw new InvalidOperationException("The solver failed.", job.Error);
        if (_cancellation.IsCancellationRequested || job.Result?.Status == SolveStatus.Cancelled)
            return null;
        return job;
    }

    private static string Describe(SolveJob job) => job.Result?.Status switch
    {
        SolveStatus.Impossible => "no tiles fit",
        SolveStatus.OutOfAttempts => $"out of attempts after {job.Result.Restarts} restarts",
        _ => "no result",
    };

    private static DungeonResult Cancelled(DungeonLayout layout, List<AttemptFailure> failures) =>
        new() { Status = DungeonStatus.Cancelled, Layout = layout, Attempts = failures };

    /// <summary>The layout grid cells over which a hand-placed cell sits within the room or corridor layers.</summary>
    private static OccupancyGrid Occupied(DungeonRequest request)
    {
        var occupied = new OccupancyGrid(request.Layout.Bounds);
        int layers = request.Layout.RoomHeight;
        foreach (var cell in request.HandPlaced)
            if (cell.Y >= request.Origin.Y && cell.Y < request.Origin.Y + layers)
                occupied.Take(new Vector2I(cell.X - request.Origin.X, cell.Z - request.Origin.Z));
        return occupied;
    }
}
