namespace Godoodads.Tests.Dungeon;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Core;
using static GdUnit4.Assertions;

/// <summary>The DungeonRoomBox node (brief godoodungeon §13.1; card dgn-1-2-room-box-node).</summary>
[TestSuite]
public class DungeonRoomBoxTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void A_room_box_keeps_mode_data_origin_and_size_through_a_saved_scene()
    {
        var root = new Node3D { Name = "Root" };
        var map = new GridMap { Name = "GridMap" };
        root.AddChild(map);
        map.Owner = root;
        var box = new DungeonRoomBox
        {
            Name = "BossRoom", Mode = RoomBoxMode.Built, Origin = new Vector3I(4, 0, 9), Size = new Vector3I(7, 2, 6),
        };
        box.Data["role"] = "boss";
        map.AddChild(box);
        box.Owner = root;

        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string path = "user://room_box_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, path)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var room = loaded.GetNode<DungeonRoomBox>("GridMap/BossRoom");
        AssertThat(room.Mode).IsEqual(RoomBoxMode.Built);
        AssertThat(room.Data["role"].AsString()).IsEqual("boss");
        AssertThat(room.Origin).IsEqual(new Vector3I(4, 0, 9));
        AssertThat(room.Size).IsEqual(new Vector3I(7, 2, 6));
        AssertThat(room.Enabled).IsTrue();
        // The node sits at the centre of the room's floor (card dgn-1-2-1-box-pivot-centre): cells 4..10
        // and 9..14 of 2 m each, so x 8..22 and z 18..30, floor at y 0.
        AssertThat(room.Position).IsEqual(new Vector3(15, 0, 24));
        loaded.Free();
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Only_enabled_direct_room_box_children_count_in_child_order()
    {
        var map = new GridMap();
        var entrance = new DungeonRoomBox { Name = "Entrance" };
        var off = new DungeonRoomBox { Name = "Off", Enabled = false };
        var holder = new Node3D { Name = "Holder" };
        var nested = new DungeonRoomBox { Name = "Nested" };
        var boss = new DungeonRoomBox { Name = "Boss", Mode = RoomBoxMode.Built };
        map.AddChild(entrance);
        map.AddChild(off);
        map.AddChild(holder);
        holder.AddChild(nested);
        map.AddChild(boss);

        var boxes = GridMapBox.ActiveChildren<DungeonRoomBox>(map);
        AssertThat(boxes.Select(b => b.Name.ToString()).ToArray()).IsEqual(new[] { "Entrance", "Boss" });
        AssertThat(entrance.BoxColour).IsNotEqual(boss.BoxColour);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Add_room_box_centres_a_generated_box_at_max_room_size_and_room_height()
    {
        AssertThat(DungeonRoomBox.CentredIn(new Vector3I(-4, 2, 6), new Vector3I(64, 3, 48), new Vector2I(9, 7), 2))
            .IsEqual((new Vector3I(23, 2, 26), new Vector3I(9, 2, 7)));
        // Never larger than the bounds.
        AssertThat(DungeonRoomBox.CentredIn(Vector3I.Zero, new Vector3I(6, 1, 6), new Vector2I(9, 9), 3))
            .IsEqual((Vector3I.Zero, new Vector3I(6, 1, 6)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Adding_a_room_box_is_one_undo_step_and_it_is_saved_with_the_scene()
    {
        // The same do and undo pair as the plugin's OnAddRoomBoxRequested, on a plain UndoRedo.
        var root = new Node3D { Name = "Root" };
        var map = new GridMap { Name = "GridMap" };
        root.AddChild(map);
        map.Owner = root;
        var box = new DungeonRoomBox { Name = "RoomBox", Origin = new Vector3I(23, 0, 26), Size = new Vector3I(9, 1, 7) };
        var undo = new UndoRedo();
        undo.CreateAction("Add Room Box");
        undo.AddDoMethod(Callable.From(() => map.AddChild(box, true)));
        undo.AddDoMethod(Callable.From(() => box.Owner = root));
        undo.AddUndoMethod(Callable.From(() => map.RemoveChild(box)));
        undo.CommitAction();
        AssertThat(GridMapBox.ActiveChildren<DungeonRoomBox>(map).Single() == box).IsTrue();
        AssertThat(box.Mode).IsEqual(RoomBoxMode.Generated);
        undo.Undo();
        AssertThat(GridMapBox.ActiveChildren<DungeonRoomBox>(map).Count).IsEqual(0);
        undo.Redo();
        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        var copy = packed.Instantiate();
        AssertThat(copy.GetNode<DungeonRoomBox>("GridMap/RoomBox").Size).IsEqual(new Vector3I(9, 1, 7));
        copy.Free();
        undo.Free();
        root.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_job_with_an_invalid_room_box_fails_once_naming_the_box()
    {
        var settings = DungeonEngineTiles.Valid();
        settings.BoundsSize = new Vector3I(40, 1, 40);
        settings.RoomCount = 6;
        var map = new GridMap { MeshLibrary = settings.RoomTileSet.Library };
        map.AddChild(new DungeonRoomBox { Name = "Tiny", Origin = new Vector3I(4, 0, 4), Size = new Vector3I(2, 1, 6) });
        map.AddChild(new DungeonRoomBox { Name = "Empty", Mode = RoomBoxMode.Built, Origin = new Vector3I(20, 0, 20), Size = new Vector3I(6, 1, 6) });
        var check = Godoodads.Dungeon.Adapters.DungeonTileSets.Check(settings, map);
        var boxes = Godoodads.Dungeon.Adapters.DungeonGridMap.ReadRoomBoxes(map, settings.BoundsOrigin, settings.Door);
        var result = new DungeonJob(Godoodads.Dungeon.Adapters.DungeonGridMap.Request(settings, check, new System.Collections.Generic.HashSet<Vector3I>(), boxes)).RunNow();
        AssertThat(result.Status).IsEqual(DungeonStatus.Invalid);
        AssertThat(result.Attempts.Count).IsEqual(0);
        AssertThat(result.Errors.Count).IsEqual(2);
        AssertThat(result.Errors[0]).StartsWith("Room box `Tiny` (room 1) is 2 × 6 cells");
        AssertThat(result.Errors[1]).StartsWith("Room box `Empty` (room 2) is Built but has no door");
        map.Free();
    }
}
