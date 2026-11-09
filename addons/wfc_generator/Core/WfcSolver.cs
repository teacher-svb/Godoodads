using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// Wave function collapse over bitset domains, with a trail of changes for chronological
/// backtracking and seed-derived restarts. A solver instance is not thread-safe, but it can run
/// on a background thread because it touches no engine objects.
/// </summary>
public sealed class WfcSolver
{
    /// <summary>Initial cell value: the solver chooses this cell.</summary>
    public const int Free = -1;

    /// <summary>Initial cell value: not solved, and no constraint on its neighbours.</summary>
    public const int Excluded = -2;

    private enum Outcome { Solved, Impossible, LimitReached, Cancelled }

    private readonly GridTopology _grid;
    private readonly CompiledTileSet _tiles;
    private readonly int[] _initial;
    private readonly int _variantCount;
    private readonly int _words;
    private readonly bool _hasFixedCells;

    private readonly ulong[] _domains;
    private readonly int[] _counts;
    private readonly ulong[] _full;
    private readonly ulong[] _scratch;

    // Cells grouped by option count, with each cell's position in its bucket.
    private readonly List<int>[] _buckets;
    private readonly int[] _bucketPosition;

    private readonly int[] _queue;
    private readonly bool[] _queued;
    private int _queueHead;
    private int _queueTail;

    private int[] _trailWord;
    private ulong[] _trailOld;
    private int _trailLength;

    private (int TrailMark, int Cell, int Variant)[] _decisions;
    private int _decisionCount;

    private WfcRandom _random;
    private ISolveObserver _observer;
    private int _contradiction;
    private FailureReport _failure;

    /// <param name="initial">Per-cell variant index, <see cref="Free"/> or <see cref="Excluded"/>. Null means all free.</param>
    public WfcSolver(GridTopology grid, CompiledTileSet tiles, int[] initial = null)
    {
        if (!tiles.IsValid)
            throw new ArgumentException("The tile set has errors.", nameof(tiles));
        if (initial != null && initial.Length != grid.CellCount)
            throw new ArgumentException("There must be one initial value per cell.", nameof(initial));

        _grid = grid;
        _tiles = tiles;
        _variantCount = tiles.Variants.Count;
        _words = tiles.WordCount;
        _initial = initial ?? Enumerable.Repeat(Free, grid.CellCount).ToArray();
        foreach (int value in _initial)
            if (value < Excluded || value >= _variantCount)
                throw new ArgumentOutOfRangeException(nameof(initial), value, "Unknown variant.");
        _hasFixedCells = _initial.Any(value => value >= 0);

        int cells = grid.CellCount;
        _domains = new ulong[cells * _words];
        _counts = new int[cells];
        _bucketPosition = new int[cells];
        _queue = new int[cells + 1];
        _queued = new bool[cells];
        _trailWord = new int[cells];
        _trailOld = new ulong[cells];
        _decisions = new (int, int, int)[1024];
        _scratch = new ulong[_words];

        _full = new ulong[_words];
        for (int v = 0; v < _variantCount; v++)
            _full[v >> 6] |= 1UL << v;

        _buckets = new List<int>[_variantCount + 1];
        for (int i = 0; i <= _variantCount; i++)
            _buckets[i] = new List<int>();
    }

    public SolveResult Solve(SolveOptions options = null, ISolveObserver observer = null, CancellationToken cancellation = default)
    {
        options ??= new SolveOptions();
        _observer = observer;
        _failure = null;

        var conflicts = FindFixedConflicts();
        if (conflicts.Count > 0)
            return new SolveResult { Status = SolveStatus.Impossible, FixedConflicts = conflicts };

        long totalBacktracks = 0;
        for (int attempt = 0; ; attempt++)
        {
            if (attempt > 0)
                _observer?.OnRestart();
            _random = new WfcRandom(options.Seed, attempt);
            bool lastAttempt = attempt >= options.MaxRestarts;
            var outcome = RunAttempt(options.MaxBacktracks, lastAttempt, cancellation, out long backtracks);
            totalBacktracks += backtracks;

            switch (outcome)
            {
                case Outcome.Solved:
                    return new SolveResult
                    {
                        Status = SolveStatus.Solved,
                        Cells = ExtractCells(),
                        Backtracks = totalBacktracks,
                        Restarts = attempt,
                    };
                case Outcome.Impossible:
                    return Failed(SolveStatus.Impossible, totalBacktracks, attempt);
                case Outcome.Cancelled:
                    return Failed(SolveStatus.Cancelled, totalBacktracks, attempt);
                case Outcome.LimitReached when lastAttempt:
                    return Failed(SolveStatus.OutOfAttempts, totalBacktracks, attempt);
            }
        }
    }

    private SolveResult Failed(SolveStatus status, long backtracks, int restarts) => new()
    {
        Status = status,
        Backtracks = backtracks,
        Restarts = restarts,
        Failure = _failure,
    };

    private Outcome RunAttempt(int maxBacktracks, bool lastAttempt, CancellationToken cancellation, out long backtracks)
    {
        backtracks = 0;
        Reset();
        bool consistent = Propagate();

        while (true)
        {
            while (!consistent)
            {
                if (_decisionCount == 0)
                {
                    _failure = BuildFailure(_contradiction);
                    return Outcome.Impossible;
                }
                if (backtracks >= maxBacktracks)
                {
                    if (lastAttempt)
                        _failure = BuildFailure(_contradiction);
                    return Outcome.LimitReached;
                }

                backtracks++;
                var decision = _decisions[--_decisionCount];
                Undo(decision.TrailMark);

                // Every arrangement with this choice failed, so it is banned at the earlier level.
                _full.CopyTo(_scratch, 0);
                _scratch[decision.Variant >> 6] &= ~(1UL << decision.Variant);
                Narrow(decision.Cell);
                Enqueue(decision.Cell);
                consistent = Propagate();
            }

            if (cancellation.IsCancellationRequested)
                return Outcome.Cancelled;

            int cell = PickCell();
            if (cell < 0)
                return Outcome.Solved;

            int variant = PickVariant(cell);
            PushDecision(cell, variant);
            SetScratchToSingle(variant);
            Narrow(cell);
            Enqueue(cell);
            consistent = Propagate();
        }
    }

    private void Reset()
    {
        for (int cell = 0; cell < _grid.CellCount; cell++)
            _full.CopyTo(_domains, cell * _words);
        Array.Fill(_counts, _variantCount);
        foreach (var bucket in _buckets)
            bucket.Clear();
        _trailLength = 0;
        _decisionCount = 0;
        _queueHead = _queueTail = 0;
        Array.Clear(_queued);

        for (int cell = 0; cell < _grid.CellCount; cell++)
        {
            if (_initial[cell] == Excluded)
                continue;
            AddToBucket(cell, _variantCount);
            if (_variantCount == 1)
                _observer?.OnCellResolved(cell, 0);
        }

        for (int cell = 0; cell < _grid.CellCount; cell++)
        {
            if (_initial[cell] < 0)
                continue;
            SetScratchToSingle(_initial[cell]);
            Narrow(cell);
        }

        // Starting from every cell also removes tiles that can never fit before any choice is made.
        for (int cell = 0; cell < _grid.CellCount; cell++)
            if (_initial[cell] != Excluded)
                Enqueue(cell);
    }

    private bool Propagate()
    {
        while (_queueHead != _queueTail)
        {
            int cell = _queue[_queueHead];
            _queueHead = (_queueHead + 1) % _queue.Length;
            _queued[cell] = false;

            foreach (int direction in _grid.ActiveDirections)
            {
                int neighbour = _grid.Neighbour(cell, direction);
                if (neighbour < 0 || _initial[neighbour] == Excluded || !BuildAllowed(cell, direction, neighbour))
                    continue;
                if (!Narrow(neighbour))
                    continue;
                if (_counts[neighbour] == 0)
                {
                    _contradiction = neighbour;
                    ClearQueue();
                    return false;
                }
                Enqueue(neighbour);
            }
        }
        return true;
    }

    /// <summary>
    /// Fills the scratch mask with what the cell allows in a direction. Returns false as soon as
    /// the mask covers the neighbour's whole domain, because nothing could be removed.
    /// </summary>
    private bool BuildAllowed(int cell, int direction, int neighbour)
    {
        Array.Clear(_scratch);
        int cellBase = cell * _words;
        int neighbourBase = neighbour * _words;
        var compatible = _tiles.Compatible;

        for (int word = 0; word < _words; word++)
        {
            ulong bits = _domains[cellBase + word];
            while (bits != 0)
            {
                int variant = word * 64 + BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;

                int offset = _tiles.CompatibleOffset(variant, direction);
                bool covered = true;
                for (int w = 0; w < _words; w++)
                {
                    _scratch[w] |= compatible[offset + w];
                    if ((_domains[neighbourBase + w] & ~_scratch[w]) != 0)
                        covered = false;
                }
                if (covered)
                    return false;
            }
        }
        return true;
    }

    /// <summary>Intersects the cell's domain with the scratch mask. Returns true if anything was removed.</summary>
    private bool Narrow(int cell)
    {
        int cellBase = cell * _words;
        int count = 0;
        bool changed = false;
        for (int word = 0; word < _words; word++)
        {
            ulong before = _domains[cellBase + word];
            ulong after = before & _scratch[word];
            if (after != before)
            {
                PushTrail(cellBase + word, before);
                _domains[cellBase + word] = after;
                changed = true;
            }
            count += BitOperations.PopCount(after);
        }
        if (changed)
            SetCount(cell, count);
        return changed;
    }

    private void Undo(int mark)
    {
        int i = _trailLength;
        while (i > mark)
        {
            // Changes to one cell are stored together. Restore them all before updating its count.
            int cell = _trailWord[i - 1] / _words;
            while (i > mark && _trailWord[i - 1] / _words == cell)
            {
                i--;
                _domains[_trailWord[i]] = _trailOld[i];
            }

            int count = 0;
            for (int word = 0; word < _words; word++)
                count += BitOperations.PopCount(_domains[cell * _words + word]);
            SetCount(cell, count);
        }
        _trailLength = mark;
    }

    private void SetCount(int cell, int count)
    {
        int previous = _counts[cell];
        if (previous == count)
            return;

        var bucket = _buckets[previous];
        int position = _bucketPosition[cell];
        int last = bucket[^1];
        bucket[position] = last;
        _bucketPosition[last] = position;
        bucket.RemoveAt(bucket.Count - 1);
        AddToBucket(cell, count);
        _counts[cell] = count;

        if (_observer == null)
            return;
        if (count == 1)
            _observer.OnCellResolved(cell, FirstVariant(cell));
        else if (previous == 1)
            _observer.OnCellReset(cell);
    }

    private void AddToBucket(int cell, int count)
    {
        _bucketPosition[cell] = _buckets[count].Count;
        _buckets[count].Add(cell);
    }

    /// <summary>A random cell among those with the fewest options above one, or -1 when all are resolved.</summary>
    private int PickCell()
    {
        for (int count = 2; count <= _variantCount; count++)
        {
            var bucket = _buckets[count];
            if (bucket.Count > 0)
                return bucket[_random.Next(bucket.Count)];
        }
        return -1;
    }

    private int PickVariant(int cell)
    {
        int remaining = _random.Next(_counts[cell]);
        for (int word = 0; word < _words; word++)
        {
            ulong bits = _domains[cell * _words + word];
            int population = BitOperations.PopCount(bits);
            if (remaining >= population)
            {
                remaining -= population;
                continue;
            }
            for (; remaining > 0; remaining--)
                bits &= bits - 1;
            return word * 64 + BitOperations.TrailingZeroCount(bits);
        }
        throw new InvalidOperationException("The option count does not match the domain.");
    }

    private int FirstVariant(int cell)
    {
        for (int word = 0; word < _words; word++)
        {
            ulong bits = _domains[cell * _words + word];
            if (bits != 0)
                return word * 64 + BitOperations.TrailingZeroCount(bits);
        }
        return -1;
    }

    private void SetScratchToSingle(int variant)
    {
        Array.Clear(_scratch);
        _scratch[variant >> 6] = 1UL << variant;
    }

    private void Enqueue(int cell)
    {
        if (_queued[cell])
            return;
        _queued[cell] = true;
        _queue[_queueTail] = cell;
        _queueTail = (_queueTail + 1) % _queue.Length;
    }

    private void ClearQueue()
    {
        while (_queueHead != _queueTail)
        {
            _queued[_queue[_queueHead]] = false;
            _queueHead = (_queueHead + 1) % _queue.Length;
        }
    }

    private void PushTrail(int word, ulong before)
    {
        if (_trailLength == _trailWord.Length)
        {
            Array.Resize(ref _trailWord, _trailLength * 2);
            Array.Resize(ref _trailOld, _trailLength * 2);
        }
        _trailWord[_trailLength] = word;
        _trailOld[_trailLength] = before;
        _trailLength++;
    }

    private void PushDecision(int cell, int variant)
    {
        if (_decisionCount == _decisions.Length)
            Array.Resize(ref _decisions, _decisionCount * 2);
        _decisions[_decisionCount++] = (_trailLength, cell, variant);
    }

    private int[] ExtractCells()
    {
        var cells = new int[_grid.CellCount];
        for (int cell = 0; cell < cells.Length; cell++)
            cells[cell] = _initial[cell] == Excluded ? -1 : FirstVariant(cell);
        return cells;
    }

    private List<FixedConflict> FindFixedConflicts()
    {
        var conflicts = new List<FixedConflict>();
        for (int cell = 0; cell < _grid.CellCount; cell++)
        {
            int variant = _initial[cell];
            if (variant < 0)
                continue;
            foreach (int direction in _grid.ActiveDirections)
            {
                // Each pair is checked once, from its positive side.
                if ((direction & 1) != 0)
                    continue;
                int neighbour = _grid.Neighbour(cell, direction);
                if (neighbour < 0 || _initial[neighbour] < 0 || _tiles.Allows(variant, direction, _initial[neighbour]))
                    continue;
                conflicts.Add(new FixedConflict(
                    _grid.Coordinates(cell), _grid.Coordinates(neighbour), direction,
                    _tiles.Variants[variant].Sockets[direction].ToString(),
                    _tiles.Variants[_initial[neighbour]].Sockets[Directions.Opposite(direction)].ToString()));
            }
        }
        return conflicts;
    }

    private FailureReport BuildFailure(int cell)
    {
        var neighbours = new List<FailureNeighbour>();
        foreach (int direction in _grid.ActiveDirections)
        {
            int neighbour = _grid.Neighbour(cell, direction);
            if (neighbour < 0 || _initial[neighbour] == Excluded)
                continue;

            var sockets = new SortedSet<string>(StringComparer.Ordinal);
            for (int word = 0; word < _words; word++)
            {
                ulong bits = _domains[neighbour * _words + word];
                for (; bits != 0; bits &= bits - 1)
                {
                    int variant = word * 64 + BitOperations.TrailingZeroCount(bits);
                    sockets.Add(_tiles.Variants[variant].Sockets[Directions.Opposite(direction)].ToString());
                }
            }
            neighbours.Add(new FailureNeighbour(direction, _grid.Coordinates(neighbour), _counts[neighbour], sockets.ToArray()));
        }
        return new FailureReport(_grid.Coordinates(cell), neighbours, _hasFixedCells);
    }
}

/// <summary>SplitMix64. Each restart gets its own stream derived from the seed.</summary>
internal sealed class WfcRandom
{
    private ulong _state;

    public WfcRandom(long seed, int attempt) => _state = unchecked((ulong)seed + (ulong)attempt * 0xA0761D6478BD642FUL);

    public int Next(int bound)
    {
        _state = unchecked(_state + 0x9E3779B97F4A7C15UL);
        ulong z = _state;
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        z ^= z >> 31;
        return (int)Math.BigMul(z, (ulong)bound, out _);
    }
}
