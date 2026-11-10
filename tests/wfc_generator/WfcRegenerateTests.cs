namespace Godoodads.Tests.WfcGenerator;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;
using static GdUnit4.Assertions;

// Brief §11.2: Generate replaces what the previous generation wrote and keeps everything else.
[TestSuite]
public class WfcRegenerateTests
{
    private static WfcTile Tile(int item) => new()
    {
        Item = item, SocketPosX = "a", SocketNegX = "a", SocketPosY = "v", SocketNegY = "v", SocketPosZ = "a", SocketNegZ = "a",
    };

    private static TileSetBinding Binding()
    {
        var library = new MeshLibrary();
        for (int i = 0; i < 3; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        var tileSet = new WfcTileSet { Library = library, Tiles = new Array<WfcTile>(new[] { Tile(0), Tile(1), Tile(2), Tile(WfcTile.EmptyItem) }) };
        return TileSetBinding.Create(tileSet, Directions.All3D);
    }

    private static GridMap Map(TileSetBinding binding) => AutoFree(new GridMap());

    [TestCase]
    [RequireGodotRuntime]
    public void Unchanged_generated_cells_are_free_and_edited_or_placed_cells_are_kept()
    {
        var binding = Binding();
        var map = Map(binding);
        var grid = GridTopology.Create3D(new Vector3I(4, 1, 1));
        var at = (int x) => new Vector3I(x, 0, 0);

        var record = new GeneratedRecord();
        record.Set(at(0), 0, 0); map.SetCellItem(at(0), 0, 0);   // generated, unchanged
        record.Set(at(1), 0, 0); map.SetCellItem(at(1), 1, 0);   // generated, then edited
        map.SetCellItem(at(2), 1, 0);                            // placed by hand
        record.Set(at(3), 0, 0);                                 // generated, then erased

        var read = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record);
        AssertThat(read.Replaced).IsEqual(1);
        AssertThat(read.Kept).IsEqual(2);
        AssertThat(read.Edited).IsEqual(1);
        AssertThat(read.Initial[0]).IsEqual(WfcSolver.Free);
        AssertThat(read.Initial[1]).IsEqual(binding.Compiled.FindVariant(1, 0));
        AssertThat(read.Initial[2]).IsEqual(binding.Compiled.FindVariant(1, 0));
        AssertThat(read.Initial[3]).IsEqual(WfcSolver.Free);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_new_record_keeps_cells_outside_the_bounds_and_skips_empty_tiles()
    {
        var binding = Binding();
        var grid = GridTopology.Create3D(new Vector3I(3, 1, 1));
        var previous = new GeneratedRecord();
        previous.Set(new Vector3I(1, 0, 0), 2, 0);   // inside: replaced
        previous.Set(new Vector3I(9, 0, 0), 2, 0);   // outside: kept

        int item0 = binding.Compiled.FindVariant(0, 0), empty = binding.Compiled.FindVariant(3, 0);
        var initial = new[] { WfcSolver.Free, WfcSolver.Free, binding.Compiled.FindVariant(1, 0) };
        var record = GridMapCells.RecordAfter(previous, Vector3I.Zero, grid, binding, initial, new[] { item0, empty, -1 });

        AssertThat(record.Count).IsEqual(2);
        AssertThat(record.Matches(new Vector3I(0, 0, 0), 0, 0)).IsTrue();
        AssertThat(record.Contains(new Vector3I(1, 0, 0))).IsFalse();   // now empty
        AssertThat(record.Contains(new Vector3I(2, 0, 0))).IsFalse();   // fixed, not written
        AssertThat(record.Matches(new Vector3I(9, 0, 0), 2, 0)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Generating_again_with_another_seed_gives_that_seeds_result()
    {
        var binding = Binding();
        var map = Map(binding);
        var grid = GridTopology.Create3D(new Vector3I(6, 2, 6));
        var placed = new Vector3I(2, 1, 3);
        map.SetCellItem(placed, 1, 0);

        GeneratedRecord Generate(long seed, GeneratedRecord record)
        {
            var read = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record);
            var result = new WfcSolver(grid, binding.Compiled, read.Initial).Solve(new SolveOptions { Seed = seed });
            GridMapCells.Apply(map, GridMapCells.Diff(map, Vector3I.Zero, grid, binding, read.Initial, result.Cells).Do);
            return GridMapCells.RecordAfter(record, Vector3I.Zero, grid, binding, read.Initial, result.Cells);
        }

        string Snapshot() => string.Join(",", Enumerable.Range(0, grid.CellCount).Select(c => map.GetCellItem(grid.Coordinates(c))));

        var first = Generate(1, new GeneratedRecord());
        var second = Generate(2, GeneratedRecord.Decode(first.Encode()));
        string regenerated = Snapshot();

        // The same seed on a map with only the placed cell.
        map.Clear();
        map.SetCellItem(placed, 1, 0);
        Generate(2, new GeneratedRecord());
        AssertThat(regenerated).IsEqual(Snapshot());
        AssertThat(map.GetCellItem(placed)).IsEqual(1);
        AssertThat(second.Contains(placed)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Undo_restores_the_previous_record()
    {
        // The same do and undo pair as the plugin's action, on a plain UndoRedo.
        var map = Map(null);
        var undo = new UndoRedo();
        var first = new GeneratedRecord();
        first.Set(Vector3I.One, 1, 0);
        foreach (var bytes in new[] { first.Encode(), new GeneratedRecord().Encode() })
        {
            var before = GridMapCells.StoredRecord(map);
            undo.CreateAction("Generate");
            undo.AddDoMethod(Callable.From(() => GridMapCells.WriteRecord(map, bytes)));
            undo.AddUndoMethod(Callable.From(() => GridMapCells.WriteRecord(map, before)));
            undo.CommitAction();
        }
        AssertThat(GridMapCells.ReadRecord(map).Count).IsEqual(0);

        undo.Undo();
        AssertThat(GridMapCells.ReadRecord(map).Matches(Vector3I.One, 1, 0)).IsTrue();
        undo.Undo();
        AssertThat(map.HasMeta(WfcSettings.GeneratedKey)).IsFalse();
        undo.Free();
    }

    [TestCase(Timeout = 120000)]
    [RequireGodotRuntime]
    public void The_record_of_a_full_size_demo_generation_is_small()
    {
        var tileSet = ResourceLoader.Load<WfcTileSet>("res://demo/wfc_generator/assets/ruin_tileset.tres");
        var grid = GridTopology.Create3D(new Vector3I(100, 16, 100));
        var binding = TileSetBinding.Create(tileSet, grid.ActiveDirections);
        var cells = GridMapCells.AllFree(grid);
        var result = new WfcSolver(grid, binding.Compiled, cells.Initial).Solve(new SolveOptions { Seed = 1 });
        AssertThat((int)result.Status).IsEqual((int)SolveStatus.Solved);

        var map = new GridMap { Name = "GridMap", MeshLibrary = tileSet.Library };
        GridMapCells.Apply(map, GridMapCells.Diff(map, Vector3I.Zero, grid, binding, cells.Initial, result.Cells).Do);
        var record = GridMapCells.RecordAfter(null, Vector3I.Zero, grid, binding, cells.Initial, result.Cells);

        long Saved(string path)
        {
            var scene = new PackedScene();
            scene.Pack(map);
            AssertThat(ResourceSaver.Save(scene, path)).IsEqual(Error.Ok);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            long length = (long)file.GetLength();
            file.Close();
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            return length;
        }

        long without = Saved("user://wfc_record_without.tscn");
        map.SetMeta(WfcSettings.GeneratedKey, record.Encode());
        long with = Saved("user://wfc_record_with.tscn");
        GD.Print($"WFC record: {record.Count} cells, {record.Encode().Length} bytes encoded, scene {without} -> {with} bytes (+{with - without}).");
        map.Free();
        AssertThat(with - without < 1024 * 1024).IsTrue();
    }
}
