using System.Collections.Generic;

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
/// </summary>
public sealed record TileDefinition(string[] Sockets, TileRotation Rotation = TileRotation.None, double Weight = 1);

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

    public IReadOnlyList<TileVariant> Variants => _variants;

    /// <summary>Per-variant choice weight: the tile's weight divided by its number of variants.</summary>
    public IReadOnlyList<double> VariantWeights => _weights;
    public IReadOnlyList<TileIssue> Errors => _errors;
    public bool IsValid => _errors.Count == 0;
    public int[] ActiveDirections { get; }

    /// <summary>Number of 64-bit words in one variant bitset.</summary>
    public int WordCount { get; private set; }

    /// <summary>Flat compatibility bitsets, addressed through <see cref="CompatibleOffset"/>.</summary>
    internal ulong[] Compatible { get; private set; }

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
        else if (set._errors.Count == 0 && !set._weights.Exists(w => w > 0))
            set._errors.Add(new TileIssue(-1, -1, "Every tile has weight 0, so nothing can be generated."));
        if (set.IsValid)
            set.BuildAdjacency();
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
            _weights.Add(tile.Weight / added);
    }

    private void BuildAdjacency()
    {
        int count = _variants.Count;
        WordCount = (count + 63) / 64;
        Compatible = new ulong[count * Directions.Count * WordCount];
        Generatable = new ulong[WordCount];
        for (int v = 0; v < count; v++)
            if (_weights[v] > 0)
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
    }
}
