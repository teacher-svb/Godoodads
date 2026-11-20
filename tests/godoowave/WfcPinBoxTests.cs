namespace Godoodads.Tests.Wave;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
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

    [TestCase]
    [RequireGodotRuntime]
    public void A_1_4_scene_with_pinned_cells_loads_and_its_pins_are_ordinary_generated_cells()
    {
        // Brief §16.4: no migration. See tests/godoowave/fixtures/wfc_1_4/README.md.
        var root = ResourceLoader.Load<PackedScene>("res://tests/godoowave/fixtures/wfc_1_4/pinned_1_4.tscn", null, ResourceLoader.CacheMode.Ignore)
            .Instantiate();
        var map = root.GetNode<GridMap>("GridMap");
        var settings = WfcSettings.StoredOn(map);
        AssertThat(settings).IsNotNull();
        AssertThat(settings.Seed).IsEqual(3L);
        AssertThat(settings.BoundsSize).IsEqual(new Vector3I(3, 1, 1));
        AssertThat(map.GetCellItem(new Vector3I(2, 0, 0))).IsEqual(2);
        AssertThat(map.GetCellItemOrientation(new Vector3I(2, 0, 0))).IsEqual(10);

        var record = GridMapCells.ReadRecord(map);
        AssertThat(record.Count).IsEqual(3);
        AssertThat(record.Matches(new Vector3I(0, 0, 0), 1, 0) && record.Matches(new Vector3I(2, 0, 0), 2, 10)).IsTrue();

        var binding = TileSetBinding.Create(settings.TileSet, Directions.All3D);
        var grid = GridTopology.Create3D(settings.BoundsSize);
        var cells = GridMapCells.ReadFixed(map, settings.BoundsOrigin, grid, binding, record);
        AssertThat(cells.Replaced).IsEqual(3);   // the old pins no longer keep cells
        AssertThat(cells.Initial.All(c => c == WfcSolver.Free)).IsTrue();
        root.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Add_pin_box_centres_the_box_in_the_bounds_at_half_size_and_full_height()
    {
        AssertThat(WfcPinBox.CentredIn(new Vector3I(10, -2, 4), new Vector3I(24, 8, 16)))
            .IsEqual((new Vector3I(16, -2, 8), new Vector3I(12, 8, 8)));
        AssertThat(WfcPinBox.CentredIn(Vector3I.Zero, new Vector3I(5, 3, 7))).IsEqual((new Vector3I(1, 0, 2), new Vector3I(2, 3, 3)));
        AssertThat(WfcPinBox.CentredIn(Vector3I.Zero, new Vector3I(1, 1, 1))).IsEqual((Vector3I.Zero, Vector3I.One));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Adding_a_pin_box_is_one_undo_step_and_the_box_is_saved_with_the_scene()
    {
        // The same do and undo pair as the plugin's OnAddPinBoxRequested, on a plain UndoRedo.
        var root = new Node3D { Name = "Root" };
        var map = Map();
        root.AddChild(map);
        map.Owner = root;
        var (origin, size) = WfcPinBox.CentredIn(Vector3I.Zero, new Vector3I(8, 2, 8));
        var box = new WfcPinBox { Name = "PinBox", Origin = origin, Size = size };
        var undo = new UndoRedo();
        undo.CreateAction("Add WFC Pin Box");
        undo.AddDoMethod(Callable.From(() => map.AddChild(box, true)));
        undo.AddDoMethod(Callable.From(() => box.Owner = root));
        undo.AddUndoMethod(Callable.From(() => map.RemoveChild(box)));
        undo.CommitAction();

        AssertThat(GridMapBox.ActiveChildren<WfcPinBox>(map).Single() == box).IsTrue();
        AssertThat(box.Owner == root).IsTrue();
        undo.Undo();
        AssertThat(GridMapBox.ActiveChildren<WfcPinBox>(map).Count).IsEqual(0);
        undo.Redo();
        AssertThat(box.GetParent() == map && box.Owner == root).IsTrue();

        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        var copy = packed.Instantiate();
        AssertThat(copy.GetNode<WfcPinBox>("GridMap/PinBox").Origin).IsEqual(new Vector3I(2, 0, 2));
        copy.Free();
        undo.Free();
        root.Free();
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
