namespace Godoodads.Tests.Dungeon;

using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon;

/// <summary>The M0 prototype's rules as engine resources: a MeshLibrary, room and corridor tile sets and settings.</summary>
public static class DungeonEngineTiles
{
    public const int Floor = 0, Wall = 1, Corner = 2, Door = 3;

    public static WfcTile Tile(string name, int item, string[] s, TileRotation rotation = TileRotation.None, float weight = 1) => new()
    {
        ResourceName = name, Item = item, Rotation = rotation, Weight = weight,
        SocketPosX = s[0], SocketNegX = s[1], SocketPosY = s[2], SocketNegY = s[3], SocketPosZ = s[4], SocketNegZ = s[5],
    };

    public static MeshLibrary Library()
    {
        var library = new MeshLibrary();
        for (int i = 0; i < 10; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        return library;
    }

    /// <summary>A one-sided edge socket, a weight-0 door, corridor straights and an empty tile. Item 7 is free for hand-placed cells.</summary>
    public static DungeonSettings Valid(MeshLibrary library = null, PackedScene cornerMarker = null)
    {
        library ??= Library();
        var room = new WfcTileSet { Library = library };
        room.ExternalSockets.Add("outside-");
        room.Tiles.Add(Tile("Floor", 0, new[] { "in", "in", "v", "v", "in", "in" }));
        room.Tiles.Add(Tile("Wall", 1, new[] { "outside+", "in", "v", "v", "edge", "edge" }, TileRotation.FourWay));
        var corner = Tile("Corner", 2, new[] { "outside+", "edge", "v", "v", "outside+", "edge" }, TileRotation.FourWay);
        corner.Marker = cornerMarker;
        room.Tiles.Add(corner);
        room.Tiles.Add(Tile("Door", 3, new[] { "outside+", "in", "v", "v", "edge", "edge" }, TileRotation.FourWay, 0));
        var corridor = new WfcTileSet { Library = library };
        corridor.Tiles.Add(Tile("Straight", 4, new[] { "path", "path", "v", "v", "x", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("Bend", 5, new[] { "path", "x", "v", "v", "path", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("End", 6, new[] { "path", "x", "v", "v", "x", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("T", 8, new[] { "path", "path", "v", "v", "path", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("Cross", 9, new[] { "path", "path", "v", "v", "path", "path" }));
        corridor.Tiles.Add(Tile("Empty", WfcTile.EmptyItem, new[] { "x", "x", "v", "v", "x", "x" }, TileRotation.None, 20));
        return new DungeonSettings { RoomTileSet = room, CorridorTileSet = corridor, Door = room.Tiles[Door].Item };
    }
}
