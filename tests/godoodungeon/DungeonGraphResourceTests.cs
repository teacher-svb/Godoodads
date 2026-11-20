namespace Godoodads.Tests.Dungeon;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.Dungeon;
using Godoodads.Dungeon.Core;
using static GdUnit4.Assertions;

/// <summary>The DungeonGraph resources and their GDScript API (brief godoodungeon §12.2, §12.3; card dgn-1-1-graph-resource).</summary>
[TestSuite]
public class DungeonGraphResourceTests
{
    private static readonly LayoutSettings Settings = new() { Bounds = new Vector2I(40, 40), RoomCount = 8 };

    /// <summary>The graph of a laid-out and routed dungeon; no tiles or solve needed.</summary>
    private static DungeonGraphData Core(long seed)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            var layout = LayoutGenerator.Generate(Settings, seed, attempt, new Vector3I(-6, 2, 4));
            if (!layout.IsValid)
                continue;
            var plan = CorridorRouter.Route(layout, Settings, null, 0.3, LayoutSeeds.Corridors(seed, attempt));
            if (plan.IsValid)
                return DungeonGraphData.Build(layout, plan, 2);
        }
        throw new System.InvalidOperationException($"seed {seed} gave no routed layout");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_resource_holds_the_core_graph()
    {
        var core = Core(3);
        var graph = DungeonGraph.From(core);
        AssertThat(graph.Rooms.Count).IsEqual(core.Rooms.Count);
        AssertThat(graph.Corridors.Count).IsEqual(core.Corridors.Count);
        AssertThat(graph.Origin).IsEqual(core.Origin);
        for (int r = 0; r < core.Rooms.Count; r++)
        {
            var room = graph.Rooms[r];
            AssertThat(room.Index).IsEqual(r);
            AssertThat(room.Number).IsEqual(r + 1);
            AssertThat(room.Position).IsEqual(core.Rooms[r].Position);
            AssertThat(room.Size).IsEqual(core.Rooms[r].Size);
            AssertThat(room.Size.Y).IsEqual(2);
            AssertThat(room.DoorCells.Count).IsEqual(core.Rooms[r].Doors.Count);
            AssertThat(room.Corridors).IsEqual(core.Rooms[r].Corridors.ToArray());
            foreach (var (cell, step) in room.DoorCells.Zip(room.DoorDirections))
                AssertThat(room.HasCell(cell) && !room.HasCell(cell + step)).IsTrue();
        }
        foreach (var corridor in graph.Corridors)
        {
            var source = core.Corridors[corridor.Index];
            AssertThat(corridor.Cells.ToArray()).IsEqual(source.Cells.ToArray());
            AssertThat(corridor.Length).IsEqual(source.Length);
            AssertThat(graph.Rooms[corridor.RoomA].DoorCells.Contains(corridor.DoorA)).IsTrue();
            AssertThat(graph.Rooms[corridor.RoomB].DoorCells.Contains(corridor.DoorB)).IsTrue();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_graph_stored_on_a_gridmap_survives_a_save_and_reload_of_the_scene()
    {
        var core = Core(5);
        var graph = DungeonGraph.From(core);
        graph.Rooms[2].Data["role"] = "boss";
        graph.Corridors[1].Data["locked"] = true;

        var root = new Node3D { Name = "Root" };
        var map = new GridMap { Name = "GridMap" };
        root.AddChild(map);
        map.Owner = root;
        AssertThat(DungeonGraph.StoredOn(map)).IsNull();
        DungeonGraph.StoreOn(map, graph);
        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string path = "user://dungeon_graph_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, path)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var loadedMap = loaded.GetNode<GridMap>("GridMap");
        var stored = DungeonGraph.StoredOn(loadedMap);
        AssertThat(stored).IsNotNull();
        AssertThat(stored.Origin).IsEqual(core.Origin);
        AssertThat(stored.Size).IsEqual(core.Size);
        AssertThat(stored.Rooms.Count).IsEqual(core.Rooms.Count);
        AssertThat(stored.Corridors.Count).IsEqual(core.Corridors.Count);
        AssertThat(stored.Rooms[2].Data["role"].AsString()).IsEqual("boss");
        AssertThat(stored.Corridors[1].Data["locked"].AsBool()).IsTrue();
        for (int i = 0; i < core.Corridors.Count; i++)
        {
            AssertThat(stored.Corridors[i].Cells.ToArray()).IsEqual(core.Corridors[i].Cells.ToArray());
            AssertThat(stored.Corridors[i].IsTreeEdge).IsEqual(core.Corridors[i].IsTreeEdge);
        }

        // The rebuilt Core graph answers like the original.
        var rebuilt = stored.CoreGraph;
        AssertThat(rebuilt.Doors.Count).IsEqual(core.Doors.Count);
        for (int i = 0; i < core.Corridors.Count; i++)
        {
            AssertThat(rebuilt.Doors[rebuilt.Corridors[i].DoorA].Cell).IsEqual(core.Doors[core.Corridors[i].DoorA].Cell);
            AssertThat(rebuilt.Doors[rebuilt.Corridors[i].DoorB].Side).IsEqual(core.Doors[core.Corridors[i].DoorB].Side);
        }
        for (int r = 0; r < core.Rooms.Count; r++)
        {
            AssertThat(stored.Distances(r)).IsEqual(core.Distances(r));
            AssertThat(stored.HopDistances(r)).IsEqual(core.HopDistances(r));
        }
        AssertThat(stored.Diameter()).IsEqual(new Vector2I(core.Diameter().A, core.Diameter().B));
        for (int z = -1; z <= 40; z++)
            for (int x = -1; x <= 40; x++)
            {
                var cell = core.Origin + new Vector3I(x, 0, z);
                AssertThat(stored.RoomAtCell(cell)).IsEqual(core.RoomAtCell(cell));
                AssertThat(stored.CorridorsAtCell(cell)).IsEqual(core.CorridorsAtCell(cell));
            }

        DungeonGraph.StoreOn(loadedMap, null);
        AssertThat(loadedMap.HasMeta(DungeonGraph.MetaKey)).IsFalse();
        loaded.Free();
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Every_query_answers_the_same_through_variant_calls()
    {
        var core = Core(7);
        var graph = DungeonGraph.From(core);
        var map = new GridMap();
        int last = core.Rooms.Count - 1;
        var cell = core.Corridors[0].Cells[2];

        AssertThat(graph.Call(DungeonGraph.MethodName.HopDistances, 0).AsInt32Array()).IsEqual(core.HopDistances(0));
        AssertThat(graph.Call(DungeonGraph.MethodName.Distances, 1).AsInt32Array()).IsEqual(core.Distances(1));
        AssertThat(graph.Call(DungeonGraph.MethodName.ShortestPath, 0, last).AsInt32Array()).IsEqual(core.ShortestPath(0, last));
        AssertThat(graph.Call(DungeonGraph.MethodName.ShortestPathCorridors, 0, last).AsInt32Array()).IsEqual(core.ShortestPathCorridors(0, last));
        AssertThat(graph.Call(DungeonGraph.MethodName.FarthestRoom, 0).AsInt32()).IsEqual(core.FarthestRoom(0));
        AssertThat(graph.Call(DungeonGraph.MethodName.Diameter).AsVector2I()).IsEqual(new Vector2I(core.Diameter().A, core.Diameter().B));
        AssertThat(graph.Call(DungeonGraph.MethodName.Neighbours, 0).AsInt32Array()).IsEqual(core.Neighbours(0));
        AssertThat(graph.Call(DungeonGraph.MethodName.CorridorBetween, core.Corridors[0].RoomA, core.Corridors[0].RoomB).AsInt32()).IsEqual(0);
        AssertThat(graph.Call(DungeonGraph.MethodName.RoomAtCell, core.Rooms[1].CentreCell).AsInt32()).IsEqual(1);
        AssertThat(graph.Call(DungeonGraph.MethodName.CorridorsAtCell, cell).AsInt32Array()).IsEqual(core.CorridorsAtCell(cell));
        AssertThat(graph.Call(DungeonGraph.MethodName.RoomAtPosition, map, graph.RoomCentre(map, 1)).AsInt32()).IsEqual(1);
        AssertThat(graph.Call(DungeonGraph.MethodName.RoomCentre, map, 1).AsVector3()).IsEqual(graph.RoomCentre(map, 1));
        AssertThat(graph.Rooms[0].Call(DungeonRoom.MethodName.HasCell, core.Rooms[0].Position).AsBool()).IsTrue();
        AssertThat(graph.Rooms[0].Get(DungeonRoom.PropertyName.Number).AsInt32()).IsEqual(1);
        AssertThat(graph.Corridors[0].Get(DungeonCorridor.PropertyName.Length).AsInt32()).IsEqual(core.Corridors[0].Length);
        AssertThat(graph.Corridors[0].Call(DungeonCorridor.MethodName.Other, core.Corridors[0].RoomA).AsInt32()).IsEqual(core.Corridors[0].RoomB);
        map.Free();
    }

    private const string Script = @"extends RefCounted

func run(map: GridMap) -> Dictionary:
	var graph = map.get_meta(&""godoodads_dungeon_graph"", null)
	var ends: Vector2i = graph.Diameter()
	var first = graph.Rooms[0]
	first.Data[""role""] = ""start""
	return {
		""rooms"": graph.Rooms.size(),
		""corridors"": graph.Corridors.size(),
		""ends"": ends,
		""path"": graph.ShortestPath(ends.x, ends.y),
		""path_corridors"": graph.ShortestPathCorridors(ends.x, ends.y),
		""hops"": graph.HopDistances(ends.x),
		""lengths"": graph.Distances(ends.x),
		""farthest"": graph.FarthestRoom(0),
		""neighbours"": graph.Neighbours(0),
		""room_at_centre"": graph.RoomAtCell(first.CentreCell),
		""room_at_position"": graph.RoomAtPosition(map, graph.RoomCentre(map, 0)),
		""corridors_at"": graph.CorridorsAtCell(graph.Corridors[0].Cells[0]),
		""number"": first.Number,
		""length"": graph.Corridors[0].Length,
		""door_a"": graph.Corridors[0].DoorA,
	}
";

    [TestCase]
    [RequireGodotRuntime]
    public void Gdscript_reads_the_graph_from_the_gridmap_and_gets_the_csharp_answers()
    {
        var core = Core(9);
        var graph = DungeonGraph.From(core);
        var map = new GridMap();
        DungeonGraph.StoreOn(map, graph);
        var script = new GDScript { SourceCode = Script };
        AssertThat(script.Reload()).IsEqual(Error.Ok);
        var runner = (GodotObject)script.New();
        var result = runner.Call("run", map).AsGodotDictionary();

        var (a, b) = core.Diameter();
        AssertThat(result["rooms"].AsInt32()).IsEqual(core.Rooms.Count);
        AssertThat(result["corridors"].AsInt32()).IsEqual(core.Corridors.Count);
        AssertThat(result["ends"].AsVector2I()).IsEqual(new Vector2I(a, b));
        AssertThat(result["path"].AsInt32Array()).IsEqual(core.ShortestPath(a, b));
        AssertThat(result["path_corridors"].AsInt32Array()).IsEqual(core.ShortestPathCorridors(a, b));
        AssertThat(result["hops"].AsInt32Array()).IsEqual(core.HopDistances(a));
        AssertThat(result["lengths"].AsInt32Array()).IsEqual(core.Distances(a));
        AssertThat(result["farthest"].AsInt32()).IsEqual(core.FarthestRoom(0));
        AssertThat(result["neighbours"].AsInt32Array()).IsEqual(core.Neighbours(0));
        AssertThat(result["room_at_centre"].AsInt32()).IsEqual(0);
        AssertThat(result["room_at_position"].AsInt32()).IsEqual(0);
        AssertThat(result["corridors_at"].AsInt32Array()).IsEqual(core.CorridorsAtCell(core.Corridors[0].Cells[0]));
        AssertThat(result["number"].AsInt32()).IsEqual(1);
        AssertThat(result["length"].AsInt32()).IsEqual(core.Corridors[0].Length);
        AssertThat(result["door_a"].AsVector3I()).IsEqual(core.Doors[core.Corridors[0].DoorA].Cell);
        AssertThat(graph.Rooms[0].Data["role"].AsString()).IsEqual("start");
        map.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Positions_find_their_room_on_a_moved_rotated_and_scaled_gridmap()
    {
        var core = Core(11);
        var graph = DungeonGraph.From(core);
        var root = new Node3D { Position = new Vector3(4, -1, 2), Rotation = new Vector3(0, 0.3f, 0) };
        var map = new GridMap
        {
            CellSize = new Vector3(2, 3, 1.5f),
            CellCenterY = false,
            Position = new Vector3(-7, 2, 11),
            Rotation = new Vector3(0, 1.1f, 0),
            Scale = new Vector3(1.5f, 1.5f, 1.5f),
        };
        root.AddChild(map);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(root);
        try
        {
            foreach (var room in graph.Rooms)
            {
                var centre = graph.RoomCentre(map, room.Index);
                AssertThat(graph.RoomAtPosition(map, centre)).IsEqual(room.Index);
                AssertThat(graph.RoomAtPosition(map, centre + map.GlobalBasis.Y * 2.9f)).IsEqual(room.Index);
                foreach (var cell in new[] { room.Position, room.Position + new Vector3I(room.Size.X - 1, 0, room.Size.Z - 1) })
                {
                    var global = map.ToGlobal(map.MapToLocal(cell));
                    AssertThat(graph.RoomAtPosition(map, global)).IsEqual(room.Index);
                }
                var outside = map.ToGlobal(map.MapToLocal(room.Position - new Vector3I(1, 0, 0)));
                AssertThat(graph.RoomAtPosition(map, outside)).IsNotEqual(room.Index);
            }
            AssertThat(graph.RoomAtPosition(map, map.ToGlobal(map.MapToLocal(core.Origin - Vector3I.One)))).IsEqual(-1);
            AssertThat(graph.RoomCentre(map, 99)).IsEqual(Vector3.Zero);
        }
        finally
        {
            root.QueueFree();
        }
    }
}
