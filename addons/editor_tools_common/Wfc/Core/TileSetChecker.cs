using System.Collections.Generic;
using System.Linq;

namespace Godoodads.Common.Wfc.Core;

/// <summary>A tile face whose socket has no partner in any rotation of any tile.</summary>
public sealed record UnmatchedFace(int TileIndex, int Direction, string Socket);

/// <summary>Warnings about a valid tile set. Tiles that fail here can still appear at the grid's edges.</summary>
public sealed record TileSetReport(IReadOnlyList<UnmatchedFace> UnmatchedFaces, IReadOnlyList<int> EdgeOnlyTiles)
{
    public bool IsClean => UnmatchedFaces.Count == 0 && EdgeOnlyTiles.Count == 0;
}

public static class TileSetChecker
{
    /// <param name="external">
    /// Sockets supplied from outside the tiles (a tile set's External Sockets, such as a boundary
    /// socket): a face that one of them can meet is not unmatched. Null or empty means none.
    /// </param>
    public static TileSetReport Check(CompiledTileSet tiles, IReadOnlyCollection<Socket> external = null)
    {
        int count = tiles.Variants.Count;
        var unmatched = FindUnmatchedFaces(tiles, external);
        var alive = RemoveVariantsWithoutNeighbours(tiles, count, external);

        var withUnmatchedFace = unmatched.Select(f => f.TileIndex).ToHashSet();
        var aliveTiles = tiles.Variants.Where((_, v) => alive[v]).Select(v => v.TileIndex).ToHashSet();
        var edgeOnly = tiles.Variants.Select(v => v.TileIndex).Distinct()
            .Where(t => !aliveTiles.Contains(t) && !withUnmatchedFace.Contains(t))
            .ToList();
        return new TileSetReport(unmatched, edgeOnly);
    }

    /// <summary>True when an external socket can face <paramref name="socket"/>.</summary>
    public static bool MeetsExternal(SocketSet socket, IReadOnlyCollection<Socket> external) =>
        external != null && external.Any(e => socket.Matches(e));

    private static List<UnmatchedFace> FindUnmatchedFaces(CompiledTileSet tiles, IReadOnlyCollection<Socket> external)
    {
        var faces = new SortedDictionary<(int Tile, int Direction), (bool Matched, string Socket)>();
        for (int v = 0; v < tiles.Variants.Count; v++)
        {
            var variant = tiles.Variants[v];
            foreach (int direction in tiles.ActiveDirections)
            {
                int local = Directions.RotateY(direction, -variant.QuarterTurns);
                bool matched = MeetsExternal(variant.Sockets[direction], external)
                    || Enumerable.Range(0, tiles.Variants.Count).Any(u => tiles.Allows(v, direction, u));
                faces.TryGetValue((variant.TileIndex, local), out var previous);
                string socket = variant.QuarterTurns == 0 ? variant.Sockets[direction].ToString() : previous.Socket ?? variant.Sockets[direction].ToString();
                faces[(variant.TileIndex, local)] = (previous.Matched || matched, socket);
            }
        }
        return faces.Where(f => !f.Value.Matched)
            .Select(f => new UnmatchedFace(f.Key.Tile, f.Key.Direction, f.Value.Socket))
            .ToList();
    }

    /// <summary>Repeatedly removes variants that have no remaining neighbour in some direction.</summary>
    private static bool[] RemoveVariantsWithoutNeighbours(CompiledTileSet tiles, int count, IReadOnlyCollection<Socket> external)
    {
        var alive = Enumerable.Repeat(true, count).ToArray();
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int v = 0; v < count; v++)
            {
                if (!alive[v])
                    continue;
                foreach (int direction in tiles.ActiveDirections)
                {
                    bool supported = MeetsExternal(tiles.Variants[v].Sockets[direction], external);
                    for (int u = 0; u < count && !supported; u++)
                        supported = alive[u] && tiles.Allows(v, direction, u);
                    if (!supported)
                    {
                        alive[v] = false;
                        changed = true;
                        break;
                    }
                }
            }
        }
        return alive;
    }
}
