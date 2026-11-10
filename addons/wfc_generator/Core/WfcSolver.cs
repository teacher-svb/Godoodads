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
    private readonly ulong[] _start;
    private readonly int _startCount;
    private readonly double[] _weights;
    private readonly ulong[] _scratch;

    // Total weight of each cell's remaining options. Unresolved cells are chosen lightest first.
    private readonly double[] _mass;
    private readonly double _perUnit;

    // Weight of every byte value at every byte position of a domain: 8 lookups per 64-bit word.
    private readonly double[] _byteMass;

    // Unresolved cells grouped by remaining weight on a log scale (see BucketOf), with each
    // cell's bucket and position in it. Buckets below _lowestBucket are empty.
    private readonly List<int>[] _buckets;
    private readonly int[] _bucketOf;
    private readonly int[] _bucketPosition;
    private int _lowestBucket;

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

        // Free cells start without weight-0 variants. Fixed cells start full, so they may use them.
        _start = (ulong[])tiles.Generatable.Clone();
        foreach (ulong word in _start)
            _startCount += BitOperations.PopCount(word);
        _weights = new double[_variantCount];
        for (int v = 0; v < _variantCount; v++)
            _weights[v] = tiles.VariantWeights[v];

        _byteMass = new double[_words * 8 * 256];
        for (int v = 0; v < _variantCount; v++)
        {
            int table = (v >> 3) * 256, bit = 1 << (v & 7);
            for (int value = 0; value < 256; value++)
                if ((value & bit) != 0)
                    _byteMass[table + value] += _weights[v];
        }
        _mass = new double[cells];
        _bucketOf = new int[cells];
        _perUnit = 1 / _weights.Where(w => w > 0).Min();
        int bucketCount = BucketOf(2, _weights.Sum()) + 1;
        _buckets = new List<int>[bucketCount];
        for (int i = 0; i < bucketCount; i++)
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
        foreach (var bucket in _buckets)
            bucket.Clear();
        _lowestBucket = 0;
        _trailLength = 0;
        _decisionCount = 0;
        _queueHead = _queueTail = 0;
        Array.Clear(_queued);

        for (int cell = 0; cell < _grid.CellCount; cell++)
        {
            bool free = _initial[cell] == Free;
            (free ? _start : _full).CopyTo(_domains, cell * _words);
            _counts[cell] = free ? _startCount : _variantCount;
            _mass[cell] = MassOf(cell);
            if (_initial[cell] == Excluded)
                continue;
            AddToBucket(cell, BucketOf(_counts[cell], _mass[cell]));
            if (_counts[cell] == 1)
                _observer?.OnCellResolved(cell, FirstVariant(cell));
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
                _mass[cell] -= WordMass(word, before & ~after);
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
                int index = _trailWord[i];
                _mass[cell] += WordMass(index - cell * _words, _trailOld[i] & ~_domains[index]);
                _domains[index] = _trailOld[i];
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
        int key = BucketOf(count, _mass[cell]);
        if (previous == count && key == _bucketOf[cell])
            return;

        if (key != _bucketOf[cell])
        {
            var bucket = _buckets[_bucketOf[cell]];
            int position = _bucketPosition[cell];
            int last = bucket[^1];
            bucket[position] = last;
            _bucketPosition[last] = position;
            bucket.RemoveAt(bucket.Count - 1);
            AddToBucket(cell, key);
        }
        _counts[cell] = count;
        if (previous == count)
            return;

        if (_observer == null)
            return;
        if (count == 1)
            _observer.OnCellResolved(cell, FirstVariant(cell));
        else if (previous == 1)
            _observer.OnCellReset(cell);
    }

    private void AddToBucket(int cell, int bucket)
    {
        _bucketOf[cell] = bucket;
        _bucketPosition[cell] = _buckets[bucket].Count;
        _buckets[bucket].Add(cell);
        if (bucket < _lowestBucket)
            _lowestBucket = bucket;
    }

    /// <summary>
    /// Buckets 0 and 1 hold contradicted and resolved cells. Unresolved cells go from bucket 2 up,
    /// by remaining weight to within about 1%. With equal weights this is the fewest-options order.
    /// </summary>
    private int BucketOf(int count, double mass)
    {
        if (count <= 1)
            return count;
        // The exponent and top six mantissa bits of a double grow with its logarithm.
        long bits = BitConverter.DoubleToInt64Bits(Math.Max(1, mass * _perUnit));
        int bucket = 2 + (int)((bits >> 46) - (1023L << 6));
        return _buckets == null ? bucket : Math.Min(bucket, _buckets.Length - 1);
    }

    private double MassOf(int cell)
    {
        double mass = 0;
        for (int word = 0; word < _words; word++)
            mass += WordMass(word, _domains[cell * _words + word]);
        return mass;
    }

    private double WordMass(int word, ulong bits)
    {
        double mass = 0;
        for (int table = word * 8 * 256; bits != 0; bits >>= 8, table += 256)
            mass += _byteMass[table + (int)(bits & 0xFF)];
        return mass;
    }

    /// <summary>A random cell among the unresolved ones with the least weight left, or -1 when all are resolved.</summary>
    private int PickCell()
    {
        for (_lowestBucket = Math.Max(_lowestBucket, 2); _lowestBucket < _buckets.Length; _lowestBucket++)
        {
            var bucket = _buckets[_lowestBucket];
            if (bucket.Count > 0)
                return bucket[_random.Next(bucket.Count)];
        }
        return -1;
    }

    /// <summary>A random option of the cell, chosen in proportion to the variant weights.</summary>
    private int PickVariant(int cell)
    {
        int cellBase = cell * _words;
        double target = _random.NextDouble() * _mass[cell];
        int last = -1;
        for (int word = 0; word < _words; word++)
        {
            for (ulong bits = _domains[cellBase + word]; bits != 0; bits &= bits - 1)
            {
                last = word * 64 + BitOperations.TrailingZeroCount(bits);
                target -= _weights[last];
                if (target < 0)
                    return last;
            }
        }
        // Rounding can leave a tiny remainder; the last option then takes it.
        return last >= 0 ? last : throw new InvalidOperationException("The cell has no options.");
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

    public int Next(int bound) => (int)Math.BigMul(Mix(), (ulong)bound, out _);

    /// <summary>A double in [0, 1) from the top 53 bits.</summary>
    public double NextDouble() => (Mix() >> 11) * (1.0 / (1UL << 53));

    private ulong Mix()
    {
        _state = unchecked(_state + 0x9E3779B97F4A7C15UL);
        ulong z = _state;
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }
}
