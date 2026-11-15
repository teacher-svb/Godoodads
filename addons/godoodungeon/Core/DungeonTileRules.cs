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
    /// Checks the room tile set (edge socket, door tile) and the corridor tile set (path socket,
    /// straights, empty variant). Errors make generation impossible; warnings do not.
    /// </summary>
    /// <param name="roomNames">Readable tile names of the room tile set, for messages.</param>
    public static (List<string> Errors, List<string> Warnings) Check(CompiledTileSet room, IReadOnlyList<string> roomNames, int doorTile,
        string edgeSocket, CompiledTileSet corridor, string pathSocket)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        string edge = edgeSocket?.Trim() ?? "";
        string path = pathSocket?.Trim() ?? "";

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
            errors.Add("Choose the Door Tile from the room tile set.");
        else if (edge.Length > 0)
        {
            int variant = room.FindVariant(doorTile, 0);
            int outward = variant < 0 ? 0 : HorizontalDirections.Count(d => IsEdgeOut(room.Variants[variant].Sockets[d], edge));
            if (outward != 1)
                errors.Add($"The Door Tile '{roomNames[doorTile]}' needs exactly one horizontal face with '{edge}+' (its outside), but has {outward}.");
            else if (variant >= 0 && room.VariantWeights[variant] > 0)
                warnings.Add($"The Door Tile '{roomNames[doorTile]}' has a weight above 0, so the solver may also put doors in walls by itself. Weight 0 leaves doors to the layout.");
        }

        // Corridor tile set: path socket, straights both ways, and an empty variant.
        if (path.Length == 0)
            errors.Add("Path Socket is empty. Name the socket of the corridor tiles' open faces, such as 'path'.");
        else
        {
            if (!HorizontalFaces(corridor).Any(f => f.Socket.Name == path))
                errors.Add($"No corridor tile has a face '{path}'. Check Path Socket against the corridor tile set.");
            else
            {
                if (StraightVariant(corridor, path, alongX: true) < 0)
                    errors.Add($"The corridor tile set has no straight piece along X: a tile with '{path}' on exactly +X and -X (or a turnable one). Doors on the X sides of rooms need it.");
                if (StraightVariant(corridor, path, alongX: false) < 0)
                    errors.Add($"The corridor tile set has no straight piece along Z: a tile with '{path}' on exactly +Z and -Z. Give the straight piece four rotations.");
            }
            int empty = EmptyVariant(corridor, path);
            if (empty < 0)
                errors.Add($"The corridor tile set has no tile without '{path}' faces and with a weight above 0, so the space between corridors cannot be filled. Add an empty tile.");
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
    public static int StraightVariant(CompiledTileSet corridor, string pathSocket, bool alongX)
    {
        int wanted = alongX ? (1 << Directions.PosX) | (1 << Directions.NegX) : (1 << Directions.PosZ) | (1 << Directions.NegZ);
        for (int variant = 0; variant < corridor.Variants.Count; variant++)
            if (PathFaces(corridor.Variants[variant], pathSocket.Trim()) == wanted)
                return variant;
        return -1;
    }

    /// <summary>The corridor variant without path faces and with the highest weight (the empty tile), or -1.</summary>
    public static int EmptyVariant(CompiledTileSet corridor, string pathSocket)
    {
        int best = -1;
        for (int variant = 0; variant < corridor.Variants.Count; variant++)
            if (PathFaces(corridor.Variants[variant], pathSocket.Trim()) == 0 && corridor.VariantWeights[variant] > 0
                && (best < 0 || corridor.VariantWeights[variant] > corridor.VariantWeights[best]))
                best = variant;
        return best;
    }

    /// <summary>Bit mask of the horizontal faces of a variant whose socket name is the path socket (any +/- form).</summary>
    public static int PathFaces(TileVariant variant, string pathSocket)
    {
        int mask = 0;
        foreach (int direction in HorizontalDirections)
            if (variant.Sockets[direction].Name == pathSocket)
                mask |= 1 << direction;
        return mask;
    }

    private static bool IsEdgeOut(Socket socket, string edge) => socket.Name == edge && socket.Kind == SocketKind.HorizontalPlus;

    private static IEnumerable<(int Tile, Socket Socket)> HorizontalFaces(CompiledTileSet set) =>
        set.Variants.SelectMany(v => HorizontalDirections.Select(d => (v.TileIndex, v.Sockets[d])));

    private static string Names(IReadOnlyList<string> names, IEnumerable<(int Tile, Socket Socket)> faces) =>
        string.Join(", ", faces.Select(f => f.Tile < names.Count ? names[f.Tile] : $"tile {f.Tile}").Distinct());
}
