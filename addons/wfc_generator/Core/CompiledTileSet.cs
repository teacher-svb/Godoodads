using System;
using System.Collections.Generic;
using System.Linq;

namespace Godoodads.WfcGenerator.Core;

public enum TileRotation
{
    None,
    TwoWay,
    FourWay,
}

/// <summary>
/// A tile as the author defines it. Sockets are indexed by <see cref="Directions"/>. The weight is
/// shared by the tile's rotations; 0 means the solver never chooses it, but it may be fixed.
/// <paramref name="HeightProfile"/> optionally multiplies the weight by height: evenly spaced
/// samples from the bottom layer of the grid (first) to the top layer (last), interpolated linearly.
/// </summary>
public sealed record TileDefinition(string[] Sockets, TileRotation Rotation = TileRotation.None, double Weight = 1,
    IReadOnlyList<double> HeightProfile = null);

/// <summary>One placeable rotation of a tile. Sockets are in world directions.</summary>
public sealed record TileVariant(int TileIndex, int QuarterTurns, Socket[] Sockets);

/// <summary>A problem with a tile definition. Direction is -1 when it applies to the whole tile set.</summary>
public sealed record TileIssue(int TileIndex, int Direction, string Message);

/// <summary>Variants generated from tile definitions, with the adjacency table between them.</summary>
public sealed class CompiledTileSet
{
    private readonly Dictionary<(int Tile, int Turns), int> _lookup = new();
    private readonly List<TileVariant> _variants = new();
    private readonly List<TileIssue> _errors = new();
    private readonly List<double> _weights = new();
    private readonly List<IReadOnlyList<double>> _profiles = new();

    public IReadOnlyList<TileVariant> Variants => _variants;

    /// <summary>Per-variant choice weight: the tile's weight divided by its number of variants.</summary>
    public IReadOnlyList<double> VariantWeights => _weights;
    public IReadOnlyList<TileIssue> Errors => _errors;
    public bool IsValid => _errors.Count == 0;
    public int[] ActiveDirections { get; }

    /// <summary>Number of 64-bit words in one variant bitset.</summary>
    public int WordCount { get; private set; }

    /// <summary>True when any tile has a height profile, so weights differ per layer.</summary>
    public bool HasHeightProfiles => _profiles.Exists(p => p != null);

    /// <summary>
    /// A variant's weight at relative height <paramref name="height"/>: 0 is the bottom layer of
    /// the grid, 1 the top layer.
    /// </summary>
    public double WeightAt(int variant, double height)
    {
        var profile = _profiles[variant];
        if (profile == null)
            return _weights[variant];
        double position = Math.Clamp(height, 0, 1) * (profile.Count - 1);
        int below = (int)Math.Floor(position);
        int above = Math.Min(below + 1, profile.Count - 1);
        double multiplier = profile[below] + (profile[above] - profile[below]) * (position - below);
        return _weights[variant] * multiplier;
    }

    /// <summary>The relative height of a layer in a grid with <paramref name="layers"/> layers.</summary>
    public static double RelativeHeight(int layer, int layers) => layers <= 1 ? 0 : layer / (double)(layers - 1);

    /// <summary>Layers of a grid with this many layers where no variant has weight above 0.</summary>
    public List<int> LayersWithoutWeight(int layers)
    {
        var empty = new List<int>();
        for (int layer = 0; layer < layers; layer++)
        {
            double height = RelativeHeight(layer, layers);
            bool any = false;
            for (int v = 0; v < _variants.Count && !any; v++)
                any = WeightAt(v, height) > 0;
            if (!any)
                empty.Add(layer);
        }
        return empty;
    }

    /// <summary>Flat compatibility bitsets, addressed through <see cref="CompatibleOffset"/>.</summary>
    internal ulong[] Compatible { get; private set; }

    // Per direction, variants whose compatibility rows are identical form a class. Propagation ORs
    // one row per class present in a domain instead of one per variant. Addressed through ClassOffset.
    internal int[] ClassStart { get; private set; }     // first class of each direction, plus an end marker
    internal ulong[] ClassMembers { get; private set; }  // variants in each class
    internal ulong[] ClassRows { get; private set; }     // the shared compatibility row of each class

    internal int ClassOffset(int classIndex) => classIndex * WordCount;

    /// <summary>Bitset of the variants with a weight above zero: the starting options of a free cell.</summary>
    internal ulong[] Generatable { get; private set; }

    private CompiledTileSet(int[] activeDirections) => ActiveDirections = activeDirections;

    public static CompiledTileSet Compile(IReadOnlyList<TileDefinition> tiles, int[] activeDirections)
    {
        var set = new CompiledTileSet(activeDirections);
        for (int i = 0; i < tiles.Count; i++)
            set.AddTile(i, tiles[i]);
        if (set._variants.Count == 0 && set._errors.Count == 0)
            set._errors.Add(new TileIssue(-1, -1, "The tile set has no tiles."));
        if (set.IsValid)
        {
            set.BuildAdjacency();
            if (set.Generatable.All(word => word == 0))
                set._errors.Add(new TileIssue(-1, -1, "Every tile has weight 0, so nothing can be generated."));
        }
        return set;
    }

    /// <summary>The variant for a tile at a rotation, or -1 when that rotation is not available.</summary>
    public int FindVariant(int tileIndex, int quarterTurns) =>
        _lookup.TryGetValue((tileIndex, ((quarterTurns % 4) + 4) % 4), out int variant) ? variant : -1;

    /// <summary>True when <paramref name="other"/> may sit next to <paramref name="variant"/> in <paramref name="direction"/>.</summary>
    public bool Allows(int variant, int direction, int other) =>
        (Compatible[CompatibleOffset(variant, direction) + (other >> 6)] & (1UL << other)) != 0;

    internal int CompatibleOffset(int variant, int direction) => (variant * Directions.Count + direction) * WordCount;

    private void AddTile(int tileIndex, TileDefinition tile)
    {
        var parsed = new Socket[Directions.Count];
        bool valid = true;
        if (!(tile.Weight >= 0) || double.IsInfinity(tile.Weight))
        {
            _errors.Add(new TileIssue(tileIndex, -1, $"Weight {tile.Weight} is not allowed; use 0 or more."));
            valid = false;
        }
        if (tile.HeightProfile != null && (tile.HeightProfile.Count == 0
            || tile.HeightProfile.Any(m => !(m >= 0) || double.IsInfinity(m))))
        {
            _errors.Add(new TileIssue(tileIndex, -1, "The height curve has values below 0; use 0 or more."));
            valid = false;
        }
        foreach (int direction in ActiveDirections)
        {
            string label = tile.Sockets != null && tile.Sockets.Length == Directions.Count ? tile.Sockets[direction] : null;
            if (!Socket.TryParse(label, Directions.IsVertical(direction), out parsed[direction], out string error))
            {
                _errors.Add(new TileIssue(tileIndex, direction, error));
                valid = false;
            }
        }
        if (!valid)
            return;

        int step = tile.Rotation switch { TileRotation.FourWay => 1, TileRotation.TwoWay => 2, _ => 4 };
        int first = _variants.Count;
        var seen = new Dictionary<string, int>();
        for (int turns = 0; turns < 4; turns += step)
        {
            var world = new Socket[Directions.Count];
            var key = new List<string>();
            foreach (int direction in ActiveDirections)
                world[Directions.RotateY(direction, turns)] = parsed[direction].RotatedY(turns);
            foreach (int direction in ActiveDirections)
                key.Add(world[direction].ToString());

            string identity = string.Join("|", key);
            if (!seen.TryGetValue(identity, out int variant))
            {
                variant = _variants.Count;
                seen[identity] = variant;
                _variants.Add(new TileVariant(tileIndex, turns, world));
            }
            _lookup[(tileIndex, turns)] = variant;
        }

        // Identical rotations were merged, so the weight is split over the variants that remain.
        int added = _variants.Count - first;
        for (int i = 0; i < added; i++)
        {
            _weights.Add(tile.Weight / added);
            _profiles.Add(tile.HeightProfile);
        }
    }

    private void BuildAdjacency()
    {
        int count = _variants.Count;
        WordCount = (count + 63) / 64;
        Compatible = new ulong[count * Directions.Count * WordCount];
        Generatable = new ulong[WordCount];
        for (int v = 0; v < count; v++)
            if (_weights[v] > 0 && (_profiles[v] == null || _profiles[v].Any(m => m > 0)))
                Generatable[v >> 6] |= 1UL << v;
        for (int a = 0; a < count; a++)
        {
            foreach (int direction in ActiveDirections)
            {
                int offset = CompatibleOffset(a, direction);
                var socket = _variants[a].Sockets[direction];
                for (int b = 0; b < count; b++)
                    if (socket.Matches(_variants[b].Sockets[Directions.Opposite(direction)]))
                        Compatible[offset + (b >> 6)] |= 1UL << b;
            }
        }
        BuildClasses();
    }

    private void BuildClasses()
    {
        int count = _variants.Count;
        var starts = new List<int>();
        var members = new List<ulong>();
        var rows = new List<ulong>();
        for (int direction = 0; direction < Directions.Count; direction++)
        {
            starts.Add(rows.Count / WordCount);
            var classOf = new Dictionary<string, int>();
            for (int v = 0; v < count; v++)
            {
                int offset = CompatibleOffset(v, direction);
                string key = string.Join(",", Compatible.AsSpan(offset, WordCount).ToArray());
                if (!classOf.TryGetValue(key, out int c))
                {
                    c = rows.Count / WordCount;
                    classOf[key] = c;
                    for (int w = 0; w < WordCount; w++)
                    {
                        rows.Add(Compatible[offset + w]);
                        members.Add(0);
                    }
                }
                members[c * WordCount + (v >> 6)] |= 1UL << v;
            }
        }
        starts.Add(rows.Count / WordCount);
        ClassStart = starts.ToArray();
        ClassMembers = members.ToArray();
        ClassRows = rows.ToArray();
    }
}
