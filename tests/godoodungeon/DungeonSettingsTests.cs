namespace Godoodads.Tests.Dungeon;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Adapters;
using static GdUnit4.Assertions;

[TestSuite]
public class DungeonSettingsTests
{
    private static WfcTile Tile(string name, int item, string[] s, TileRotation rotation = TileRotation.None, float weight = 1) => new()
    {
        ResourceName = name, Item = item, Rotation = rotation, Weight = weight,
        SocketPosX = s[0], SocketNegX = s[1], SocketPosY = s[2], SocketNegY = s[3], SocketPosZ = s[4], SocketNegZ = s[5],
    };

    private static MeshLibrary Library()
    {
        var library = new MeshLibrary();
        for (int i = 0; i < 6; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        return library;
    }

    /// <summary>The M0 prototype's rules: a one-sided edge socket, a weight-0 door, corridor straights and an empty tile.</summary>
    private static DungeonSettings Valid(MeshLibrary library = null)
    {
        library ??= Library();
        var room = new WfcTileSet { Library = library };
        room.Tiles.Add(Tile("Floor", 0, new[] { "in", "in", "v", "v", "in", "in" }));
        room.Tiles.Add(Tile("Wall", 1, new[] { "outside+", "in", "v", "v", "edge", "edge" }, TileRotation.FourWay));
        room.Tiles.Add(Tile("Corner", 2, new[] { "outside+", "edge", "v", "v", "outside+", "edge" }, TileRotation.FourWay));
        room.Tiles.Add(Tile("Door", 3, new[] { "outside+", "in", "v", "v", "edge", "edge" }, TileRotation.FourWay, 0));
        var corridor = new WfcTileSet { Library = library };
        corridor.Tiles.Add(Tile("Straight", 4, new[] { "path", "path", "v", "v", "x", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("End", 5, new[] { "path", "x", "v", "v", "x", "x" }, TileRotation.FourWay));
        corridor.Tiles.Add(Tile("Empty", WfcTile.EmptyItem, new[] { "x", "x", "v", "v", "x", "x" }, TileRotation.None, 20));
        return new DungeonSettings { RoomTileSet = room, CorridorTileSet = corridor, DoorTile = 3 };
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Valid_settings_pass_and_bind_both_tile_sets()
    {
        var settings = Valid();
        var map = new GridMap { MeshLibrary = settings.RoomTileSet.Library };
        var check = DungeonTileSets.Check(settings, map);
        AssertThat(string.Join("; ", check.Errors)).IsEqual("");
        AssertThat(check.Warnings.Count).IsEqual(0);
        AssertThat(check.Room.Compiled.Variants.Count > 0).IsTrue();
        AssertThat(check.Corridor.Compiled.Variants.Count > 0).IsTrue();
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Section_4_2_errors_are_reported()
    {
        AssertThat(DungeonTileSets.Check(new DungeonSettings()).Errors).Contains("Choose a Room Tile Set.", "Choose a Corridor Tile Set.");

        var symmetric = Valid();
        symmetric.RoomTileSet.Tiles[1].SocketPosX = "outside";
        AssertThat(DungeonTileSets.Check(symmetric).Errors.Single()).StartsWith("Room Edge Socket 'outside' must be one-sided");

        var noDoor = Valid();
        noDoor.DoorTile = -1;
        AssertThat(DungeonTileSets.Check(noDoor).Errors.Single()).IsEqual("Choose the Door Tile from the room tile set.");

        var cornerDoor = Valid();
        cornerDoor.DoorTile = 2;
        AssertThat(DungeonTileSets.Check(cornerDoor).Errors.Single()).StartsWith("The Door Tile 'Corner' needs exactly one horizontal face");

        var wrongPath = Valid();
        wrongPath.PathSocket = "road";
        AssertThat(DungeonTileSets.Check(wrongPath).Errors.First()).StartsWith("No corridor tile has a face 'road'");

        var noStraight = Valid();
        noStraight.CorridorTileSet.Tiles.RemoveAt(0);
        AssertThat(DungeonTileSets.Check(noStraight).Errors.Count(e => e.Contains("no straight piece"))).IsEqual(2);

        var noEmpty = Valid();
        noEmpty.CorridorTileSet.Tiles.RemoveAt(2);
        AssertThat(DungeonTileSets.Check(noEmpty).Errors.Single()).StartsWith("The corridor tile set has no tile without 'path' faces");

        var weightedDoor = Valid();
        weightedDoor.RoomTileSet.Tiles[3].Weight = 1;
        var weighted = DungeonTileSets.Check(weightedDoor);
        AssertThat(weighted.IsValid).IsTrue();
        AssertThat(weighted.Warnings.Single()).StartsWith("The Door Tile 'Door' has a weight above 0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Libraries_bounds_and_layout_numbers_are_checked()
    {
        var mixed = Valid();
        mixed.CorridorTileSet.Library = Library();
        AssertThat(DungeonTileSets.Check(mixed).Errors.Single()).StartsWith("The room and corridor tile sets use different MeshLibrary");

        var settings = Valid();
        var map = new GridMap { MeshLibrary = Library() };
        AssertThat(DungeonTileSets.Check(settings, map).Errors.Single()).IsEqual("The GridMap uses a different MeshLibrary than the tile sets.");
        map.Free();

        var tall = Valid();
        tall.RoomHeight = 3;
        AssertThat(DungeonTileSets.Check(tall).Errors.Single()).IsEqual("Room Height is 3, but the bounds have 1 layer(s) on Y.");

        var crowded = Valid();
        crowded.Gap = 1;
        AssertThat(DungeonTileSets.Check(crowded).Errors.Single()).StartsWith("Gap is 1, but Wall Clearance 1 needs a gap of at least 3");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_door_tile_is_a_drop_down_of_the_room_tiles()
    {
        var settings = Valid();
        var property = settings.GetPropertyList().Single(p => p["name"].AsString() == "DoorTile");
        AssertThat((PropertyHint)property["hint"].AsInt64()).IsEqual(PropertyHint.Enum);
        AssertThat(property["hint_string"].AsString()).IsEqual("(none):-1,Floor:0,Wall:1,Corner:2,Door:3");

        var empty = new DungeonSettings();
        var none = empty.GetPropertyList().Single(p => p["name"].AsString() == "DoorTile");
        AssertThat(none["hint_string"].AsString()).IsEqual("Choose a Room Tile Set first:-1");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Settings_survive_a_save_and_reload_of_the_scene()
    {
        var settings = Valid();
        settings.Seed = 4242;
        settings.RoomCount = 17;
        settings.MinRoomSize = new Vector2I(4, 6);
        settings.WallClearance = 2;
        settings.Gap = 5;
        settings.ExtraConnections = 0.4f;
        settings.BoundsOrigin = new Vector3I(-3, 1, 7);
        settings.BoundsSize = new Vector3I(60, 2, 40);
        settings.RandomiseSeed = true;

        var root = new Node3D { Name = "Root" };
        var map = new GridMap { Name = "GridMap", MeshLibrary = settings.RoomTileSet.Library };
        root.AddChild(map);
        map.Owner = root;
        DungeonSettings.StoreOn(map, settings);
        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string path = "user://dungeon_settings_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, path)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var stored = DungeonSettings.StoredOn(loaded.GetNode<GridMap>("GridMap"));
        AssertThat(stored).IsNotNull();
        AssertThat(stored.Seed).IsEqual(4242L);
        AssertThat(stored.RoomCount).IsEqual(17);
        AssertThat(stored.MinRoomSize).IsEqual(new Vector2I(4, 6));
        AssertThat(stored.WallClearance).IsEqual(2);
        AssertThat(stored.Gap).IsEqual(5);
        AssertThat(stored.ExtraConnections).IsEqual(0.4f);
        AssertThat(stored.BoundsOrigin).IsEqual(new Vector3I(-3, 1, 7));
        AssertThat(stored.BoundsSize).IsEqual(new Vector3I(60, 2, 40));
        AssertThat(stored.RandomiseSeed).IsTrue();
        AssertThat(stored.DoorTile).IsEqual(3);
        AssertThat(stored.RoomTileSet.Tiles.Count).IsEqual(4);
        AssertThat(stored.CorridorTileSet.Tiles[0].SocketPosX).IsEqual("path");
        AssertThat(DungeonTileSets.Check(stored).IsValid).IsTrue();
        loaded.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Layout_settings_take_the_bounds_x_and_z()
    {
        var settings = Valid();
        settings.BoundsSize = new Vector3I(30, 4, 50);
        var layout = settings.ToLayoutSettings();
        AssertThat(layout.Bounds).IsEqual(new Vector2I(30, 50));
        AssertThat(layout.RoomCount).IsEqual(settings.RoomCount);
        AssertThat(System.Math.Abs(layout.ExtraConnections - 0.15) < 1e-6).IsTrue();
    }
}
