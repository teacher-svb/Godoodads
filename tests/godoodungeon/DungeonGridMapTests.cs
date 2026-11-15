namespace Godoodads.Tests.Dungeon;

using System.Collections.Generic;
using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Adapters;
using Godoodads.Dungeon.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class DungeonGridMapTests
{
    private const int HandItem = 7;

    private static DungeonSettings Settings(long seed, PackedScene cornerMarker = null)
    {
        var settings = DungeonEngineTiles.Valid(cornerMarker: cornerMarker);
        settings.BoundsOrigin = new Vector3I(-4, 0, 6);
        settings.BoundsSize = new Vector3I(32, 1, 32);
        settings.RoomCount = 5;
        settings.Seed = seed;
        return settings;
    }

    private sealed record Generated(CellChanges Changes, DungeonResult Result, byte[] PreviousRecord, byte[] Record);

    /// <summary>Generate as the dialog will: read the record and hand-placed cells, run the job, write.</summary>
    private static Generated Generate(GridMap map, DungeonSettings settings)
    {
        var tileSets = DungeonTileSets.Check(settings, map);
        AssertThat(string.Join("; ", tileSets.Errors)).IsEqual("");
        var previousBytes = DungeonGridMap.StoredRecord(map);
        var previous = GeneratedRecord.Decode(previousBytes);
        var handPlaced = DungeonGridMap.ReadHandPlaced(map, settings.BoundsOrigin, settings.BoundsSize, previous);
        var result = new DungeonJob(DungeonGridMap.Request(settings, tileSets, handPlaced)).RunNow();
        AssertThat(result.Status == DungeonStatus.Solved ? "" : $"{result.Status}: {string.Join(" | ", result.Errors.Concat(result.Attempts.Select(a => a.Message)))}").IsEqual("");
        var cells = DungeonGridMap.Cells(result, tileSets.Room, tileSets.Corridor);
        var (changes, record) = DungeonGridMap.Write(map, previous, cells);
        GridMapCells.Apply(map, changes.Do);
        var bytes = record.Encode();
        DungeonGridMap.WriteRecord(map, bytes);
        return new Generated(changes, result, previousBytes, bytes);
    }

    private static string Snapshot(GridMap map) => string.Join(";", map.GetUsedCells().Select(c => (Vector3I)c)
        .OrderBy(c => c.Y).ThenBy(c => c.Z).ThenBy(c => c.X).Select(c => $"{c}={map.GetCellItem(c)}/{map.GetCellItemOrientation(c)}"));

    private static GridMap Map(DungeonSettings settings) => new() { MeshLibrary = settings.RoomTileSet.Library };

    [TestCase]
    [RequireGodotRuntime]
    public void Generate_writes_rooms_doors_and_corridors_and_records_them()
    {
        var settings = Settings(1);
        var map = Map(settings);
        var generated = Generate(map, settings);
        var record = DungeonGridMap.ReadRecord(map);

        AssertThat(record.Count > 0).IsTrue();
        AssertThat(record.Count).IsEqual(map.GetUsedCells().Count);
        foreach (var (cell, written) in record.Cells)
            AssertThat(map.GetCellItem(cell)).IsEqual(written.Item);
        AssertThat(record.Cells.Count(c => c.Value.Item == DungeonEngineTiles.Door)).IsEqual(generated.Result.Layout.Doors.Count);
        AssertThat(record.Cells.Count(c => c.Value.Item == DungeonEngineTiles.Corner)).IsEqual(4 * generated.Result.Layout.Rooms.Count);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Hand_placed_cells_are_unchanged()
    {
        var settings = Settings(2);
        var map = Map(settings);
        var hand = new[] { new Vector3I(0, 0, 10), new Vector3I(10, 0, 20), new Vector3I(20, 0, 30), new Vector3I(-10, 0, 0) /* outside */ };
        foreach (var cell in hand)
            map.SetCellItem(cell, HandItem);
        var generated = Generate(map, settings);
        foreach (var cell in hand)
            AssertThat(map.GetCellItem(cell)).IsEqual(HandItem);
        foreach (var room in generated.Result.Layout.Rooms)
            foreach (var cell in hand)
                AssertThat(room.Contains(new Vector2I(cell.X - settings.BoundsOrigin.X, cell.Z - settings.BoundsOrigin.Z))).IsFalse();
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Regenerate_replaces_only_recorded_cells_and_keeps_edits()
    {
        var settings = Settings(3);
        var map = Map(settings);
        Generate(map, settings);
        var first = DungeonGridMap.ReadRecord(map);

        var edited = first.Cells.First(c => c.Value.Item == DungeonEngineTiles.Wall).Key;
        map.SetCellItem(edited, HandItem);
        var added = new Vector3I(1, 0, 7);
        map.SetCellItem(added, HandItem);

        settings.Seed = 4;
        Generate(map, settings);
        var second = DungeonGridMap.ReadRecord(map);

        AssertThat(map.GetCellItem(edited)).IsEqual(HandItem);
        AssertThat(map.GetCellItem(added)).IsEqual(HandItem);
        AssertThat(second.Contains(edited)).IsFalse();
        foreach (Vector3I cell in map.GetUsedCells())
            AssertThat(second.Contains(cell) || cell == edited || cell == added).IsTrue();
        foreach (var (cell, _) in first.Cells)
            if (!second.Contains(cell) && cell != edited)
                AssertThat(map.GetCellItem(cell)).IsEqual((int)GridMap.InvalidCellItem);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Undo_restores_every_cell_and_redo_reapplies()
    {
        var settings = Settings(5);
        var map = Map(settings);
        map.SetCellItem(new Vector3I(2, 0, 9), HandItem);
        Generate(map, settings);              // an earlier dungeon to replace
        settings.Seed = 6;
        var before = Snapshot(map);
        var generated = Generate(map, settings);
        var after = Snapshot(map);

        GridMapCells.Apply(map, generated.Changes.Undo);
        DungeonGridMap.WriteRecord(map, generated.PreviousRecord);
        AssertThat(Snapshot(map)).IsEqual(before);

        GridMapCells.Apply(map, generated.Changes.Do);
        DungeonGridMap.WriteRecord(map, generated.Record);
        AssertThat(Snapshot(map)).IsEqual(after);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_mesh_library_and_tile_sets_are_unchanged()
    {
        var settings = Settings(7);
        string Describe()
        {
            var library = settings.RoomTileSet.Library;
            var items = string.Join(",", library.GetItemList().Select(i => $"{i}:{library.GetItemName(i)}:{library.GetItemMesh(i)?.GetInstanceId()}"));
            var tiles = string.Join("|", settings.RoomTileSet.Tiles.Concat(settings.CorridorTileSet.Tiles)
                .Select(t => $"{t.ResourceName}:{t.Item}:{t.Rotation}:{t.Weight}:{t.SocketPosX},{t.SocketNegX},{t.SocketPosZ},{t.SocketNegZ}"));
            return items + "#" + tiles;
        }
        string before = Describe();
        var map = Map(settings);
        Generate(map, settings);
        AssertThat(Describe()).IsEqual(before);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Clear_dungeon_removes_recorded_cells_and_keeps_edits_and_hand_placed()
    {
        var settings = Settings(8);
        var map = Map(settings);
        map.SetCellItem(new Vector3I(0, 0, 8), HandItem);
        Generate(map, settings);
        var record = DungeonGridMap.ReadRecord(map);
        var edited = record.Cells.First().Key;
        map.SetCellItem(edited, HandItem);

        var clear = DungeonGridMap.Clear(map, record);
        GridMapCells.Apply(map, clear.Do);
        var used = map.GetUsedCells().Select(c => (Vector3I)c).ToHashSet();
        AssertThat(used.Count).IsEqual(2);
        AssertThat(used.Contains(edited)).IsTrue();
        AssertThat(used.Contains(new Vector3I(0, 0, 8))).IsTrue();

        GridMapCells.Apply(map, clear.Undo);
        AssertThat(map.GetUsedCells().Count).IsEqual(record.Count + 1);
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Markers_are_planned_for_marker_tiles_and_replaced_on_regenerate()
    {
        var markerRoot = new Node3D { Name = "Torch" };
        var scene = new PackedScene();
        scene.Pack(markerRoot);
        markerRoot.Free();

        var settings = Settings(9, scene);
        var map = Map(settings);
        var tileSets = DungeonTileSets.Check(settings, map);
        var generated = Generate(map, settings);
        var placements = DungeonGridMap.PlanMarkers(map, generated.Result, tileSets.Room, tileSets.Corridor);
        AssertThat(placements.Count).IsEqual(4 * generated.Result.Layout.Rooms.Count);

        var container = new Node3D { Name = DungeonGridMap.MarkerContainerName };
        map.AddChild(container);
        foreach (var placement in placements)
        {
            var marker = DungeonGridMap.InstantiateMarker(placement);
            AssertThat(marker.HasMeta(DungeonGridMap.MarkerTag)).IsTrue();
            container.AddChild(marker);
        }
        var untagged = new Node3D { Position = map.MapToLocal(new Vector3I(0, 0, 0)) };
        container.AddChild(untagged);

        var replaced = DungeonGridMap.ReplacedMarkers(map, DungeonGridMap.ReadRecord(map), new HashSet<Vector3I>());
        AssertThat(replaced.Count).IsEqual(placements.Count);
        AssertThat(replaced.Contains(untagged)).IsFalse();
        map.Free();
    }
}
