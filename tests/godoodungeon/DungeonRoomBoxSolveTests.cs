namespace Godoodads.Tests.Dungeon;

using System.Collections.Generic;
using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Adapters;
using Godoodads.Dungeon.Core;
using static GdUnit4.Assertions;

/// <summary>Room boxes through solve, write, Clear Dungeon and the graph (brief godoodungeon §13.3, §13.4; card dgn-1-2-solve-write).</summary>
[TestSuite]
public class DungeonRoomBoxSolveTests
{
    private const int Filler = 7;   // a hand-placed item no tile set uses

    private static readonly Vector3I[] BossDoors = { new(32, 0, 28), new(28, 0, 31) };

    private sealed record Scene(GridMap Map, DungeonSettings Settings, DungeonRoomBox Entrance, DungeonRoomBox Boss, Dictionary<Vector3I, int> BossCells);

    /// <summary>A GridMap with a Generated entrance box and a Built boss room: filler cells and two Door tiles.</summary>
    private static Scene Build(int roomCount = 10)
    {
        var settings = DungeonEngineTiles.Valid();
        settings.BoundsOrigin = Vector3I.Zero;
        settings.BoundsSize = new Vector3I(64, 1, 64);   // the brief's target size (§3)
        settings.RoomCount = roomCount;
        var map = new GridMap { MeshLibrary = settings.RoomTileSet.Library };
        var entrance = new DungeonRoomBox { Name = "Entrance", Origin = new Vector3I(2, 0, 2), Size = new Vector3I(6, 1, 5) };
        var boss = new DungeonRoomBox { Name = "Boss", Mode = RoomBoxMode.Built, Origin = new Vector3I(28, 0, 28), Size = new Vector3I(8, 1, 8) };
        boss.Data["role"] = "boss";
        map.AddChild(entrance);
        map.AddChild(boss);
        var cells = new Dictionary<Vector3I, int>();
        for (int x = 28; x < 36; x++)
            for (int z = 28; z < 36; z++)
            {
                var cell = new Vector3I(x, 0, z);
                int item = BossDoors.Contains(cell) ? settings.Door : Filler;
                map.SetCellItem(cell, item);
                cells[cell] = item;
            }
        return new Scene(map, settings, entrance, boss, cells);
    }

    /// <summary>Generate as the dialog does: record, hand-placed cells, room boxes, job, write.</summary>
    private static DungeonResult Generate(Scene scene, long seed)
    {
        scene.Settings.Seed = seed;
        var check = DungeonTileSets.Check(scene.Settings, scene.Map);
        AssertThat(string.Join("; ", check.Errors)).IsEqual("");
        var previous = DungeonGridMap.ReadRecord(scene.Map);
        var handPlaced = DungeonGridMap.ReadHandPlaced(scene.Map, scene.Settings.BoundsOrigin, scene.Settings.BoundsSize, previous);
        var boxes = DungeonGridMap.ReadRoomBoxes(scene.Map, scene.Settings.BoundsOrigin, scene.Settings.Door);
        var result = new DungeonJob(DungeonGridMap.Request(scene.Settings, check, handPlaced, boxes)).RunNow();
        AssertThat(result.Status == DungeonStatus.Solved ? "" : $"seed {seed}: {result.Status}: {string.Join(" | ", result.Errors.Concat(result.Attempts.Select(a => a.Message)))}").IsEqual("");
        var cells = DungeonGridMap.Cells(result, check.Room, check.Corridor);
        var (changes, record) = DungeonGridMap.Write(scene.Map, previous, cells);
        GridMapCells.Apply(scene.Map, changes.Do);
        DungeonGridMap.WriteRecord(scene.Map, record.Encode());
        return result;
    }

    private static void AssertBossUnchanged(Scene scene)
    {
        foreach (var (cell, item) in scene.BossCells)
            AssertThat(scene.Map.GetCellItem(cell)).IsEqual(item);
    }

    [TestCase(Timeout = 120000)]
    [RequireGodotRuntime]
    public void Over_50_seeds_with_10_rooms_both_boxes_are_rooms_1_and_2_and_every_built_door_gets_a_corridor()
    {
        var scene = Build();
        for (long seed = 1; seed <= 50; seed++)
        {
            var result = Generate(scene, seed);   // each one regenerates over the last
            var layout = result.Layout;
            AssertThat(layout.Rooms.Count).IsEqual(10);
            AssertThat(layout.Rooms[0]).IsEqual(new RoomRect(new Vector2I(2, 2), new Vector2I(6, 5)));
            AssertThat(layout.Rooms[1]).IsEqual(new RoomRect(new Vector2I(28, 28), new Vector2I(8, 8)));
            var bossDoors = Enumerable.Range(0, layout.Doors.Count).Where(d => layout.Doors[d].Room == 1).ToList();
            AssertThat(bossDoors.Count).IsEqual(2);
            foreach (int door in bossDoors)
                AssertThat(layout.EdgeDoors.Any(e => e.A == door || e.B == door)).IsTrue();
            // The connectivity check passed (Solved), and nothing was written into the boss room.
            AssertThat(result.Solves.Count(s => !s.IsCorridor)).IsEqual(9);
            AssertThat(DungeonGridMap.Cells(result, DungeonTileSets.Check(scene.Settings, scene.Map).Room,
                DungeonTileSets.Check(scene.Settings, scene.Map).Corridor).Keys.Any(c => scene.BossCells.ContainsKey(c))).IsFalse();
            AssertBossUnchanged(scene);
        }
        scene.Map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Regenerate_and_clear_dungeon_keep_the_built_room_and_replace_the_generated_box()
    {
        var scene = Build();
        Generate(scene, 1);
        var record = DungeonGridMap.ReadRecord(scene.Map);
        var entranceCell = new Vector3I(2, 0, 2);   // a corner of the Generated box
        AssertThat(record.Contains(entranceCell)).IsTrue();
        AssertThat(scene.BossCells.Keys.Any(record.Contains)).IsFalse();

        Generate(scene, 2);
        AssertBossUnchanged(scene);
        AssertThat(DungeonGridMap.ReadRecord(scene.Map).Contains(entranceCell)).IsTrue();

        var clear = DungeonGridMap.Clear(scene.Map, DungeonGridMap.ReadRecord(scene.Map));
        GridMapCells.Apply(scene.Map, clear.Do);
        AssertBossUnchanged(scene);
        AssertThat(scene.Map.GetCellItem(entranceCell)).IsEqual((int)GridMap.InvalidCellItem);
        scene.Map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_graph_names_each_box_and_carries_its_data_after_regenerating()
    {
        var scene = Build();
        DungeonGraph graph = null;
        foreach (long seed in new long[] { 3, 4 })
        {
            var result = Generate(scene, seed);
            graph = DungeonGridMap.Graph(result, scene.Map);
            AssertThat(graph.Rooms[0].Box.ToString()).IsEqual("Entrance");
            AssertThat(graph.Rooms[0].IsBuilt).IsFalse();
            AssertThat(graph.Rooms[1].Box.ToString()).IsEqual("Boss");
            AssertThat(graph.Rooms[1].IsBuilt).IsTrue();
            AssertThat(graph.Rooms[1].Data["role"].AsString()).IsEqual("boss");
            AssertThat(graph.Rooms[2].Box.IsEmpty).IsTrue();
            AssertThat(graph.RoomAtCell(new Vector3I(30, 0, 30))).IsEqual(1);
            AssertThat(graph.Rooms[1].Size.Y).IsEqual(1);
        }

        // The copy is the room's own: changing it leaves the box alone.
        graph.Rooms[1].Data["visited"] = true;
        AssertThat(scene.Boss.Data.ContainsKey("visited")).IsFalse();

        // The Data warning leaves out Data copied from a box, unless it changed since.
        AssertThat(DungeonGridMap.EntriesWithData(graph, scene.Map)).IsEqual(1);
        graph.Rooms[1].Data.Remove("visited");
        AssertThat(DungeonGridMap.EntriesWithData(graph, scene.Map)).IsEqual(0);
        AssertThat(DungeonGridMap.EntriesWithData(graph)).IsEqual(1);   // without the GridMap it counts

        // The rebuilt Core graph keeps the box names after a save.
        var copy = (DungeonGraph)graph.Duplicate(true);
        AssertThat(copy.CoreGraph.Rooms[1].Box).IsEqual("Boss");
        AssertThat(copy.CoreGraph.Rooms[1].IsBuilt).IsTrue();
        scene.Map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_disabled_built_box_is_an_obstacle_of_cells_you_placed()
    {
        var scene = Build(5);
        scene.Boss.Enabled = false;
        var result = Generate(scene, 5);
        AssertThat(result.Layout.Boxes.Count).IsEqual(1);
        AssertThat(result.Layout.Rooms.Count).IsEqual(5);
        AssertThat(result.Layout.Rooms.Skip(1).Any(r => r.Overlaps(new RoomRect(new Vector2I(28, 28), new Vector2I(8, 8))))).IsFalse();
        AssertBossUnchanged(scene);
        scene.Map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Room_boxes_are_read_from_the_gridmap_children()
    {
        var scene = Build();
        var boxes = DungeonGridMap.ReadRoomBoxes(scene.Map, new Vector3I(-1, 0, 0), scene.Settings.Door);
        AssertThat(boxes.Select(b => b.Name).ToArray()).IsEqual(new[] { "Entrance", "Boss" });
        AssertThat(boxes[0].Rect).IsEqual(new RoomRect(new Vector2I(3, 2), new Vector2I(6, 5)));   // relative to the bounds origin
        AssertThat(boxes[0].DoorCells.Count).IsEqual(0);
        AssertThat(boxes[1].IsBuilt).IsTrue();
        AssertThat(boxes[1].DoorCells.ToArray()).IsEqual(new[] { new Vector2I(33, 28), new Vector2I(29, 31) });
        AssertThat(RoomBoxes.BuiltDoors(boxes[1]).Count).IsEqual(2);
        scene.Map.Free();
    }
}
