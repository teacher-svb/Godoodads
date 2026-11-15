namespace Godoodads.Tests.Dungeon;

using System.Diagnostics;
using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Adapters;
using Godoodads.Dungeon.Core;
using static GdUnit4.Assertions;

/// <summary>The godoodungeon demo scenes (card dgn-m4-demo-scenes, brief §8 reliability).</summary>
[TestSuite]
public class DungeonDemoTests
{
    private const int Seeds = 20;

    [TestCase("dungeon_free")]
    [TestCase("dungeon_obstacles")]
    [RequireGodotRuntime]
    public void The_demo_scene_generates_at_least_19_of_20_seeds(string name)
    {
        var root = ResourceLoader.Load<PackedScene>($"res://demo/godoodungeon/{name}.tscn").Instantiate();
        var map = root.GetNode<GridMap>("GridMap");
        var settings = DungeonSettings.StoredOn(map);
        AssertThat(settings).IsNotNull();
        AssertThat(settings.BoundsSize).IsEqual(new Vector3I(64, 1, 64));

        var check = DungeonTileSets.Check(settings, map);
        AssertThat(string.Join("; ", check.Errors.Concat(check.Warnings))).IsEqual("");
        var handPlaced = DungeonGridMap.ReadHandPlaced(map, settings.BoundsOrigin, settings.BoundsSize, DungeonGridMap.ReadRecord(map));
        AssertThat(handPlaced.Count).IsEqual(name == "dungeon_obstacles" ? 5 + 24 : 0);

        int solved = 0, rooms = 0, attempts = 0;
        var clock = Stopwatch.StartNew();
        for (long seed = 1; seed <= Seeds; seed++)
        {
            settings.Seed = seed;
            var result = new DungeonJob(DungeonGridMap.Request(settings, check, handPlaced)).RunNow();
            if (result.Status != DungeonStatus.Solved)
            {
                GD.Print($"{name} seed {seed}: {result.Status} {string.Join(" | ", result.Errors.Concat(result.Attempts.Select(a => a.Message)))}");
                continue;
            }
            solved++;
            rooms += result.Layout.Rooms.Count;
            attempts += result.LayoutAttempt + 1;
            foreach (var cell in handPlaced)
                AssertThat(DungeonGridMap.Cells(result, check.Room, check.Corridor).ContainsKey(cell)).IsFalse();
        }
        GD.Print($"{name}: {solved} of {Seeds} seeds solved, {rooms / (double)System.Math.Max(solved, 1):F1} rooms on average, "
            + $"{attempts - solved} extra layout attempts, {clock.ElapsedMilliseconds / Seeds} ms per dungeon.");
        AssertThat(solved >= 19).IsTrue();
        root.Free();
    }

    /// <summary>Brief §8 performance, with the GridMap write (card dgn-m5-performance).</summary>
    [TestCase(64, 20)]
    [TestCase(128, 60)]
    [RequireGodotRuntime]
    public void Generate_and_write_timings(int size, int rooms)
    {
        var root = ResourceLoader.Load<PackedScene>("res://demo/godoodungeon/dungeon_free.tscn").Instantiate();
        var map = root.GetNode<GridMap>("GridMap");
        var settings = (DungeonSettings)DungeonSettings.StoredOn(map).Duplicate();
        settings.BoundsSize = new Vector3I(size, 1, size);
        settings.RoomCount = rooms;
        var check = DungeonTileSets.Check(settings, map);
        double generate = 0, write = 0;
        int cells = 0;
        const int Runs = 5;
        for (long seed = 1; seed <= Runs; seed++)
        {
            settings.Seed = seed;
            var clock = Stopwatch.StartNew();
            var record = DungeonGridMap.ReadRecord(map);
            var handPlaced = DungeonGridMap.ReadHandPlaced(map, settings.BoundsOrigin, settings.BoundsSize, record);
            var result = new DungeonJob(DungeonGridMap.Request(settings, check, handPlaced)).RunNow();
            generate += clock.Elapsed.TotalMilliseconds;
            AssertThat(result.Status).IsEqual(DungeonStatus.Solved);
            clock.Restart();
            var (changes, newRecord) = DungeonGridMap.Write(map, record, DungeonGridMap.Cells(result, check.Room, check.Corridor));
            GridMapCells.Apply(map, changes.Do);
            DungeonGridMap.WriteRecord(map, newRecord.Encode());
            write += clock.Elapsed.TotalMilliseconds;
            cells = newRecord.Count;
        }
        GD.Print($"{size}×{size}, {rooms} rooms (demo tile sets): generate {generate / Runs:F0} ms, write {write / Runs:F0} ms per dungeon "
            + $"(last one {cells} cells; each run replaces the previous dungeon).");
        if (size == 64)
            AssertThat(generate / Runs + write / Runs < 10_000).IsTrue();
        root.Free();
    }
}
