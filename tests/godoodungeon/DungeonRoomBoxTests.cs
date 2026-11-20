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
}
