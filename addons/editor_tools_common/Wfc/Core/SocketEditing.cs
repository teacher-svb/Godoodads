using System;
using System.Collections.Generic;
using System.Linq;

namespace Godoodads.Common.Wfc.Core;

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
        if (!Directions.IsVertical(direction) || !SocketSet.TryParse(label, true, out var socket, out _))
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
                if (Directions.IsVertical(direction) != vertical || !SocketSet.TryParse(tile.Sockets[direction], vertical, out var set, out _))
                    continue;
                foreach (var socket in set.Labels)
                {
                    labels.Add(socket.ToString());
                    if (socket.Kind == SocketKind.HorizontalPlus)
                        labels.Add(socket.Name + "-");
                    else if (socket.Kind == SocketKind.HorizontalMinus)
                        labels.Add(socket.Name + "+");
                }
            }
        }
        filter = filter?.Trim() ?? "";
        return labels.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>The entry after the last comma of <paramref name="text"/>, trimmed: the label being typed.</summary>
    public static string EntryBeingTyped(string text)
    {
        text ??= "";
        return text[(text.LastIndexOf(SocketSet.Separator) + 1)..].Trim();
    }

    /// <summary>
    /// <paramref name="text"/> with the entry being typed replaced by <paramref name="suggestion"/>.
    /// Entries are joined with ", ", and a label that is already listed is not added again.
    /// </summary>
    public static string PickSuggestion(string text, string suggestion)
    {
        var entries = (text ?? "").Split(SocketSet.Separator).Select(e => e.Trim()).ToList();
        entries.RemoveAt(entries.Count - 1);
        entries.RemoveAll(e => e.Length == 0);
        if (!entries.Contains(suggestion))
            entries.Add(suggestion);
        return string.Join(SocketSet.Separator + " ", entries);
    }

    /// <summary>
    /// True for each direction in which no variant, and no external socket (see
    /// <see cref="TileSetChecker.Check"/>), can face <paramref name="variant"/>.
    /// </summary>
    public static bool[] UnmatchedFaces(CompiledTileSet set, int variant, IReadOnlyCollection<Socket> external = null)
    {
        var unmatched = new bool[Directions.Count];
        foreach (int direction in set.ActiveDirections)
        {
            bool any = TileSetChecker.MeetsExternal(set.Variants[variant].Sockets[direction], external);
            for (int other = 0; other < set.Variants.Count && !any; other++)
                any = set.Allows(variant, direction, other);
            unmatched[direction] = !any;
        }
        return unmatched;
    }
}
