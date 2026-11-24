using System.Collections.Generic;
using System.Linq;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// The tile set rules of brief godoodungeon §4.2, checked on compiled tile sets, and the lookups the
/// solve stage needs: the door variant for each side, the corridor straight for each axis and the
/// corridor variant without path faces.
/// </summary>
public static class DungeonTileRules
{
    public static readonly int[] HorizontalDirections = { Directions.PosX, Directions.NegX, Directions.PosZ, Directions.NegZ };

    /// <summary>
    /// Checks the room tile set (edge socket, door tile) and the corridor tile set (path sockets,
    /// doorway sockets, straights, empty variant). Errors make generation impossible; warnings do not.
    /// <paramref name="pathSocket"/> and <paramref name="doorwaySocket"/> are comma-separated name
    /// lists (brief §14.1); an empty doorway list lets any path socket face a door.
    /// </summary>
    /// <param name="roomNames">Readable tile names of the room tile set, for messages.</param>
    public static (List<string> Errors, List<string> Warnings) Check(CompiledTileSet room, IReadOnlyList<string> roomNames, int doorTile,
        string edgeSocket, CompiledTileSet corridor, string pathSocket, string doorwaySocket = "")
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        string edge = edgeSocket?.Trim() ?? "";
        var paths = SocketNames(pathSocket);
        var doorways = SocketNames(doorwaySocket);
        string path = Quoted(paths);

        // Room tile set: a one-sided edge socket.
        if (edge.Length == 0)
            errors.Add("Room Edge Socket is empty. Name the socket of the faces that point out of a room, such as 'outside'.");
        else
        {
            var edgeFaces = HorizontalFaces(room).Where(f => f.Socket.Name == edge).ToList();
            if (edgeFaces.Any(f => f.Socket.Kind == SocketKind.HorizontalSymmetric))
                errors.Add($"Room Edge Socket '{edge}' must be one-sided: tiles face out of a room with '{edge}+', and the room bounds answer with '{edge}-'. "
                    + $"With a plain '{edge}', two walls can meet inside a room. Used plain by: {Names(roomNames, edgeFaces.Where(f => f.Socket.Kind == SocketKind.HorizontalSymmetric))}.");
            if (edgeFaces.Any(f => f.Socket.Kind == SocketKind.HorizontalMinus))
                errors.Add($"Tiles use '{edge}-', which only the room bounds may use. Faces that point out of a room use '{edge}+'. "
                    + $"Used by: {Names(roomNames, edgeFaces.Where(f => f.Socket.Kind == SocketKind.HorizontalMinus))}.");
            if (!edgeFaces.Any(f => f.Socket.Kind == SocketKind.HorizontalPlus))
                errors.Add($"No room tile has a face '{edge}+', so nothing can stand at the edge of a room. Give the outer faces of walls, corners and doors '{edge}+'.");
        }

        // Door tile: exactly one horizontal face with the edge socket.
        if (doorTile < 0 || doorTile >= roomNames.Count)
            errors.Add("Choose the Door tile from the room tile set.");
        else if (edge.Length > 0)
        {
            int variant = room.FindVariant(doorTile, 0);
            int outward = variant < 0 ? 0 : HorizontalDirections.Count(d => IsEdgeOut(room.Variants[variant].Sockets[d], edge));
            if (outward != 1)
                errors.Add($"The Door tile '{roomNames[doorTile]}' needs exactly one horizontal face with '{edge}+' (its outside), but has {outward}.");
            else if (variant >= 0 && room.VariantWeights[variant] > 0)
                warnings.Add($"The Door tile '{roomNames[doorTile]}' has a weight above 0, so the solver may also put doors in walls by itself. Weight 0 leaves doors to the layout.");
        }

        // Corridor tile set: path sockets, straights both ways, and an empty variant.
        var usedNames = HorizontalFaces(corridor).Select(f => f.Socket.Name).ToHashSet();
        if (paths.Length == 0)
            errors.Add("Path Socket is empty. Name the socket of the corridor tiles' open faces, such as 'path', or several separated by commas.");
        else
        {
            var unusedPaths = paths.Where(n => !usedNames.Contains(n)).ToArray();
            if (unusedPaths.Length == paths.Length)
                errors.Add($"No corridor tile has a face {path}. Check Path Socket against the corridor tile set.");
            else
            {
                if (unusedPaths.Length > 0)
                    warnings.Add($"No corridor tile has a face {Quoted(unusedPaths)}, a name in Path Socket.");
                if (StraightVariant(corridor, paths, alongX: true) < 0)
                    errors.Add($"The corridor tile set has no straight piece along X: a tile with {path} on exactly +X and -X (or a turnable one). Doors on the X sides of rooms need it.");
                if (StraightVariant(corridor, paths, alongX: false) < 0)
                    errors.Add($"The corridor tile set has no straight piece along Z: a tile with {path} on exactly +Z and -Z. Give the straight piece four rotations.");
                if (!HasBends(corridor, paths))
                    errors.Add($"The corridor tile set has no bend in every rotation: a tile with {path} on two sides at a right angle (with four rotations). Corridors turn with it.");
                if (!HasJunction(corridor, paths))
                    errors.Add($"The corridor tile set has no T piece: a tile with {path} on three horizontal sides (with four rotations). "
                        + "Corridors that meet join with T pieces; without them only two doors can be joined.");
                if (!HasCross(corridor, paths))
                    errors.Add($"The corridor tile set has no cross: a tile with {path} on all four horizontal sides. Corridors that cross need it.");
            }
            int empty = EmptyVariant(corridor, paths);
            if (empty < 0)
                errors.Add($"The corridor tile set has no tile without {path} faces and with a weight above 0, so the space between corridors cannot be filled. Add an empty tile. "
                    + "Corridors are one layer high, so a height curve counts at its bottom.");

            // Doorway sockets: a subset of the path sockets that some piece turns towards every side.
            var notPaths = doorways.Where(n => !paths.Contains(n)).ToArray();
            if (notPaths.Length > 0)
                errors.Add($"Doorway Socket {Quoted(notPaths)} is not in Path Socket. The face towards a door is a corridor opening, so name path sockets, such as '{paths[0]}'.");
            else if (doorways.Length > 0)
            {
                var unusedDoorways = doorways.Where(n => !usedNames.Contains(n)).ToArray();
                if (unusedDoorways.Length == doorways.Length)
                    errors.Add($"No corridor tile has a face {Quoted(doorways)}, so no corridor can reach a door. Check Doorway Socket against the corridor tile set.");
                else
                {
                    if (unusedDoorways.Length > 0)
                        warnings.Add($"No corridor tile has a face {Quoted(unusedDoorways)}, a name in Doorway Socket.");
                    var missing = HorizontalDirections.Where(d => !corridor.Variants.Any(v => HasAnyName(v.Sockets[d], doorways))).ToList();
                    if (missing.Count > 0)
                        errors.Add($"No corridor tile has {Quoted(doorways)} on {string.Join(" or ", missing.Select(Directions.Name))}, so corridors cannot reach doors from that side. "
                            + "Give the pieces with doorway faces four rotations.");
                }
            }
        }
        return (errors, warnings);
    }

    /// <summary>
    /// The door tile's variant whose only edge face points out through <paramref name="outward"/> (a
    /// horizontal direction), or -1. A tile with more than one edge face is never a door.
    /// </summary>
    public static int DoorVariant(CompiledTileSet room, int doorTile, string edgeSocket, int outward)
    {
        string edge = edgeSocket.Trim();
        for (int turns = 0; turns < 4; turns++)
        {
            int variant = room.FindVariant(doorTile, turns);
            if (variant < 0)
                continue;
            var sockets = room.Variants[variant].Sockets;
            if (IsEdgeOut(sockets[outward], edge) && HorizontalDirections.Count(d => IsEdgeOut(sockets[d], edge)) == 1)
                return variant;
        }
        return -1;
    }

    /// <summary>A corridor variant with path faces on exactly the two sides of one axis, or -1.</summary>
    public static int StraightVariant(CompiledTileSet corridor, string pathSocket, bool alongX) =>
        StraightVariant(corridor, SocketNames(pathSocket), alongX);

    /// <inheritdoc cref="StraightVariant(CompiledTileSet, string, bool)"/>
    public static int StraightVariant(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets, bool alongX)
    {
        int wanted = alongX ? (1 << Directions.PosX) | (1 << Directions.NegX) : (1 << Directions.PosZ) | (1 << Directions.NegZ);
        for (int variant = 0; variant < corridor.Variants.Count; variant++)
            if (PathFaces(corridor.Variants[variant], pathSockets) == wanted)
                return variant;
        return -1;
    }

    /// <summary>
    /// The corridor variant without path faces and with the highest weight (the empty tile), or -1.
    /// The corridor grid has one layer, so a height curve counts at its bottom (height 0), as in the solver.
    /// </summary>
    public static int EmptyVariant(CompiledTileSet corridor, string pathSocket) => EmptyVariant(corridor, SocketNames(pathSocket));

    /// <inheritdoc cref="EmptyVariant(CompiledTileSet, string)"/>
    public static int EmptyVariant(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets)
    {
        int best = -1;
        for (int variant = 0; variant < corridor.Variants.Count; variant++)
            if (PathFaces(corridor.Variants[variant], pathSockets) == 0 && corridor.WeightAt(variant, 0) > 0
                && (best < 0 || corridor.WeightAt(variant, 0) > corridor.WeightAt(best, 0)))
                best = variant;
        return best;
    }

    /// <summary>True when a variant has path faces on exactly three horizontal sides, for every one of the four ways.</summary>
    public static bool HasJunction(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets)
    {
        int all = HorizontalDirections.Aggregate(0, (mask, d) => mask | 1 << d);
        var masks = Masks(corridor, pathSockets);
        return HorizontalDirections.All(missing => masks.Contains(all & ~(1 << missing)));
    }

    private static HashSet<int> Masks(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets) =>
        corridor.Variants.Select(v => PathFaces(v, pathSockets)).ToHashSet();

    /// <summary>True when the four right-angle bends all exist.</summary>
    public static bool HasBends(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets)
    {
        var masks = Masks(corridor, pathSockets);
        int px = 1 << Directions.PosX, nx = 1 << Directions.NegX, pz = 1 << Directions.PosZ, nz = 1 << Directions.NegZ;
        return new[] { px | pz, pz | nx, nx | nz, nz | px }.All(masks.Contains);
    }

    /// <summary>True when a piece with path faces on all four horizontal sides exists.</summary>
    public static bool HasCross(CompiledTileSet corridor, IReadOnlyCollection<string> pathSockets) =>
        Masks(corridor, pathSockets).Contains(HorizontalDirections.Aggregate(0, (mask, d) => mask | 1 << d));

    /// <summary>
    /// Bit mask of the horizontal faces of a variant with a label named in <paramref name="pathSocket"/>,
    /// a comma-separated list (any +/- form).
    /// </summary>
    public static int PathFaces(TileVariant variant, string pathSocket) => PathFaces(variant, SocketNames(pathSocket));

    /// <inheritdoc cref="PathFaces(TileVariant, string)"/>
    public static int PathFaces(TileVariant variant, IReadOnlyCollection<string> pathSockets)
    {
        int mask = 0;
        foreach (int direction in HorizontalDirections)
            if (HasAnyName(variant.Sockets[direction], pathSockets))
                mask |= 1 << direction;
        return mask;
    }

    /// <summary>The socket names of a comma-separated list such as "corridor, pitfall": trimmed, without empty or repeated entries.</summary>
    public static string[] SocketNames(string list) =>
        (list ?? "").Split(SocketSet.Separator).Select(n => n.Trim()).Where(n => n.Length > 0).Distinct().ToArray();

    /// <summary>True when any label of <paramref name="face"/> has one of <paramref name="names"/>.</summary>
    public static bool HasAnyName(SocketSet face, IReadOnlyCollection<string> names)
    {
        foreach (var name in names)
            if (face.HasName(name))
                return true;
        return false;
    }

    /// <summary>"'corridor' or 'pitfall'" for messages.</summary>
    public static string Quoted(IEnumerable<string> names) => string.Join(" or ", names.Select(n => $"'{n}'"));

    private static bool IsEdgeOut(SocketSet socket, string edge) => socket.Labels.Any(l => l.Name == edge && l.Kind == SocketKind.HorizontalPlus);

    /// <summary>Every label on every horizontal face, one entry per label of a comma-separated list.</summary>
    private static IEnumerable<(int Tile, Socket Socket)> HorizontalFaces(CompiledTileSet set) =>
        set.Variants.SelectMany(v => HorizontalDirections.SelectMany(d => v.Sockets[d].Labels.Select(l => (v.TileIndex, l))));

    private static string Names(IReadOnlyList<string> names, IEnumerable<(int Tile, Socket Socket)> faces) =>
        string.Join(", ", faces.Select(f => f.Tile < names.Count ? names[f.Tile] : $"tile {f.Tile}").Distinct());
}
