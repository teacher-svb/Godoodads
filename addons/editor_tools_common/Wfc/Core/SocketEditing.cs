using System;
using System.Collections.Generic;
using System.Linq;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// Helpers for editing sockets in the tile set preview (brief §13.1): which face of the tile an
/// edit on a rotated variant belongs to, label suggestions, and faces nothing can face.
/// </summary>
public static class SocketEditing
{
    /// <summary>The tile's own face shown in <paramref name="direction"/> by its variant turned <paramref name="quarterTurns"/>.</summary>
    public static int LocalFace(int direction, int quarterTurns) => Directions.RotateY(direction, -quarterTurns);

    /// <summary>
    /// The label to store on the tile's own face when <paramref name="label"/> is typed on a variant
    /// turned <paramref name="quarterTurns"/>. Only rotation-specific vertical labels change. A label
    /// that does not parse is stored as typed, so the tile set check reports it.
    /// </summary>
    public static string ToLocalLabel(string label, int direction, int quarterTurns)
    {
        label = label?.Trim() ?? "";
        if (!Directions.IsVertical(direction) || !Socket.TryParse(label, true, out var socket, out _))
            return label;
        return socket.RotatedY(-quarterTurns).ToString();
    }

    /// <summary>
    /// Labels used on faces of the same kind (horizontal or vertical), plus the partner of every
    /// one-sided horizontal label, containing <paramref name="filter"/> (case-insensitive) and sorted.
    /// </summary>
    public static List<string> Suggestions(IEnumerable<TileDefinition> tiles, bool vertical, string filter = "")
    {
        var labels = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var tile in tiles)
        {
            if (tile?.Sockets == null)
                continue;
            for (int direction = 0; direction < Directions.Count && direction < tile.Sockets.Length; direction++)
            {
                if (Directions.IsVertical(direction) != vertical || !Socket.TryParse(tile.Sockets[direction], vertical, out var socket, out _))
                    continue;
                labels.Add(socket.ToString());
                if (socket.Kind == SocketKind.HorizontalPlus)
                    labels.Add(socket.Name + "-");
                else if (socket.Kind == SocketKind.HorizontalMinus)
                    labels.Add(socket.Name + "+");
            }
        }
        filter = filter?.Trim() ?? "";
        return labels.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>True for each direction in which no variant can face <paramref name="variant"/>.</summary>
    public static bool[] UnmatchedFaces(CompiledTileSet set, int variant)
    {
        var unmatched = new bool[Directions.Count];
        foreach (int direction in set.ActiveDirections)
        {
            bool any = false;
            for (int other = 0; other < set.Variants.Count && !any; other++)
                any = set.Allows(variant, direction, other);
            unmatched[direction] = !any;
        }
        return unmatched;
    }
}
