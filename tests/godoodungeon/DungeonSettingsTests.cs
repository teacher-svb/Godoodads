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
        noDoor.Door = DungeonSettings.NoDoor;
        AssertThat(DungeonTileSets.Check(noDoor).Errors.Single()).IsEqual("Choose the Door tile from the room tile set.");

        var cornerDoor = DungeonEngineTiles.Valid();
        cornerDoor.Door = cornerDoor.RoomTileSet.Tiles[DungeonEngineTiles.Corner].Item;
        AssertThat(DungeonTileSets.Check(cornerDoor).Errors.Single()).StartsWith("The Door tile 'Corner' needs exactly one horizontal face");

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
        AssertThat(weighted.Warnings.Single()).StartsWith("The Door tile 'Door' has a weight above 0");
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
    public void The_door_is_a_drop_down_of_the_room_tiles_by_item()
    {
        var settings = DungeonEngineTiles.Valid();
        // A tile in front whose item is not its position, and an empty tile, which cannot be a door.
        settings.RoomTileSet.Tiles.Insert(0, DungeonEngineTiles.Tile("Rubble", 7, new[] { "in", "in", "v", "v", "in", "in" }));
        settings.RoomTileSet.Tiles.Add(DungeonEngineTiles.Tile("Air", WfcTile.EmptyItem, new[] { "in", "in", "v", "v", "in", "in" }));
        var property = settings.GetPropertyList().Single(p => p["name"].AsString() == DungeonSettings.PropertyName.Door);
        AssertThat((PropertyHint)property["hint"].AsInt64()).IsEqual(PropertyHint.Enum);
        AssertThat(property["hint_string"].AsString()).IsEqual("(none):-1,Rubble:7,Floor:0,Wall:1,Corner:2,Door:3");
        AssertThat(settings.GetPropertyList().Any(p => p["name"].AsString() == "DoorTile")).IsFalse();

        var empty = new DungeonSettings();
        var none = empty.GetPropertyList().Single(p => p["name"].AsString() == DungeonSettings.PropertyName.Door);
        AssertThat(none["hint_string"].AsString()).IsEqual("Choose a Room Tile Set first:-1");
    }

    /// <summary>A room tile set where positions and items differ: Rubble (7) in front of Floor, Wall, Corner and Door (3).</summary>
    private static WfcTileSet ShiftedRoomTiles(string path)
    {
        var room = DungeonEngineTiles.Valid().RoomTileSet;
        room.Tiles.Insert(0, DungeonEngineTiles.Tile("Rubble", 7, new[] { "in", "in", "v", "v", "in", "in" }));
        AssertThat(ResourceSaver.Save(room, path)).IsEqual(Error.Ok);
        return room;
    }

    private static DungeonSettings LoadSettingsText(string path, string properties)
    {
        string text = "[gd_resource type=\"Resource\" script_class=\"DungeonSettings\" format=3]\n\n"
            + "[ext_resource type=\"Script\" path=\"res://addons/godoodungeon/Resources/DungeonSettings.cs\" id=\"1_s\"]\n"
            + "[ext_resource type=\"Resource\" path=\"user://door_legacy_room.tres\" id=\"2_r\"]\n\n"
            + "[resource]\nscript = ExtResource(\"1_s\")\n" + properties;
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
            file.StoreString(text);
        return ResourceLoader.Load<DungeonSettings>(path, null, ResourceLoader.CacheMode.Ignore);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_door_tile_saved_by_1_0_0_loads_as_that_tiles_item_and_saves_as_door()
    {
        ShiftedRoomTiles("user://door_legacy_room.tres");
        const string path = "user://door_legacy_settings.tres";

        // 1.0.0 stored the position: 4 is Door, whose item is 3.
        var loaded = LoadSettingsText(path, "RoomTileSet = ExtResource(\"2_r\")\nDoorTile = 4\n");
        AssertThat(loaded.Door).IsEqual(3);
        AssertThat(loaded.RoomTileSet.Tiles[4].ResourceName).IsEqual("Door");

        // In either order: the position waits for the room tile set.
        AssertThat(LoadSettingsText(path, "DoorTile = 4\nRoomTileSet = ExtResource(\"2_r\")\n").Door).IsEqual(3);
        AssertThat(LoadSettingsText(path, "RoomTileSet = ExtResource(\"2_r\")\nDoorTile = -1\n").Door).IsEqual(DungeonSettings.NoDoor);
        AssertThat(LoadSettingsText(path, "RoomTileSet = ExtResource(\"2_r\")\nDoorTile = 40\n").Door).IsEqual(DungeonSettings.NoDoor);

        // An assembly reload sets DoorTile by name too: the position a 1.0.0 instance held, or the -1
        // that a 1.0.1 instance saved, restored after its Door.
        var reloadedOld = new DungeonSettings { RoomTileSet = loaded.RoomTileSet };
        reloadedOld.Set("DoorTile", 4);
        AssertThat(reloadedOld.Door).IsEqual(3);
        var reloadedNew = new DungeonSettings { RoomTileSet = loaded.RoomTileSet, Door = 3 };
        reloadedNew.Set("DoorTile", -1);
        AssertThat(reloadedNew.Door).IsEqual(3);

        // Saved again, it holds the item and no position.
        AssertThat(ResourceSaver.Save(loaded, path)).IsEqual(Error.Ok);
        string saved = FileAccess.GetFileAsString(path);
        AssertThat(saved.Contains("Door = 3")).IsTrue();
        AssertThat(saved.Contains("DoorTile")).IsFalse();
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://door_legacy_room.tres"));
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
        AssertThat(stored.Door).IsEqual(3);
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

    [TestCase]
    [RequireGodotRuntime]
    public void Undoing_a_bounds_resize_changes_only_the_settings_that_were_dragged()
    {
        // The same do and undo pair as the plugin's CommitBox, on a plain UndoRedo, called by name
        // as the editor's undo manager calls it after an assembly reload.
        var dragged = new DungeonSettings { BoundsOrigin = new Vector3I(1, 0, 2), BoundsSize = new Vector3I(30, 1, 30) };
        var other = new DungeonSettings { BoundsOrigin = new Vector3I(-5, 0, 5), BoundsSize = new Vector3I(40, 2, 40) };
        var (fromOrigin, fromSize) = (dragged.BoundsOrigin, dragged.BoundsSize);
        dragged.SetBounds(new Vector3I(4, 0, 4), new Vector3I(20, 1, 24));   // as the drag left it
        var (toOrigin, toSize) = (dragged.BoundsOrigin, dragged.BoundsSize);
        var undo = new UndoRedo();
        undo.CreateAction("Resize Dungeon Bounds");
        undo.AddDoMethod(Callable.From(() => dragged.Call(DungeonSettings.MethodName.SetBounds, toOrigin, toSize)));
        undo.AddUndoMethod(Callable.From(() => dragged.Call(DungeonSettings.MethodName.SetBounds, fromOrigin, fromSize)));
        undo.CommitAction(false);

        // The dialog is opened for another GridMap, so it edits other settings; then Ctrl+Z.
        undo.Undo();
        AssertThat(dragged.BoundsOrigin).IsEqual(new Vector3I(1, 0, 2));
        AssertThat(dragged.BoundsSize).IsEqual(new Vector3I(30, 1, 30));
        AssertThat(other.BoundsOrigin).IsEqual(new Vector3I(-5, 0, 5));
        AssertThat(other.BoundsSize).IsEqual(new Vector3I(40, 2, 40));
        undo.Redo();
        AssertThat(dragged.BoundsSize).IsEqual(new Vector3I(20, 1, 24));
        AssertThat(other.BoundsSize).IsEqual(new Vector3I(40, 2, 40));
        undo.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_runs_settings_copy_keeps_its_values_when_the_dialog_settings_change()
    {
        var dialog = DungeonEngineTiles.Valid();
        dialog.Seed = 5;
        dialog.SetBounds(Vector3I.Zero, new Vector3I(40, 1, 40));
        var run = (DungeonSettings)dialog.Duplicate();   // as OnActionPressed takes it

        // Edits made in the inspector while the generation runs.
        dialog.Seed = 6;
        dialog.SetBounds(new Vector3I(3, 0, 3), new Vector3I(60, 2, 60));
        dialog.RoomCount = 3;

        AssertThat(run.Seed).IsEqual(5L);
        AssertThat(run.BoundsOrigin).IsEqual(Vector3I.Zero);
        AssertThat(run.BoundsSize).IsEqual(new Vector3I(40, 1, 40));
        AssertThat(run.RoomCount).IsEqual(12);
        AssertThat(run.RoomTileSet == dialog.RoomTileSet).IsTrue();   // the tile set assets are shared, not copied
    }
}
