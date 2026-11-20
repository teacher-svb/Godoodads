namespace Godoodads.Tests.Wave;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
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
    public void Cells_in_pin_boxes_stay_and_empty_ones_stay_empty()
    {
        var binding = Binding();
        var map = Map(binding);
        var grid = GridTopology.Create3D(new Vector3I(7, 1, 1));
        var at = (int x) => new Vector3I(x, 0, 0);

        // A first generation wrote item 0 on cells 0 to 5; cell 4 was edited and cell 5 erased since.
        var record = new GeneratedRecord();
        for (int x = 0; x < 6; x++)
        {
            record.Set(at(x), 0, 0);
            map.SetCellItem(at(x), 0, 0);
        }
        map.SetCellItem(at(4), 1, 0);
        map.SetCellItem(at(5), (int)GridMap.InvalidCellItem);
        // Two overlapping pin boxes over cells 2 to 6; one sticks out of the bounds.
        var boxes = new[] { new CellBox(at(2), new Vector3I(3, 1, 1)), new CellBox(at(4), new Vector3I(9, 1, 1)) };

        var read = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record, boxes);
        AssertThat(read.PinBoxCells).IsEqual(5);
        AssertThat(read.Replaced).IsEqual(2);
        AssertThat(read.Kept).IsEqual(3);
        AssertThat(read.Initial[0]).IsEqual(WfcSolver.Free);
        AssertThat(read.Initial[1]).IsEqual(WfcSolver.Free);
        AssertThat(read.Initial[2]).IsEqual(binding.Compiled.FindVariant(0, 0));   // generated, now pinned
        AssertThat(read.Initial[3]).IsEqual(binding.Compiled.FindVariant(0, 0));
        AssertThat(read.Initial[4]).IsEqual(binding.Compiled.FindVariant(1, 0));   // edited, pinned
        AssertThat(read.Initial[5]).IsEqual(WfcSolver.Excluded);                   // empty: stays empty
        AssertThat(read.Initial[6]).IsEqual(WfcSolver.Excluded);

        // Generated cells in the boxes stay generated in the record, so once the boxes go the next
        // Generate replaces them.
        var solved = Enumerable.Range(0, 7).Select(c => read.Initial[c] >= 0 ? read.Initial[c] : binding.Compiled.FindVariant(2, 0)).ToArray();
        var after = GridMapCells.RecordAfter(record, Vector3I.Zero, grid, binding, read.Initial, solved, boxes);
        AssertThat(after.Matches(at(0), 2, 0) && after.Matches(at(1), 2, 0)).IsTrue();
        AssertThat(after.Matches(at(2), 0, 0) && after.Matches(at(3), 0, 0)).IsTrue();
        GridMapCells.Apply(map, GridMapCells.Diff(map, Vector3I.Zero, grid, binding, read.Initial, solved).Do);
        var freed = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, after);
        AssertThat(freed.Replaced).IsEqual(4);   // cells 0 to 3
        AssertThat(freed.Initial[4]).IsEqual(binding.Compiled.FindVariant(1, 0));   // still edited: kept
        AssertThat(freed.PinBoxCells).IsEqual(0);

        // Old records may carry 1.2–1.4 pins: they no longer keep a cell.
        AssertThat(GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, GeneratedRecord.Decode(after.Encode())).Replaced).IsEqual(4);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_bounds_first_keeps_pin_box_cells_with_and_without_keep_placed_cells()
    {
        var binding = Binding();
        var map = Map(binding);
        var grid = GridTopology.Create3D(new Vector3I(5, 1, 1));
        var at = (int x) => new Vector3I(x, 0, 0);

        var record = new GeneratedRecord();
        record.Set(at(0), 0, 0); map.SetCellItem(at(0), 0, 0);   // generated, unchanged
        record.Set(at(1), 0, 0); map.SetCellItem(at(1), 0, 0);   // generated, in a pin box
        record.Set(at(2), 0, 0); map.SetCellItem(at(2), 1, 0);   // generated, then edited
        map.SetCellItem(at(3), 2, 0);                            // placed by hand
        var boxes = new[] { new CellBox(at(1), Vector3I.One), new CellBox(at(4), Vector3I.One) };   // cell 4 is empty

        var keep = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record, boxes, keepPlaced: true);
        AssertThat(keep.Replaced).IsEqual(1);
        AssertThat(keep.Edited).IsEqual(1);
        AssertThat(keep.Kept).IsEqual(3);
        AssertThat(keep.PinBoxCells).IsEqual(2);
        AssertThat(keep.Initial[0]).IsEqual(WfcSolver.Free);
        AssertThat(keep.Initial[1]).IsEqual(binding.Compiled.FindVariant(0, 0));
        AssertThat(keep.Initial[2]).IsEqual(binding.Compiled.FindVariant(1, 0));
        AssertThat(keep.Initial[3]).IsEqual(binding.Compiled.FindVariant(2, 0));
        AssertThat(keep.Initial[4]).IsEqual(WfcSolver.Excluded);

        var all = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record, boxes, keepPlaced: false);
        AssertThat(all.Replaced).IsEqual(3);
        AssertThat(all.Kept).IsEqual(1);
        AssertThat(all.Initial[0] == WfcSolver.Free && all.Initial[2] == WfcSolver.Free && all.Initial[3] == WfcSolver.Free).IsTrue();
        AssertThat(all.Initial[1]).IsEqual(binding.Compiled.FindVariant(0, 0));
        AssertThat(all.Initial[4]).IsEqual(WfcSolver.Excluded);

        // Without pin boxes nothing survives Clear Bounds First with Keep Placed Cells off.
        AssertThat(GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding, record, keepPlaced: false).Initial.All(c => c == WfcSolver.Free)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_freed_cell_leaves_no_entry_where_the_new_solve_is_empty()
    {
        var binding = Binding();
        var grid = GridTopology.Create3D(new Vector3I(3, 1, 1));
        var at = (int x) => new Vector3I(x, 0, 0);
        var previous = new GeneratedRecord();
        previous.Set(at(0), 0, 0);   // freed and solved empty
        previous.Set(at(1), 0, 0);   // freed and solved as item 2
        previous.Set(at(2), 0, 0);   // in a pin box and fixed

        int item0 = binding.Compiled.FindVariant(0, 0), item2 = binding.Compiled.FindVariant(2, 0), empty = binding.Compiled.FindVariant(3, 0);
        var initial = new[] { WfcSolver.Free, WfcSolver.Free, item0 };
        var record = GridMapCells.RecordAfter(previous, Vector3I.Zero, grid, binding, initial, new[] { empty, item2, item0 },
            new[] { new CellBox(at(2), Vector3I.One) });

        AssertThat(record.Contains(at(0))).IsFalse();
        AssertThat(record.Matches(at(1), 2, 0)).IsTrue();
        AssertThat(record.Matches(at(2), 0, 0)).IsTrue();
    }

    private static PropertyUsageFlags Usage(WfcSettings settings, string name) =>
        (PropertyUsageFlags)settings.GetPropertyList().First(p => p["name"].AsString() == name)["usage"].AsInt64();

    [TestCase]
    [RequireGodotRuntime]
    public void The_seed_is_read_only_while_randomise_seed_is_on()
    {
        var settings = new WfcSettings { Seed = 7 };
        AssertThat(Usage(settings, WfcSettings.PropertyName.Seed).HasFlag(PropertyUsageFlags.ReadOnly)).IsFalse();
        settings.RandomiseSeed = true;
        var usage = Usage(settings, WfcSettings.PropertyName.Seed);
        AssertThat(usage.HasFlag(PropertyUsageFlags.ReadOnly) && usage.HasFlag(PropertyUsageFlags.Editor) && usage.HasFlag(PropertyUsageFlags.Storage)).IsTrue();

        settings.PickNewSeed();
        AssertThat(settings.Seed).IsNotEqual(7L);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_new_seed_avoids_the_seeds_that_failed()
    {
        // Randi has 2^32 values; avoiding all but two of a narrow guess is not testable, so check
        // that the avoided set and the current seed are never returned over many picks.
        var settings = new WfcSettings { Seed = 5 };
        var failed = new System.Collections.Generic.HashSet<long>();
        for (int i = 0; i < 200; i++)
        {
            long current = settings.Seed;
            settings.PickNewSeed(failed);
            AssertThat(settings.Seed).IsNotEqual(current);
            AssertThat(failed.Contains(settings.Seed)).IsFalse();
            failed.Add(settings.Seed);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Keep_placed_cells_is_shown_also_while_clear_bounds_first_is_off()
    {
        // It also applies to the Clear Bounds button.
        var settings = new WfcSettings { ClearBoundsFirst = false };
        AssertThat(Usage(settings, WfcSettings.PropertyName.KeepPlacedCells).HasFlag(PropertyUsageFlags.Editor)).IsTrue();
    }

    private static (GridMap Map, GridTopology Grid, GeneratedRecord Record) ClearScene()
    {
        var map = AutoFree(new GridMap());
        var grid = GridTopology.Create3D(new Vector3I(5, 1, 1));
        var at = (int x) => new Vector3I(x, 0, 0);
        var record = new GeneratedRecord();
        record.Set(at(0), 0, 0); map.SetCellItem(at(0), 0, 0);                 // generated, unchanged
        record.Set(at(1), 0, 0); map.SetCellItem(at(1), 0, 0);                 // generated, unchanged
        record.Set(at(2), 0, 0); map.SetCellItem(at(2), 1, 22);                // generated, then edited
        map.SetCellItem(at(3), 2, 10);                                         // placed by hand
        record.Set(new Vector3I(9, 0, 0), 0, 0);                               // outside the bounds
        return (map, grid, record);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_bounds_removes_every_cell_and_undo_puts_them_back()
    {
        var (map, grid, record) = ClearScene();
        var cleared = GridMapCells.Clear(map, Vector3I.Zero, grid, record, keepPlaced: false);
        AssertThat(cleared.Cleared).IsEqual(4);
        AssertThat(cleared.Kept).IsEqual(0);
        AssertThat(cleared.PinBoxCells).IsEqual(0);
        AssertThat(cleared.Initial.All(c => c == WfcSolver.Free)).IsTrue();

        GridMapCells.Apply(map, cleared.Changes.Do);
        AssertThat(map.GetUsedCells().Count).IsEqual(0);
        GridMapCells.Apply(map, cleared.Changes.Undo);
        AssertThat(map.GetCellItem(new Vector3I(2, 0, 0))).IsEqual(1);
        AssertThat(map.GetCellItemOrientation(new Vector3I(2, 0, 0))).IsEqual(22);
        AssertThat(map.GetCellItemOrientation(new Vector3I(3, 0, 0))).IsEqual(10);
        AssertThat(map.GetUsedCells().Count).IsEqual(4);

        var after = GridMapCells.RecordAfterClear(record, Vector3I.Zero, grid.Size);
        AssertThat(after.Count).IsEqual(1);
        AssertThat(after.Contains(new Vector3I(9, 0, 0))).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_bounds_with_keep_placed_cells_keeps_placed_and_edited_cells()
    {
        var (map, grid, record) = ClearScene();
        var cleared = GridMapCells.Clear(map, Vector3I.Zero, grid, record, keepPlaced: true);
        AssertThat(cleared.Cleared).IsEqual(2);
        AssertThat(cleared.Kept).IsEqual(2);
        AssertThat(cleared.Initial[0]).IsEqual(WfcSolver.Free);
        AssertThat(cleared.Initial[2]).IsEqual(WfcSolver.Excluded);
        AssertThat(cleared.Initial[3]).IsEqual(WfcSolver.Excluded);

        GridMapCells.Apply(map, cleared.Changes.Do);
        AssertThat(map.GetCellItem(new Vector3I(0, 0, 0))).IsEqual((int)GridMap.InvalidCellItem);
        AssertThat(map.GetCellItem(new Vector3I(1, 0, 0))).IsEqual((int)GridMap.InvalidCellItem);
        AssertThat(map.GetCellItem(new Vector3I(2, 0, 0))).IsEqual(1);
        AssertThat(map.GetCellItem(new Vector3I(3, 0, 0))).IsEqual(2);

        // Without a record, every cell counts as placed by you.
        var unrecorded = GridMapCells.Clear(map, Vector3I.Zero, grid, new GeneratedRecord(), keepPlaced: true);
        AssertThat(unrecorded.Cleared).IsEqual(0);
        AssertThat(unrecorded.Kept).IsEqual(2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_bounds_keeps_pin_box_cells_in_both_modes()
    {
        var (map, grid, record) = ClearScene();
        var boxes = new[] { new CellBox(new Vector3I(1, 0, 0), Vector3I.One), new CellBox(new Vector3I(4, 0, 0), Vector3I.One) };

        var all = GridMapCells.Clear(map, Vector3I.Zero, grid, record, keepPlaced: false, boxes);
        AssertThat(all.Cleared).IsEqual(3);
        AssertThat(all.PinBoxCells).IsEqual(2);
        AssertThat(all.Initial[1]).IsEqual(WfcSolver.Excluded);
        AssertThat(all.Initial[4]).IsEqual(WfcSolver.Excluded);

        var keep = GridMapCells.Clear(map, Vector3I.Zero, grid, record, keepPlaced: true, boxes);
        AssertThat(keep.Cleared).IsEqual(1);
        AssertThat(keep.Kept).IsEqual(2);
        AssertThat(keep.PinBoxCells).IsEqual(2);

        GridMapCells.Apply(map, all.Changes.Do);
        AssertThat(map.GetCellItem(new Vector3I(1, 0, 0))).IsEqual(0);
        AssertThat(map.GetUsedCells().Count).IsEqual(1);

        // The record keeps the pin box cell's entry, so it stays generated.
        var after = GridMapCells.RecordAfterClear(record, Vector3I.Zero, grid.Size, boxes);
        AssertThat(after.Count).IsEqual(2);
        AssertThat(after.Matches(new Vector3I(1, 0, 0), 0, 0)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_bounds_removes_the_markers_on_cleared_cells_only()
    {
        var (map, grid, record) = ClearScene();
        var container = new Node3D { Name = GridMapMarkers.ContainerName };
        map.AddChild(container);
        Node3D Marker(int x)
        {
            var marker = new Node3D { Position = map.MapToLocal(new Vector3I(x, 0, 0)) };
            marker.SetMeta(GridMapMarkers.TagKey, true);
            container.AddChild(marker);
            return marker;
        }
        var onGenerated = Marker(0);
        Marker(2);   // on the edited cell, which stays

        var cleared = GridMapCells.Clear(map, Vector3I.Zero, grid, record, keepPlaced: true);
        var markers = GridMapMarkers.Replaced(map, Vector3I.Zero, grid, cleared.Initial);
        AssertThat(markers.Count).IsEqual(1);
        AssertThat(markers[0] == onGenerated).IsTrue();
        AssertThat(GridMapMarkers.Replaced(map, Vector3I.Zero, grid, GridMapCells.Clear(map, Vector3I.Zero, grid, record, false).Initial).Count).IsEqual(2);
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

    [TestCase]
    [RequireGodotRuntime]
    public void Undoing_a_first_generation_leaves_no_settings_entry()
    {
        // The same do and undo pair as the plugin's action.
        var map = Map(null);
        var undo = new UndoRedo();
        var settings = new WfcSettings { Seed = 7 };
        var before = WfcSettings.StoredOn(map);
        undo.CreateAction("Generate");
        undo.AddDoMethod(Callable.From(() => WfcSettings.StoreOn(map, settings)));
        undo.AddUndoMethod(Callable.From(() => WfcSettings.StoreOn(map, before)));
        undo.CommitAction();
        AssertThat(WfcSettings.StoredOn(map).Seed).IsEqual(7L);

        undo.Undo();
        AssertThat(map.HasMeta(WfcSettings.MetaKey)).IsFalse();
        undo.Redo();
        AssertThat(WfcSettings.StoredOn(map).Seed).IsEqual(7L);
        undo.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void An_empty_settings_entry_from_1_1_reads_as_none()
    {
        var map = Map(null);
        // As the 1.1 undo did: the entry existed, then the property was set to null.
        map.SetMeta(WfcSettings.MetaKey, new WfcSettings());
        map.Set("metadata/" + WfcSettings.MetaKey, default(Variant));
        AssertThat(map.HasMeta(WfcSettings.MetaKey)).IsTrue();
        AssertThat(WfcSettings.StoredOn(map)).IsNull();
    }

    [TestCase(Timeout = 120000)]
    [RequireGodotRuntime]
    public void The_record_of_a_full_size_demo_generation_is_small()
    {
        var tileSet = ResourceLoader.Load<WfcTileSet>("res://demo/godoowave/assets/ruin_tileset.tres");
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
