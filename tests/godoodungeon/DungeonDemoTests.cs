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

    /// <summary>The graph demo (brief §12.5, §12.6 items 5 and 6; card dgn-1-1-demo).</summary>
    [TestCase]
    [RequireGodotRuntime]
    public void The_graph_demo_reads_its_baked_graph_and_marks_the_way()
    {
        var root = ResourceLoader.Load<PackedScene>("res://demo/godoodungeon/dungeon_graph.tscn", null, ResourceLoader.CacheMode.Ignore).Instantiate<Node3D>();
        var map = root.GetNode<GridMap>("GridMap");
        var graph = DungeonGraph.StoredOn(map);
        AssertThat(graph).IsNotNull();
        AssertThat(graph.Rooms.Count).IsEqual(12);

        // The baked graph is the one the stored settings generate.
        var settings = DungeonSettings.StoredOn(map);
        var check = DungeonTileSets.Check(settings, map);
        var result = new DungeonJob(DungeonGridMap.Request(settings, check, new System.Collections.Generic.HashSet<Vector3I>())).RunNow();
        AssertThat(result.Status).IsEqual(DungeonStatus.Solved);
        AssertThat(result.Graph.Corridors.Count).IsEqual(graph.Corridors.Count);
        for (int i = 0; i < graph.Corridors.Count; i++)
            AssertThat(graph.Corridors[i].Cells.ToArray()).IsEqual(result.Graph.Corridors[i].Cells.ToArray());
        foreach (var room in graph.Rooms)
            AssertThat(graph.RoomAtCell(room.CentreCell)).IsEqual(room.Index);

        // Running the scene runs graph_demo.gd: start and goal get their role, and every room plus every path cell a marker.
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(root);
        var ends = graph.Diameter();
        AssertThat(graph.Rooms[ends.X].Data["role"].AsString()).IsEqual("start");
        AssertThat(graph.Rooms[ends.Y].Data["role"].AsString()).IsEqual("goal");
        int pathCells = graph.ShortestPathCorridors(ends.X, ends.Y).Sum(c => graph.Corridors[c].Length);
        AssertThat(root.GetNode("GraphMarkers").GetChildCount()).IsEqual(graph.Rooms.Count + pathCells);
        root.QueueFree();
    }

    /// <summary>The room box demo (brief §13.6; card dgn-1-2-demo).</summary>
    [TestCase]
    [RequireGodotRuntime]
    public void The_room_box_demo_keeps_its_boss_room_joins_both_boxes_and_the_script_heads_for_the_boss()
    {
        var root = ResourceLoader.Load<PackedScene>("res://demo/godoodungeon/dungeon_rooms.tscn", null, ResourceLoader.CacheMode.Ignore).Instantiate<Node3D>();
        var map = root.GetNode<GridMap>("GridMap");
        var settings = DungeonSettings.StoredOn(map);
        var boxes = DungeonGridMap.ReadRoomBoxes(map, settings.BoundsOrigin, settings.Door);
        AssertThat(boxes.Select(b => b.Name).ToArray()).IsEqual(new[] { "Entrance", "BossRoom" });
        AssertThat(boxes[1].IsBuilt).IsTrue();
        var graph = DungeonGraph.StoredOn(map);
        AssertThat(graph.Rooms[1].Box.ToString()).IsEqual("BossRoom");
        AssertThat(graph.Rooms[1].Data["role"].AsString()).IsEqual("boss");

        var bossCells = new System.Collections.Generic.Dictionary<Vector3I, (int, int)>();
        var bossBox = map.GetNode<DungeonRoomBox>("BossRoom");
        for (int x = 0; x < bossBox.Size.X; x++)
            for (int z = 0; z < bossBox.Size.Z; z++)
            {
                var cell = bossBox.Origin + new Vector3I(x, 0, z);
                bossCells[cell] = (map.GetCellItem(cell), map.GetCellItemOrientation(cell));
            }
        AssertThat(bossCells.Values.Count(v => v.Item1 == settings.Door)).IsEqual(2);

        var check = DungeonTileSets.Check(settings, map);
        foreach (long seed in new long[] { 11, 12, 13 })
        {
            settings.Seed = seed;
            var record = DungeonGridMap.ReadRecord(map);
            var handPlaced = DungeonGridMap.ReadHandPlaced(map, settings.BoundsOrigin, settings.BoundsSize, record);
            var result = new DungeonJob(DungeonGridMap.Request(settings, check, handPlaced, boxes)).RunNow();
            AssertThat(result.Status).IsEqual(DungeonStatus.Solved);
            var layout = result.Layout;
            var bossDoors = Enumerable.Range(0, layout.Doors.Count).Where(d => layout.Doors[d].Room == 1).ToList();
            AssertThat(bossDoors.Count).IsEqual(2);
            AssertThat(bossDoors.All(d => layout.EdgeDoors.Any(e => e.A == d || e.B == d))).IsTrue();
            AssertThat(layout.Doors.Any(d => d.Room == 0)).IsTrue();
            var (changes, written) = DungeonGridMap.Write(map, record, DungeonGridMap.Cells(result, check.Room, check.Corridor));
            GridMapCells.Apply(map, changes.Do);
            DungeonGridMap.WriteRecord(map, written.Encode());
            foreach (var (cell, value) in bossCells)
                AssertThat((map.GetCellItem(cell), map.GetCellItemOrientation(cell))).IsEqual(value);
        }

        // Running the scene: graph_demo.gd takes the boss room as its goal.
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(root);
        AssertThat(graph.Rooms[1].Data["role"].AsString()).IsEqual("boss");
        AssertThat(graph.Rooms.Count(r => r.Data.ContainsKey("role") && r.Data["role"].AsString() == "start")).IsEqual(1);
        int start = graph.Rooms.First(r => r.Data.ContainsKey("role") && r.Data["role"].AsString() == "start").Index;
        AssertThat(start).IsEqual(graph.FarthestRoom(1));
        root.QueueFree();
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
