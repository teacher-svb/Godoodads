using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon.Adapters;

/// <summary>
/// The room and corridor tile sets of a <see cref="DungeonSettings"/>, compiled for the solver and
/// checked against brief godoodungeon §4.2, together with the layout and bounds settings.
/// </summary>
public sealed class DungeonTileSets
{
    public TileSetBinding Room { get; private set; }
    public TileSetBinding Corridor { get; private set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;

    /// <summary>Checks everything Generate needs before solving. <paramref name="map"/> is optional: with it, its MeshLibrary is checked too.</summary>
    public static DungeonTileSets Check(DungeonSettings settings, GridMap map = null)
    {
        var result = new DungeonTileSets();
        result.Errors.AddRange(settings.CheckBounds());
        result.Errors.AddRange(LayoutValidator.Validate(settings.ToLayoutSettings()));

        if (settings.RoomTileSet == null)
            result.Errors.Add("Choose a Room Tile Set.");
        if (settings.CorridorTileSet == null)
            result.Errors.Add("Choose a Corridor Tile Set.");
        if (settings.RoomTileSet == null || settings.CorridorTileSet == null)
            return result;

        if (!SameLibrary(settings.RoomTileSet.Library, settings.CorridorTileSet.Library))
            result.Errors.Add("The room and corridor tile sets use different MeshLibrary resources. Both must use the GridMap's MeshLibrary.");
        else if (map != null && !SameLibrary(map.MeshLibrary, settings.RoomTileSet.Library))
            result.Errors.Add("The GridMap uses a different MeshLibrary than the tile sets.");

        result.Room = TileSetBinding.Create(settings.RoomTileSet, Directions.All3D);
        result.Corridor = TileSetBinding.Create(settings.CorridorTileSet, Directions.All3D);
        result.Errors.AddRange(result.Room.Errors.Select(e => $"Room tile set: {e}"));
        result.Errors.AddRange(result.Corridor.Errors.Select(e => $"Corridor tile set: {e}"));
        if (!result.Room.IsValid || !result.Corridor.IsValid)
            return result;

        var (errors, warnings) = DungeonTileRules.Check(result.Room.Compiled, result.Room.TileNames, settings.DoorTile,
            settings.RoomEdgeSocket, result.Corridor.Compiled, settings.PathSocket);
        result.Errors.AddRange(errors);
        result.Warnings.AddRange(warnings);
        return result;
    }

    // Two loaded copies of one file count as the same library.
    private static bool SameLibrary(MeshLibrary a, MeshLibrary b) =>
        a != null && b != null && (a == b || (!string.IsNullOrEmpty(a.ResourcePath) && a.ResourcePath == b.ResourcePath));
}
