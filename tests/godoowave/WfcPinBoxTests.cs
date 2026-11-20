namespace Godoodads.Tests.Wave;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common;
using Godoodads.Wave;
using static GdUnit4.Assertions;

// WFC brief §16.1, §16.3: the WfcPinBox node and its shared GridMapBox base (card wfc-1-5-pinbox-node).
[TestSuite]
public class WfcPinBoxTests
{
    private static GridMap Map() => new() { Name = "GridMap", CellSize = new Vector3(2, 1.5f, 3), CellCenterY = false };

    [TestCase]
    [RequireGodotRuntime]
    public void A_pin_box_keeps_origin_size_and_enabled_through_a_saved_scene()
    {
        var root = new Node3D { Name = "Root" };
        var map = Map();
        root.AddChild(map);
        map.Owner = root;
        var box = new WfcPinBox { Name = "Pin", Origin = new Vector3I(-3, 1, 7), Size = new Vector3I(5, 2, 4), Enabled = false };
        map.AddChild(box);
        box.Owner = root;
        AssertThat(box.Position).IsEqual(GridMapBox.CornerOf(map, new Vector3I(-3, 1, 7)));

        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string path = "user://pin_box_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, path)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var loadedMap = loaded.GetNode<GridMap>("GridMap");
        var pin = loadedMap.GetNode<WfcPinBox>("Pin");
        AssertThat(pin.Origin).IsEqual(new Vector3I(-3, 1, 7));
        AssertThat(pin.Size).IsEqual(new Vector3I(5, 2, 4));
        AssertThat(pin.Enabled).IsFalse();
        AssertThat(pin.Position).IsEqual(GridMapBox.CornerOf(loadedMap, new Vector3I(-3, 1, 7)));
        loaded.Free();
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Only_enabled_direct_children_with_cells_are_active_in_child_order()
    {
        var map = Map();
        var first = new WfcPinBox { Name = "First", Origin = new Vector3I(0, 0, 0) };
        var disabled = new WfcPinBox { Name = "Disabled", Enabled = false };
        var flat = new WfcPinBox { Name = "Flat", Size = new Vector3I(3, 0, 3) };
        var holder = new Node3D { Name = "Holder" };
        var nested = new WfcPinBox { Name = "Nested" };
        var second = new WfcPinBox { Name = "Second", Origin = new Vector3I(5, 0, 5) };
        map.AddChild(first);
        map.AddChild(disabled);
        map.AddChild(flat);
        map.AddChild(holder);
        holder.AddChild(nested);
        map.AddChild(second);

        var active = GridMapBox.ActiveChildren<WfcPinBox>(map);
        AssertThat(active.Select(b => b.Name.ToString()).ToArray()).IsEqual(new[] { "First", "Second" });
        AssertThat(first.IsActive && !disabled.IsActive && !flat.IsActive && !nested.IsActive).IsTrue();
        AssertThat(GridMapBox.ActiveChildren<WfcPinBox>(null).Count).IsEqual(0);

        AssertThat(first.HasCell(new Vector3I(3, 0, 3))).IsTrue();
        AssertThat(first.HasCell(new Vector3I(4, 0, 3))).IsFalse();
        AssertThat(first.HasCell(new Vector3I(0, 1, 0))).IsFalse();
        AssertThat(nested._GetConfigurationWarnings().Length).IsEqual(1);
        AssertThat(first._GetConfigurationWarnings().Length).IsEqual(0);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Moving_a_box_in_the_tree_snaps_its_origin_and_resets_rotation_and_scale()
    {
        var map = Map();
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(map);
        try
        {
            var box = new WfcPinBox { Origin = new Vector3I(1, 0, 1) };
            map.AddChild(box);
            box.Position = new Vector3(6.9f, 2.4f, -4.2f);
            AssertThat(box.Origin).IsEqual(new Vector3I(3, 2, -1));   // 6.9 / 2, 2.4 / 1.5, -4.2 / 3, rounded
            AssertThat(box.Position).IsEqual(GridMapBox.CornerOf(map, new Vector3I(3, 2, -1)));
            box.Rotation = new Vector3(0, 0.7f, 0);
            box.Scale = new Vector3(2, 2, 2);
            AssertThat(box.Basis.IsEqualApprox(Basis.Identity)).IsTrue();
            AssertThat(box.Origin).IsEqual(new Vector3I(3, 2, -1));
        }
        finally
        {
            map.Free();
        }
    }

#if TOOLS
    [TestCase]
    [RequireGodotRuntime]
    public void The_box_outline_and_handles_match_the_bounds_gizmo()
    {
        var map = Map();
        var box = new WfcPinBox { Origin = new Vector3I(-2, 1, 4), Size = new Vector3I(3, 2, 5) };
        map.AddChild(box);
        var (min, max) = Godoodads.Common.Editor.GridMapBoxGizmoPlugin.Corners(map, box);
        var (boundsMin, boundsMax) = Godoodads.Common.Wfc.Editor.BoundsGizmoPlugin.Extents(map, box.Origin, box.Size);
        AssertThat(box.Position + min).IsEqual(boundsMin);
        AssertThat(box.Position + max).IsEqual(boundsMax);
        var handles = Godoodads.Common.Wfc.Editor.BoundsGizmoPlugin.HandlePositions(map, box.Origin, box.Size);
        for (int direction = 0; direction < 6; direction++)
            AssertThat(box.Position + Godoodads.Common.Editor.GridMapBoxGizmoPlugin.FaceCentre(min, max, direction)).IsEqual(handles[direction]);
        map.Free();
    }
#endif
}
