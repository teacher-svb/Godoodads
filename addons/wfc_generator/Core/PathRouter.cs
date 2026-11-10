using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Godot;

namespace Godoodads.WfcGenerator.Core;

/// <summary>A problem found before solving, with the cells it is about (grid coordinates plus the plan's origin).</summary>
public sealed record PathError(string Message, IReadOnlyList<Vector3I> Cells);

/// <summary>The routes found between placed path cells, as limits for the solver.</summary>
public sealed class PathPlan
{
    private static readonly PathError[] NoErrors = new PathError[0];

    /// <summary>Null when there are errors.</summary>
    public CellRestrictions Restrictions { get; init; }

    /// <summary>Per cell, the directions (bit per <see cref="Directions"/> index) its path must open to; 0 off the route.</summary>
    public int[] RouteFaces { get; init; }

    /// <summary>Placed cells with at least one path face.</summary>
    public int PlacedPathCells { get; init; }

    public int RouteCount { get; init; }
    public int RouteCellCount { get; init; }
    public IReadOnlyList<PathError> Errors { get; init; } = NoErrors;
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Guided paths (brief §13.3). Placed cells with path faces are joined into one network by routes
/// through free cells. A route only takes a step where the tile set has a path variant, allowed in
/// that layer, with that pair of path faces and a socket that fits the one arriving, which is also
/// how it learns to climb. The routes become solver restrictions: route cells may only use the
/// variants that fit their route, and every other free cell may not use a path variant at all.
/// </summary>
public static class PathRouter
{
    /// <summary>At Path Wander 1, a cell costs between 1 and 1 + this.</summary>
    public const double WanderCostRange = 4;

    /// <summary>
    /// The socket name of a Path Socket entry: surrounding spaces and a <c>+</c>, <c>-</c> or
    /// <c>@n</c> suffix are ignored. Empty means paths are off.
    /// </summary>
    public static bool TryParseName(string label, out string name, out string error)
    {
        error = null;
        name = (label ?? "").Trim();
        int at = name.IndexOf('@');
        if (at >= 0)
            name = name[..at];
        else if (name.EndsWith('+') || name.EndsWith('-'))
            name = name[..^1];
        if (name.Length > 0 && !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            error = $"'{label}' is not a valid path socket. Names use letters, digits and underscores.";
            return false;
        }
        return true;
    }

    /// <summary>Per variant, the bit mask of its faces whose socket has the path name.</summary>
    public static int[] PathFaces(CompiledTileSet set, string name)
    {
        var faces = new int[set.Variants.Count];
        for (int variant = 0; variant < faces.Length; variant++)
            foreach (int direction in set.ActiveDirections)
                if (set.Variants[variant].Sockets[direction].Name == name)
                    faces[variant] |= 1 << direction;
        return faces;
    }

    /// <param name="origin">Added to the coordinates in error messages and cells, for example the bounds origin.</param>
    public static PathPlan Plan(GridTopology grid, CompiledTileSet set, int[] initial, string pathSocket, double wander, long seed,
        Vector3I origin = default)
    {
        if (!TryParseName(pathSocket, out string name, out string nameError))
            return Failed(new PathError(nameError, Array.Empty<Vector3I>()));
        if (name.Length == 0)
            return new PathPlan();
        initial ??= Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray();
        return new Router(grid, set, initial, name, Math.Clamp(wander, 0, 1), seed, origin).Run();
    }

    private static PathPlan Failed(params PathError[] errors) => new() { Errors = errors };

    private sealed record Stub(int Index, int FixedCell, int EntryCell, int EntryFace, int Group, Socket Incoming);

    /// <summary>Leaving a cell through <paramref name="Exit"/> with <paramref name="Variant"/>, which shows <paramref name="Outgoing"/> there.</summary>
    private readonly record struct Move(int Exit, int Outgoing, int Variant);

    private sealed class Node
    {
        public double Cost;
        public int Previous;
        public int Source;

        /// <summary>The variant the previous cell uses to step into this one.</summary>
        public int PreviousVariant;
    }

    private sealed class Router
    {
        private readonly GridTopology _grid;
        private readonly CompiledTileSet _set;
        private readonly int[] _initial;
        private readonly string _name;
        private readonly double _wander;
        private readonly long _seed;
        private readonly Vector3I _origin;
        private readonly int[] _faces;
        private readonly List<Socket> _sockets = new();
        private readonly int[] _routeFaces;
        private readonly int[] _routeVariant;
        private readonly List<Stub> _stubs = new();
        private readonly bool[] _pending;
        private readonly int[] _groupOf;
        private readonly List<PathError> _errors = new();
        private int _routeCount;

        // Per layer: variants allowed there by mask, and the moves from (entry face, arriving socket).
        private readonly Dictionary<int, List<int>>[] _byMask;
        private readonly Dictionary<int, List<Move>>[] _moves;

        public Router(GridTopology grid, CompiledTileSet set, int[] initial, string name, double wander, long seed, Vector3I origin)
        {
            _origin = origin;
            _grid = grid;
            _set = set;
            _initial = initial;
            _name = name;
            _wander = wander;
            _seed = seed;
            _faces = PathFaces(set, name);
            _routeFaces = new int[grid.CellCount];
            _routeVariant = Enumerable.Repeat(-1, grid.CellCount).ToArray();
            _groupOf = Enumerable.Repeat(-1, grid.CellCount).ToArray();

            for (int variant = 0; variant < _faces.Length; variant++)
                foreach (int direction in set.ActiveDirections)
                    if ((_faces[variant] & 1 << direction) != 0)
                        SocketId(set.Variants[variant].Sockets[direction]);

            int layers = grid.Size.Y;
            _byMask = new Dictionary<int, List<int>>[layers];
            _moves = new Dictionary<int, List<Move>>[layers];
            for (int layer = 0; layer < layers; layer++)
            {
                _byMask[layer] = new Dictionary<int, List<int>>();
                _moves[layer] = new Dictionary<int, List<Move>>();
                double height = CompiledTileSet.RelativeHeight(layer, layers);
                for (int variant = 0; variant < _faces.Length; variant++)
                {
                    int mask = _faces[variant];
                    if (mask == 0 || set.WeightAt(variant, height) <= 0)
                        continue;
                    if (!_byMask[layer].TryGetValue(mask, out var list))
                        _byMask[layer][mask] = list = new List<int>();
                    list.Add(variant);
                    if (BitOperations.PopCount((uint)mask) != 2)
                        continue;
                    foreach (int entry in set.ActiveDirections)
                        foreach (int exit in set.ActiveDirections)
                        {
                            if (entry == exit || (mask & 1 << entry) == 0 || (mask & 1 << exit) == 0)
                                continue;
                            var socket = set.Variants[variant].Sockets[entry];
                            for (int incoming = 0; incoming < _sockets.Count; incoming++)
                            {
                                if (!_sockets[incoming].Matches(socket))
                                    continue;
                                int key = entry * _sockets.Count + incoming;
                                if (!_moves[layer].TryGetValue(key, out var moves))
                                    _moves[layer][key] = moves = new List<Move>();
                                var move = new Move(exit, SocketId(set.Variants[variant].Sockets[exit]), variant);
                                if (!moves.Exists(m => m.Exit == move.Exit && m.Outgoing == move.Outgoing))
                                    moves.Add(move);
                            }
                        }
                }
            }

            FindGroupsAndStubs();
            _pending = Enumerable.Repeat(true, _stubs.Count).ToArray();
        }

        private int SocketId(Socket socket)
        {
            int id = _sockets.IndexOf(socket);
            if (id < 0)
            {
                id = _sockets.Count;
                _sockets.Add(socket);
            }
            return id;
        }

        private Vector3I At(int cell) => _origin + _grid.Coordinates(cell);

        private int Layer(int cell) => _grid.Coordinates(cell).Y;

        private int FixedFaces(int cell) => _initial[cell] >= 0 ? _faces[_initial[cell]] : 0;

        private Socket SocketOf(int variant, int direction) => _set.Variants[variant].Sockets[direction];

        private void FindGroupsAndStubs()
        {
            var fixedPath = Enumerable.Range(0, _grid.CellCount).Where(c => FixedFaces(c) != 0).ToList();
            int groups = 0;
            foreach (int start in fixedPath)
            {
                if (_groupOf[start] >= 0)
                    continue;
                var stack = new Stack<int>();
                stack.Push(start);
                _groupOf[start] = groups;
                while (stack.Count > 0)
                {
                    int cell = stack.Pop();
                    foreach (int direction in _set.ActiveDirections)
                    {
                        if ((FixedFaces(cell) & (1 << direction)) == 0)
                            continue;
                        int neighbour = _grid.Neighbour(cell, direction);
                        if (neighbour >= 0 && _groupOf[neighbour] < 0 && (FixedFaces(neighbour) & (1 << Directions.Opposite(direction))) != 0)
                        {
                            _groupOf[neighbour] = groups;
                            stack.Push(neighbour);
                        }
                    }
                }
                groups++;
            }

            // A path face towards a free cell needs a route. Faces out of the bounds or towards
            // excluded cells lead elsewhere, and faces towards other placed cells are joined already.
            foreach (int cell in fixedPath)
                foreach (int direction in _set.ActiveDirections)
                {
                    if ((FixedFaces(cell) & (1 << direction)) == 0)
                        continue;
                    int neighbour = _grid.Neighbour(cell, direction);
                    if (neighbour >= 0 && _initial[neighbour] == WfcSolver.Free)
                        _stubs.Add(new Stub(_stubs.Count, cell, neighbour, Directions.Opposite(direction), _groupOf[cell],
                            SocketOf(_initial[cell], direction)));
                }
        }

        public PathPlan Run()
        {
            if (!_faces.Any(f => f != 0))
                return Failed(new PathError($"No tile has a face with the path socket '{_name}'.", Array.Empty<Vector3I>()));
            if (!_faces.Any(f => f == 0))
                return Failed(new PathError($"Every tile has a '{_name}' face, so nothing could be placed off the path.", Array.Empty<Vector3I>()));

            ConnectGroups();
            if (_errors.Count == 0)
                CloseOpenFaces();
            if (_errors.Count > 0)
                return new PathPlan { Errors = _errors };
            return BuildPlan();
        }

        /// <summary>Joins each group to the network built so far, nearest group first.</summary>
        private void ConnectGroups()
        {
            int groupCount = _groupOf.Max() + 1;
            if (groupCount <= 1)
                return;
            var connected = new bool[groupCount];
            connected[0] = true;

            for (int group = 0; group < groupCount; group++)
            {
                if (!_stubs.Any(s => s.Group == group))
                {
                    _errors.Add(new PathError($"The placed path at {At(FirstCellOf(group))} has no open path face, so it cannot be joined to the other paths.",
                        CellsOf(group)));
                }
            }
            if (_errors.Count > 0)
                return;

            for (int joined = 1; joined < groupCount; joined++)
            {
                var targets = NetworkTargets(connected);
                int next = -1;
                int nearest = int.MaxValue;
                for (int group = 0; group < groupCount; group++)
                {
                    if (connected[group])
                        continue;
                    foreach (var stub in _stubs.Where(s => s.Group == group && _pending[s.Index]))
                        foreach (int target in targets)
                        {
                            int distance = Manhattan(stub.EntryCell, target);
                            if (distance < nearest)
                            {
                                nearest = distance;
                                next = group;
                            }
                        }
                }
                if (next < 0)
                {
                    next = Array.IndexOf(connected, false);
                    _errors.Add(new PathError($"The placed path at {At(FirstCellOf(next))} cannot be joined: the other paths have no open path face left.", CellsOf(next)));
                    return;
                }

                var sources = _stubs.Where(s => s.Group == next && _pending[s.Index]).ToList();
                if (!Route(sources, stub => connected[stub.Group]))
                {
                    _errors.Add(new PathError($"No route joins the placed path at {At(FirstCellOf(next))} to the other paths "
                        + "through the path faces the tiles offer.", CellsOf(next)));
                    return;
                }
                connected[next] = true;
            }
        }

        /// <summary>Gives every remaining open path face its own route, to another open face or into a route.</summary>
        private void CloseOpenFaces()
        {
            foreach (var stub in _stubs)
            {
                if (!_pending[stub.Index])
                    continue;
                bool anyTarget = _stubs.Any(s => s.Index != stub.Index && _pending[s.Index]) || _routeCount > 0;
                if (!anyTarget || !Route(new List<Stub> { stub }, _ => true))
                {
                    var face = Directions.Opposite(stub.EntryFace);
                    _errors.Add(new PathError(anyTarget
                        ? $"The {Directions.Name(face)} path face of the placed cell at {At(stub.FixedCell)} cannot be connected through the path faces the tiles offer."
                        : $"The {Directions.Name(face)} path face of the placed cell at {At(stub.FixedCell)} leads nowhere. Place another path cell for it to reach.",
                        new[] { At(stub.FixedCell) }));
                }
            }
        }

        private List<int> NetworkTargets(bool[] connected)
        {
            var targets = _stubs.Where(s => _pending[s.Index] && connected[s.Group]).Select(s => s.EntryCell).ToList();
            for (int cell = 0; cell < _grid.CellCount; cell++)
                if (_routeFaces[cell] != 0)
                    targets.Add(cell);
            return targets;
        }

        /// <summary>A variant for the cell where a route ends: entered as given and also open to <paramref name="face"/>.</summary>
        private int EndVariant(int cell, int entry, Socket incoming, int face, Socket faceIncoming)
        {
            int mask = (1 << entry) | (1 << face);
            if (!_byMask[Layer(cell)].TryGetValue(mask, out var variants))
                return -1;
            return variants.FirstOrDefault(v => incoming.Matches(SocketOf(v, entry)) && faceIncoming.Matches(SocketOf(v, face)), -1);
        }

        /// <summary>
        /// A variant for a route cell that another route joins: its old path faces keep their
        /// sockets, so its route neighbours still fit, and the new face fits the arriving socket.
        /// </summary>
        private int JunctionVariant(int cell, int entry, Socket incoming)
        {
            int old = _routeVariant[cell];
            if (old < 0 || (_routeFaces[cell] & 1 << entry) != 0)
                return -1;
            int mask = _routeFaces[cell] | (1 << entry);
            if (!_byMask[Layer(cell)].TryGetValue(mask, out var variants))
                return -1;
            return variants.FirstOrDefault(v => incoming.Matches(SocketOf(v, entry))
                && _set.ActiveDirections.All(d => (_routeFaces[cell] & 1 << d) == 0 || SocketOf(v, d) == SocketOf(old, d)), -1);
        }

        /// <summary>
        /// Cheapest route from one of <paramref name="sources"/> to a pending stub accepted by
        /// <paramref name="targetStub"/> or into an existing route. Applies it and returns true when found.
        /// A state is a cell, the face the route enters it by, and the socket arriving there.
        /// </summary>
        private bool Route(List<Stub> sources, Func<Stub, bool> targetStub)
        {
            var entryOf = new Dictionary<int, List<Stub>>();
            foreach (var stub in _stubs.Where(s => _pending[s.Index]))
            {
                if (!entryOf.TryGetValue(stub.EntryCell, out var list))
                    entryOf[stub.EntryCell] = list = new List<Stub>();
                list.Add(stub);
            }

            // A* towards the box around every possible end. Each step costs at least 1, so the
            // distance to that box never overestimates and the first end reached is the cheapest.
            // Ties go to the state that came furthest, which avoids exploring whole plateaus.
            var min = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            var max = new Vector3I(int.MinValue, int.MinValue, int.MinValue);
            bool anyTarget = false;
            void Include(int cell)
            {
                var at = _grid.Coordinates(cell);
                min = new Vector3I(Math.Min(min.X, at.X), Math.Min(min.Y, at.Y), Math.Min(min.Z, at.Z));
                max = new Vector3I(Math.Max(max.X, at.X), Math.Max(max.Y, at.Y), Math.Max(max.Z, at.Z));
                anyTarget = true;
            }
            foreach (var stub in _stubs)
                if (_pending[stub.Index] && targetStub(stub) && !sources.Contains(stub))
                    Include(stub.EntryCell);
            for (int cell = 0; cell < _grid.CellCount; cell++)
                if (_routeFaces[cell] != 0)
                    Include(cell);
            if (!anyTarget)
                return false;
            double Estimate(int cell)
            {
                var at = _grid.Coordinates(cell);
                return Math.Max(0, Math.Max(min.X - at.X, at.X - max.X)) + Math.Max(0, Math.Max(min.Y - at.Y, at.Y - max.Y))
                    + Math.Max(0, Math.Max(min.Z - at.Z, at.Z - max.Z));
            }

            int socketCount = _sockets.Count;
            int Encode(int cell, int entry, int incoming) => (cell * Directions.Count + entry) * socketCount + incoming;
            var nodes = new Dictionary<int, Node>();
            var queue = new PriorityQueue<int, (double, double, long)>();
            long order = 0;
            foreach (var source in sources)
            {
                int state = Encode(source.EntryCell, source.EntryFace, SocketId(source.Incoming));
                double cost = CellCost(source.EntryCell);
                if (!nodes.TryGetValue(state, out var node) || cost < node.Cost)
                {
                    nodes[state] = new Node { Cost = cost, Previous = -1, Source = source.Index, PreviousVariant = -1 };
                    queue.Enqueue(state, (cost + Estimate(source.EntryCell), -cost, order++));
                }
            }

            while (queue.TryDequeue(out int state, out var priority))
            {
                var current = nodes[state];
                int cell = state / socketCount / Directions.Count;
                int entry = state / socketCount % Directions.Count;
                int incomingId = state % socketCount;
                var incoming = _sockets[incomingId];
                if (priority.Item1 > current.Cost + Estimate(cell) + 1e-9)
                    continue;
                var source = _stubs[current.Source];

                // Ending here: into a route, or into another open face that enters this cell.
                if (_routeFaces[cell] != 0)
                {
                    int junction = JunctionVariant(cell, entry, incoming);
                    if (junction >= 0)
                    {
                        Apply(state, nodes, junction, source, null);
                        return true;
                    }
                    continue;
                }
                if (entryOf.TryGetValue(cell, out var waiting))
                {
                    foreach (var target in waiting)
                    {
                        if (target.Index == source.Index || target.EntryFace == entry || !targetStub(target))
                            continue;
                        int end = EndVariant(cell, entry, incoming, target.EntryFace, target.Incoming);
                        if (end >= 0)
                        {
                            Apply(state, nodes, end, source, target);
                            return true;
                        }
                    }
                    if (waiting.Any(t => t.Index != source.Index))
                        continue;   // another open face's cell: never pass through it
                }

                if (!_moves[Layer(cell)].TryGetValue(entry * socketCount + incomingId, out var moves))
                    continue;
                foreach (var move in moves)
                {
                    int neighbour = _grid.Neighbour(cell, move.Exit);
                    if (neighbour < 0 || _initial[neighbour] != WfcSolver.Free)
                        continue;
                    int next = Encode(neighbour, Directions.Opposite(move.Exit), move.Outgoing);
                    double cost = current.Cost + CellCost(neighbour);
                    if (!nodes.TryGetValue(next, out var node) || cost < node.Cost)
                    {
                        nodes[next] = new Node { Cost = cost, Previous = state, Source = current.Source, PreviousVariant = move.Variant };
                        queue.Enqueue(next, (cost + Estimate(neighbour), -cost, order++));
                    }
                }
            }
            return false;
        }

        private void Apply(int last, Dictionary<int, Node> nodes, int lastVariant, Stub source, Stub target)
        {
            int socketCount = _sockets.Count;
            int state = last;
            int variant = lastVariant;
            if (target != null)
            {
                _routeFaces[state / socketCount / Directions.Count] |= 1 << target.EntryFace;
                _pending[target.Index] = false;
            }
            while (state >= 0)
            {
                int cell = state / socketCount / Directions.Count;
                int entry = state / socketCount % Directions.Count;
                _routeFaces[cell] |= 1 << entry;
                _routeVariant[cell] = variant;
                var node = nodes[state];
                if (node.Previous >= 0)
                    _routeFaces[node.Previous / socketCount / Directions.Count] |= 1 << Directions.Opposite(entry);
                variant = node.PreviousVariant;
                state = node.Previous;
            }
            _pending[source.Index] = false;
            _routeCount++;
        }

        /// <summary>
        /// Route cells may use the variants with the same path faces and the same sockets on them as
        /// the one the route chose; every other free cell may use no path variant.
        /// </summary>
        private PathPlan BuildPlan()
        {
            var classOf = new Dictionary<string, int>();
            var classes = new List<IReadOnlyList<int>>();
            int ClassFor(string key, Func<IEnumerable<int>> variants)
            {
                if (!classOf.TryGetValue(key, out int index))
                {
                    index = classes.Count;
                    classOf[key] = index;
                    classes.Add(variants().ToArray());
                }
                return index;
            }

            var classOfCell = new int[_grid.CellCount];
            int routeCells = 0;
            for (int cell = 0; cell < _grid.CellCount; cell++)
            {
                if (_initial[cell] != WfcSolver.Free)
                {
                    classOfCell[cell] = CellRestrictions.None;
                    continue;
                }
                int mask = _routeFaces[cell];
                if (mask == 0)
                {
                    classOfCell[cell] = ClassFor("", () => Enumerable.Range(0, _faces.Length).Where(v => _faces[v] == 0));
                    continue;
                }
                routeCells++;
                int chosen = _routeVariant[cell];
                var faces = _set.ActiveDirections.Where(d => (mask & 1 << d) != 0).ToArray();
                string key = mask + ":" + string.Join(",", faces.Select(d => SocketOf(chosen, d)));
                classOfCell[cell] = ClassFor(key, () => Enumerable.Range(0, _faces.Length)
                    .Where(v => _faces[v] == mask && faces.All(d => SocketOf(v, d) == SocketOf(chosen, d))));
            }

            return new PathPlan
            {
                Restrictions = new CellRestrictions(classOfCell, classes),
                RouteFaces = _routeFaces,
                RouteCount = _routeCount,
                RouteCellCount = routeCells,
                PlacedPathCells = Enumerable.Range(0, _grid.CellCount).Count(c => FixedFaces(c) != 0),
            };
        }

        // A tiny share of noise even at wander 0 breaks ties between equally short routes by seed,
        // without making a longer route cheaper (routes stay far below 1 / TieNoise cells).
        private const double TieNoise = 1e-6;

        private double CellCost(int cell) => 1 + (TieNoise + _wander * WanderCostRange) * Noise(cell);

        /// <summary>A fixed pseudo-random number in [0, 1) per seed and cell.</summary>
        private double Noise(int cell)
        {
            ulong x = unchecked((ulong)_seed * 0x9E3779B97F4A7C15UL + (ulong)cell * 0xBF58476D1CE4E5B9UL + 0x94D049BB133111EBUL);
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            x ^= x >> 31;
            return (x >> 11) * (1.0 / (1UL << 53));
        }

        private int Manhattan(int a, int b)
        {
            var d = _grid.Coordinates(a) - _grid.Coordinates(b);
            return Math.Abs(d.X) + Math.Abs(d.Y) + Math.Abs(d.Z);
        }

        private int FirstCellOf(int group) => Array.IndexOf(_groupOf, group);

        private IReadOnlyList<Vector3I> CellsOf(int group) =>
            Enumerable.Range(0, _grid.CellCount).Where(c => _groupOf[c] == group).Select(At).ToArray();
    }
}
