namespace Godoodads.Tests.WfcGenerator;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class WfcAdapterTests
{
    private static WfcTile Tile(int item, string side, TileRotation rotation = TileRotation.None) => new()
    {
        Item = item, Rotation = rotation,
        SocketPosX = side, SocketNegX = side, SocketPosY = "v", SocketNegY = "v", SocketPosZ = side, SocketNegZ = side,
    };

    private static MeshLibrary Library(int items)
    {
        var library = new MeshLibrary();
        for (int i = 0; i < items; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        return library;
    }

    private static WfcTileSet TileSet(params WfcTile[] tiles) => new() { Library = Library(4), Tiles = new Array<WfcTile>(tiles) };

    private static GridMap Map(MeshLibrary library = null) => AutoFree(new GridMap { MeshLibrary = library ?? Library(4) });

    [TestCase]
    [RequireGodotRuntime]
    public void Orientation_matches_the_gridmap_basis_for_every_quarter_turn()
    {
        var map = Map();
        for (int turns = 0; turns < 4; turns++)
        {
            int orientation = GridMapOrientation.FromQuarterTurns(turns);
            AssertThat(GridMapOrientation.ToQuarterTurns(orientation)).IsEqual(turns);
            var basis = map.GetBasisWithOrthogonalIndex(orientation);
            foreach (int direction in Directions.All3D)
            {
                var rotated = (basis * (Vector3)Directions.Offset(direction)).Round();
                AssertThat(rotated).IsEqual((Vector3)Directions.Offset(Directions.RotateY(direction, turns)));
            }
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Non_y_orientations_are_rejected() =>
        AssertThat(GridMapOrientation.ToQuarterTurns(1)).IsEqual(-1);

    [TestCase]
    [RequireGodotRuntime]
    public void Tile_set_survives_save_and_load()
    {
        var set = TileSet(Tile(0, "a", TileRotation.FourWay), Tile(WfcTile.EmptyItem, "b"));
        const string path = "user://wfc_tileset_test.tres";
        AssertThat((int)ResourceSaver.Save(set, path)).IsEqual((int)Error.Ok);
        var loaded = ResourceLoader.Load<WfcTileSet>(path, null, ResourceLoader.CacheMode.Ignore);
        DirAccess.RemoveAbsolute(path);

        AssertThat(loaded.Tiles.Count).IsEqual(2);
        AssertThat(loaded.Tiles[0].Rotation).IsEqual(TileRotation.FourWay);
        AssertThat(loaded.Tiles[0].SocketPosX).IsEqual("a");
        AssertThat(loaded.Tiles[1].Item).IsEqual(WfcTile.EmptyItem);
        AssertThat(loaded.Library.GetItemList().Length).IsEqual(4);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Binding_reports_items_and_sockets_that_are_wrong()
    {
        var binding = TileSetBinding.Create(TileSet(Tile(9, "a"), Tile(1, "a"), Tile(1, "a"), Tile(2, "")), Directions.All3D);
        AssertThat(binding.IsValid).IsFalse();
        AssertThat(binding.Errors.Any(e => e.Contains("item 9"))).IsTrue();
        AssertThat(binding.Errors.Any(e => e.Contains("more than one tile"))).IsTrue();
        AssertThat(binding.Errors.Any(e => e.Contains("Tile 3"))).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Binding_requires_a_library()
    {
        var set = new WfcTileSet { Tiles = new Array<WfcTile> { Tile(WfcTile.EmptyItem, "a") } };
        AssertThat(TileSetBinding.Create(set, Directions.All3D).Errors.Any(e => e.Contains("MeshLibrary"))).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compiling_does_not_modify_the_library()
    {
        var set = TileSet(Tile(0, "a", TileRotation.FourWay));
        var before = set.Library.GetItemList();
        TileSetBinding.Create(set, Directions.All3D);
        AssertThat(set.Library.GetItemList()).IsEqual(before);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_maps_known_cells_and_ignores_unknown_ones()
    {
        var binding = TileSetBinding.Create(TileSet(Tile(0, "a", TileRotation.FourWay), Tile(1, "b")), Directions.All3D);
        var map = Map();
        var origin = new Vector3I(10, 2, -3);
        var grid = GridTopology.Create3D(new Vector3I(4, 1, 1));

        map.SetCellItem(origin, 1, 0);                                   // known tile
        map.SetCellItem(origin + new Vector3I(1, 0, 0), 3, 0);           // item without a tile
        map.SetCellItem(origin + new Vector3I(2, 0, 0), 1, 16);          // rotated, but the tile cannot rotate
        map.SetCellItem(origin + new Vector3I(5, 0, 0), 1, 0);           // outside the bounds

        var read = GridMapCells.ReadFixed(map, origin, grid, binding);
        AssertThat(read.Kept).IsEqual(1);
        AssertThat(read.Ignored).IsEqual(2);
        AssertThat(read.Initial[0]).IsEqual(binding.Compiled.FindVariant(1, 0));
        AssertThat(read.Initial[1]).IsEqual(WfcSolver.Excluded);
        AssertThat(read.Initial[2]).IsEqual(WfcSolver.Excluded);
        AssertThat(read.Initial[3]).IsEqual(WfcSolver.Free);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Diff_writes_free_cells_and_apply_restores_them()
    {
        var binding = TileSetBinding.Create(TileSet(
            new WfcTile { Item = 0, Rotation = TileRotation.FourWay, SocketPosX = "a", SocketNegX = "b", SocketPosY = "v", SocketNegY = "v", SocketPosZ = "c", SocketNegZ = "d" },
            Tile(WfcTile.EmptyItem, "e")), Directions.All3D);
        var map = Map();
        var grid = GridTopology.Create3D(new Vector3I(4, 1, 1));
        var origin = new Vector3I(1, 0, 0);
        var at = (int x) => origin + new Vector3I(x, 0, 0);

        map.SetCellItem(at(1), 2, 0);  // replaced by an empty tile
        map.SetCellItem(at(3), 3, 0);  // not free, must stay

        var initial = new[] { WfcSolver.Free, WfcSolver.Free, WfcSolver.Free, WfcSolver.Excluded };
        int turned = binding.Compiled.FindVariant(0, 1);
        int empty = binding.Compiled.FindVariant(1, 0);
        var solved = new[] { turned, empty, empty, -1 };

        var changes = GridMapCells.Diff(map, origin, grid, binding, initial, solved);
        AssertThat(changes.Do.Length).IsEqual(2 * CellChanges.Stride);

        GridMapCells.Apply(map, changes.Do);
        AssertThat(map.GetCellItem(at(0))).IsEqual(0);
        AssertThat(map.GetCellItemOrientation(at(0))).IsEqual(GridMapOrientation.FromQuarterTurns(1));
        AssertThat(map.GetCellItem(at(1))).IsEqual((int)GridMap.InvalidCellItem);
        AssertThat(map.GetCellItem(at(3))).IsEqual(3);

        GridMapCells.Apply(map, changes.Undo);
        AssertThat(map.GetCellItem(at(0))).IsEqual((int)GridMap.InvalidCellItem);
        AssertThat(map.GetCellItem(at(1))).IsEqual(2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Fixed_cells_survive_a_full_solve()
    {
        var binding = TileSetBinding.Create(TileSet(Tile(0, "a"), Tile(1, "a"), Tile(WfcTile.EmptyItem, "a")), Directions.All3D);
        var map = Map();
        var grid = GridTopology.Create3D(new Vector3I(5, 3, 5));
        map.SetCellItem(new Vector3I(2, 1, 2), 1, 0);

        var read = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding);
        var result = new WfcSolver(grid, binding.Compiled, read.Initial).Solve(new SolveOptions { Seed = 4 });
        GridMapCells.Apply(map, GridMapCells.Diff(map, Vector3I.Zero, grid, binding, read.Initial, result.Cells).Do);

        AssertThat(read.Kept).IsEqual(1);
        AssertThat(map.GetCellItem(new Vector3I(2, 1, 2))).IsEqual(1);
    }

    // A tile written by 1.0, before the Weight field existed.
    private const string LegacyTile = """
        [gd_resource type="Resource" script_class="WfcTile" format=3]

        [ext_resource type="Script" path="res://addons/wfc_generator/Resources/WfcTile.cs" id="1"]

        [resource]
        script = ExtResource("1")
        Item = 2
        SocketPosX = "solid"
        """;

    [TestCase]
    [RequireGodotRuntime]
    public void Tiles_saved_by_1_0_load_with_weight_one()
    {
        const string path = "user://wfc_legacy_tile.tres";
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
            file.StoreString(LegacyTile);

        var tile = ResourceLoader.Load<WfcTile>(path, null, ResourceLoader.CacheMode.Ignore);
        AssertThat(tile.Item).IsEqual(2);
        AssertThat(tile.Weight).IsEqual(1f);
        AssertThat(tile.ToDefinition().Weight).IsEqual(1.0);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Saved_weights_round_trip()
    {
        const string path = "user://wfc_weighted_tile_set.tres";
        var tileSet = TileSet(Tile(0, "a"), Tile(1, "a"));
        tileSet.Tiles[0].Weight = 2.5f;
        tileSet.Tiles[1].Weight = 0f;
        AssertThat(ResourceSaver.Save(tileSet, path)).IsEqual(Error.Ok);

        var loaded = ResourceLoader.Load<WfcTileSet>(path, null, ResourceLoader.CacheMode.Ignore);
        AssertThat(loaded.Tiles[0].Weight).IsEqual(2.5f);
        AssertThat(loaded.Tiles[1].Weight).IsEqual(0f);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Weight_errors_name_the_tile()
    {
        var bad = Tile(0, "a");
        bad.Weight = -1f;
        var binding = TileSetBinding.Create(TileSet(Tile(1, "a"), bad), Directions.All3D);
        AssertThat(binding.IsValid).IsFalse();
        AssertThat(binding.Errors[0]).StartsWith("Tile 1: Weight");
    }
}
