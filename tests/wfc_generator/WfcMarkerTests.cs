namespace Godoodads.Tests.WfcGenerator;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;
using static GdUnit4.Assertions;

// Brief §12.4: marker scenes placed at generated cells, replaced on the next generation.
[TestSuite]
public class WfcMarkerTests
{
    private static PackedScene Scene(string name)
    {
        var node = new Node3D { Name = name };
        var scene = new PackedScene();
        scene.Pack(node);
        node.Free();
        return scene;
    }

    private static WfcTile Tile(int item, PackedScene marker, TileRotation rotation = TileRotation.None, string side = "a") => new()
    {
        Item = item, Rotation = rotation, Marker = marker,
        SocketPosX = side + "+", SocketNegX = side + "-", SocketPosY = "v", SocketNegY = "v", SocketPosZ = "b", SocketNegZ = "b",
    };

    private static TileSetBinding Binding(params WfcTile[] tiles)
    {
        var library = new MeshLibrary();
        for (int i = 0; i < 3; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        return TileSetBinding.Create(new WfcTileSet { Library = library, Tiles = new Array<WfcTile>(tiles) }, Directions.All3D);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Markers_follow_the_cell_and_its_rotation()
    {
        var lamp = Scene("Lamp");
        var binding = Binding(Tile(0, lamp, TileRotation.FourWay));
        var map = AutoFree(new GridMap { CellSize = new Vector3(2, 2, 2) });
        var grid = GridTopology.Create3D(new Vector3I(4, 1, 1));
        var origin = new Vector3I(3, 1, -2);
        var initial = Enumerable.Repeat(WfcSolver.Free, 4).ToArray();
        var solved = Enumerable.Range(0, 4).Select(turns => binding.Compiled.FindVariant(0, turns)).ToArray();

        var plan = GridMapMarkers.Plan(map, origin, grid, binding, initial, solved);
        AssertThat(plan.Count).IsEqual(4);
        for (int turns = 0; turns < 4; turns++)
        {
            var placement = plan[turns];
            var cell = origin + new Vector3I(turns, 0, 0);
            AssertThat(placement.Cell).IsEqual(cell);
            AssertThat(placement.Transform.Origin).IsEqual(map.MapToLocal(cell));
            var expected = map.GetBasisWithOrthogonalIndex(GridMapOrientation.FromQuarterTurns(turns));
            AssertThat(placement.Transform.Basis.IsEqualApprox(expected)).IsTrue();
        }

        var marker = GridMapMarkers.Instantiate(plan[1]);
        AssertThat(marker.HasMeta(GridMapMarkers.TagKey)).IsTrue();
        AssertThat(marker.Transform).IsEqual(plan[1].Transform);
        marker.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Empty_tiles_carry_markers_and_fixed_cells_get_none()
    {
        var spawn = Scene("Spawn");
        var binding = Binding(Tile(WfcTile.EmptyItem, spawn), Tile(1, spawn, side: "c"));
        var map = AutoFree(new GridMap());
        var grid = GridTopology.Create3D(new Vector3I(3, 1, 1));
        int empty = binding.Compiled.FindVariant(0, 0);
        var initial = new[] { WfcSolver.Free, binding.Compiled.FindVariant(1, 0), WfcSolver.Free };

        var plan = GridMapMarkers.Plan(map, Vector3I.Zero, grid, binding, initial, new[] { empty, initial[1], empty });
        AssertThat(plan.Select(p => p.Cell.X).ToArray()).IsEqual(new[] { 0, 2 });
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Only_tagged_markers_on_regenerated_cells_are_replaced()
    {
        var map = AutoFree(new GridMap());
        var container = new Node3D { Name = GridMapMarkers.ContainerName };
        map.AddChild(container);
        Node3D Add(Vector3I cell, bool tagged)
        {
            var node = new Node3D { Position = map.MapToLocal(cell) };
            if (tagged)
                node.SetMeta(GridMapMarkers.TagKey, true);
            container.AddChild(node);
            return node;
        }

        var regenerated = Add(new Vector3I(0, 0, 0), true);
        var moved = Add(new Vector3I(5, 0, 0), true);          // placed elsewhere, then moved into a free cell
        moved.Position = map.MapToLocal(new Vector3I(1, 0, 0));
        var onFixed = Add(new Vector3I(2, 0, 0), true);
        var users = Add(new Vector3I(1, 0, 0), false);
        var outside = Add(new Vector3I(9, 0, 0), true);

        var grid = GridTopology.Create3D(new Vector3I(3, 1, 1));
        var initial = new[] { WfcSolver.Free, WfcSolver.Free, 0 };
        var replaced = GridMapMarkers.Replaced(map, Vector3I.Zero, grid, initial);
        AssertThat(replaced.Count).IsEqual(2);
        AssertThat(replaced.Contains(regenerated) && replaced.Contains(moved)).IsTrue();
        AssertThat(replaced.Contains(onFixed) || replaced.Contains(users) || replaced.Contains(outside)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Markers_round_trip_on_tiles()
    {
        const string path = "user://wfc_marker_tile.tres";
        var tile = Tile(0, Scene("Lamp"));
        AssertThat(ResourceSaver.Save(tile, path)).IsEqual(Error.Ok);
        var loaded = ResourceLoader.Load<WfcTile>(path, null, ResourceLoader.CacheMode.Ignore);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        AssertThat(loaded.Marker).IsNotNull();
        AssertThat(Binding(loaded).Markers[0]).IsNotNull();
    }
}
