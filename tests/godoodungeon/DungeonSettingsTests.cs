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
    [TestCase]
    [RequireGodotRuntime]
    public void Valid_settings_pass_and_bind_both_tile_sets()
    {
        var settings = DungeonEngineTiles.Valid();
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

        var symmetric = DungeonEngineTiles.Valid();
        symmetric.RoomTileSet.Tiles[1].SocketPosX = "outside";
        AssertThat(DungeonTileSets.Check(symmetric).Errors.Single()).StartsWith("Room Edge Socket 'outside' must be one-sided");

        var noDoor = DungeonEngineTiles.Valid();
        noDoor.DoorTile = -1;
        AssertThat(DungeonTileSets.Check(noDoor).Errors.Single()).IsEqual("Choose the Door Tile from the room tile set.");

        var cornerDoor = DungeonEngineTiles.Valid();
        cornerDoor.DoorTile = 2;
        AssertThat(DungeonTileSets.Check(cornerDoor).Errors.Single()).StartsWith("The Door Tile 'Corner' needs exactly one horizontal face");

        var wrongPath = DungeonEngineTiles.Valid();
        wrongPath.PathSocket = "road";
        AssertThat(DungeonTileSets.Check(wrongPath).Errors.First()).StartsWith("No corridor tile has a face 'road'");

        var noStraight = DungeonEngineTiles.Valid();
        noStraight.CorridorTileSet.Tiles.RemoveAt(0);
        AssertThat(DungeonTileSets.Check(noStraight).Errors.Count(e => e.Contains("no straight piece"))).IsEqual(2);

        var noEmpty = DungeonEngineTiles.Valid();
        noEmpty.CorridorTileSet.Tiles.RemoveAt(5);
        AssertThat(DungeonTileSets.Check(noEmpty).Errors.Single()).StartsWith("The corridor tile set has no tile without 'path' faces");

        var weightedDoor = DungeonEngineTiles.Valid();
        weightedDoor.RoomTileSet.Tiles[3].Weight = 1;
        var weighted = DungeonTileSets.Check(weightedDoor);
        AssertThat(weighted.IsValid).IsTrue();
        AssertThat(weighted.Warnings.Single()).StartsWith("The Door Tile 'Door' has a weight above 0");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Libraries_bounds_and_layout_numbers_are_checked()
    {
        var mixed = DungeonEngineTiles.Valid();
        mixed.CorridorTileSet.Library = DungeonEngineTiles.Library();
        AssertThat(DungeonTileSets.Check(mixed).Errors.Single()).StartsWith("The room and corridor tile sets use different MeshLibrary");

        var settings = DungeonEngineTiles.Valid();
        var map = new GridMap { MeshLibrary = DungeonEngineTiles.Library() };
        AssertThat(DungeonTileSets.Check(settings, map).Errors.Single()).IsEqual("The GridMap uses a different MeshLibrary than the tile sets.");
        map.Free();

        var tall = DungeonEngineTiles.Valid();
        tall.RoomHeight = 3;
        AssertThat(DungeonTileSets.Check(tall).Errors.Single()).IsEqual("Room Height is 3, but the bounds have 1 layer(s) on Y.");

        var crowded = DungeonEngineTiles.Valid();
        crowded.Gap = 1;
        AssertThat(DungeonTileSets.Check(crowded).Errors.Single()).StartsWith("Gap is 1, but Wall Clearance 1 needs a gap of at least 3");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void External_sockets_silence_the_one_sided_edge_and_are_checked()
    {
        var settings = DungeonEngineTiles.Valid();
        var check = DungeonTileSets.Check(settings);
        AssertThat(check.Warnings.Count).IsEqual(0);
        AssertThat(check.Room.Warnings().Count).IsEqual(0);

        settings.RoomTileSet.ExternalSockets.Clear();
        check = DungeonTileSets.Check(settings);
        AssertThat(check.IsValid).IsTrue();
        AssertThat(check.Warnings.Single()).StartsWith("Add 'outside-' to the room tile set's External Sockets");
        AssertThat(check.Room.Warnings().Count(w => w.Contains("'outside+' has no partner"))).IsEqual(4); // Wall, Corner (2), Door

        settings.RoomTileSet.ExternalSockets.Add("out side");
        AssertThat(DungeonTileSets.Check(settings).Errors.Single()).StartsWith("Room tile set: External socket 'out side'");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_door_tile_is_a_drop_down_of_the_room_tiles()
    {
        var settings = DungeonEngineTiles.Valid();
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
        var settings = DungeonEngineTiles.Valid();
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
        var settings = DungeonEngineTiles.Valid();
        settings.BoundsSize = new Vector3I(30, 4, 50);
        var layout = settings.ToLayoutSettings();
        AssertThat(layout.Bounds).IsEqual(new Vector2I(30, 50));
        AssertThat(layout.RoomCount).IsEqual(settings.RoomCount);
        AssertThat(System.Math.Abs(layout.ExtraConnections - 0.15) < 1e-6).IsTrue();
    }
}
