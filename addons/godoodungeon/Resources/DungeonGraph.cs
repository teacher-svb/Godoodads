using System.Linq;
using Godot;
using Godot.Collections;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon;

/// <summary>
/// The graph of a generated dungeon (brief godoodungeon §12): <see cref="Rooms"/> are the vertices,
/// <see cref="Corridors"/> (one per room graph edge) the edges. Generate stores it on the GridMap
/// under <see cref="MetaKey"/>, so gameplay code can read it at runtime without the plugin. Every
/// query uses Variant types, so GDScript calls it like C# does:
/// <code>var graph: DungeonGraph = grid_map.get_meta(&amp;"godoodads_dungeon_graph", null)</code>
/// Room indices are 0-based; the room numbers people read are <see cref="DungeonRoom.Number"/>.
/// </summary>
[Tool, GlobalClass]
public partial class DungeonGraph : Resource
{
    /// <summary>Metadata key under which a GridMap stores the graph of its last dungeon.</summary>
    public const string MetaKey = "godoodads_dungeon_graph";

    /// <summary>The GridMap cell of the dungeon bounds' origin.</summary>
    [Export] public Vector3I Origin { get; set; }

    /// <summary>The dungeon bounds on X and Z (Y is the GridMap's Z).</summary>
    [Export] public Vector2I Size { get; set; }

    [Export] public Array<DungeonRoom> Rooms { get; set; } = new();

    [Export] public Array<DungeonCorridor> Corridors { get; set; } = new();

    private DungeonGraphData _data;

    /// <summary>The graph stored on a GridMap, or null when it has none (no dungeon, or one generated before 1.1).</summary>
    public static DungeonGraph StoredOn(GridMap map) =>
        map.HasMeta(MetaKey) ? map.GetMeta(MetaKey).As<DungeonGraph>() : null;

    /// <summary>Stores a graph on a GridMap. Null removes the entry, so undo leaves nothing behind.</summary>
    public static void StoreOn(GridMap map, DungeonGraph graph)
    {
        if (graph == null)
            map.RemoveMeta(MetaKey);
        else
            map.SetMeta(MetaKey, graph);
    }

    /// <summary>The resource for a Core graph, with empty <see cref="DungeonRoom.Data"/> and <see cref="DungeonCorridor.Data"/>.</summary>
    public static DungeonGraph From(DungeonGraphData data)
    {
        var graph = new DungeonGraph { Origin = data.Origin, Size = data.Size };
        foreach (var room in data.Rooms)
            graph.Rooms.Add(new DungeonRoom
            {
                Index = room.Index,
                Position = room.Position,
                Size = room.Size,
                DoorCells = new Array<Vector3I>(room.Doors.Select(d => data.Doors[d].Cell)),
                DoorDirections = new Array<Vector3I>(room.Doors.Select(d => Step(data.Doors[d].Side))),
                Corridors = room.Corridors.ToArray(),
            });
        foreach (var corridor in data.Corridors)
            graph.Corridors.Add(new DungeonCorridor
            {
                Index = corridor.Index,
                RoomA = corridor.RoomA,
                RoomB = corridor.RoomB,
                DoorA = corridor.DoorA >= 0 ? data.Doors[corridor.DoorA].Cell : Vector3I.Zero,
                DoorB = corridor.DoorB >= 0 ? data.Doors[corridor.DoorB].Cell : Vector3I.Zero,
                IsTreeEdge = corridor.IsTreeEdge,
                Cells = new Array<Vector3I>(corridor.Cells),
            });
        graph._data = data;
        return graph;
    }

    /// <summary>
    /// The Core graph the queries run on, built from the exported properties on first use (after a
    /// scene loads). Changing rooms, doors or cells afterwards is not picked up; the <c>Data</c> dictionaries do not matter. C# only.
    /// </summary>
    public DungeonGraphData CoreGraph => _data ??= Build();

    private DungeonGraphData Build()
    {
        var doors = new System.Collections.Generic.List<GraphDoor>();
        var rooms = new System.Collections.Generic.List<GraphRoom>();
        for (int r = 0; r < Rooms.Count; r++)
        {
            var room = Rooms[r];
            var indices = new System.Collections.Generic.List<int>();
            for (int d = 0; d < room.DoorCells.Count; d++)
            {
                indices.Add(doors.Count);
                doors.Add(new GraphDoor(r, room.DoorCells[d], Side(d < room.DoorDirections.Count ? room.DoorDirections[d] : Vector3I.Zero)));
            }
            rooms.Add(new GraphRoom(r, room.Position, room.Size, indices, room.Corridors ?? System.Array.Empty<int>()));
        }
        int DoorAt(int room, Vector3I cell) =>
            room >= 0 && room < rooms.Count ? rooms[room].Doors.FirstOrDefault(d => doors[d].Cell == cell, -1) : -1;
        var corridors = Corridors.Select((c, i) => new GraphCorridor(i, c.RoomA, c.RoomB, DoorAt(c.RoomA, c.DoorA), DoorAt(c.RoomB, c.DoorB),
            c.IsTreeEdge, c.Cells.ToArray())).ToList();
        return new DungeonGraphData(Origin, Size, rooms, doors, corridors);
    }

    private static Vector3I Step(DoorSide side)
    {
        var step = side.Offset();
        return new Vector3I(step.X, 0, step.Y);
    }

    private static DoorSide Side(Vector3I step) =>
        step.X > 0 ? DoorSide.PosX : step.X < 0 ? DoorSide.NegX : step.Z > 0 ? DoorSide.PosY : DoorSide.NegY;

    private bool IsRoom(int room) => room >= 0 && room < Rooms.Count;

    // ---- Spatial lookup -------------------------------------------------------------------

    /// <summary>The index of the room whose footprint (walls included) holds the GridMap cell on X and Z, or -1. The layer is ignored.</summary>
    public int RoomAtCell(Vector3I cell) => CoreGraph.RoomAtCell(cell);

    /// <summary>The corridors whose route holds the GridMap cell on X and Z, in index order; more than one where corridors meet.</summary>
    public int[] CorridorsAtCell(Vector3I cell) => CoreGraph.CorridorsAtCell(cell);

    /// <summary>The room under a global position, such as a character's, through the GridMap's transform and cell size; or -1.</summary>
    public int RoomAtPosition(GridMap map, Vector3 globalPosition) =>
        RoomAtCell(map.LocalToMap(ToMap(map).AffineInverse() * globalPosition));

    /// <summary>The global position of a room's centre, at the height of its bottom layer's cell centres, for spawning. Zero for an unknown room.</summary>
    public Vector3 RoomCentre(GridMap map, int room)
    {
        if (!IsRoom(room))
            return Vector3.Zero;
        var r = Rooms[room];
        var first = map.MapToLocal(r.Position);
        var last = map.MapToLocal(r.Position + new Vector3I(r.Size.X - 1, 0, r.Size.Z - 1));
        return ToMap(map) * new Vector3((first.X + last.X) / 2, first.Y, (first.Z + last.Z) / 2);
    }

    /// <summary>The map's global transform, or its local one when it is not in a scene tree.</summary>
    private static Transform3D ToMap(GridMap map) => map.IsInsideTree() ? map.GlobalTransform : map.Transform;

    // ---- Neighbours -----------------------------------------------------------------------

    /// <summary>The rooms one corridor away, in corridor order, each once.</summary>
    public int[] Neighbours(int room) => CoreGraph.Neighbours(room);

    /// <summary>The lowest-index corridor joining two rooms, or -1.</summary>
    public int CorridorBetween(int a, int b) => CoreGraph.CorridorBetween(a, b);

    // ---- Distances and paths ----------------------------------------------------------------

    /// <summary>Per room, the fewest corridors from <paramref name="room"/>; -1 when unreachable. Empty for an unknown room.</summary>
    public int[] HopDistances(int room) => CoreGraph.HopDistances(room);

    /// <summary>Per room, the shortest total corridor length (in cells) from <paramref name="room"/>; -1 when unreachable. Walking through a room costs nothing.</summary>
    public int[] Distances(int room) => CoreGraph.Distances(room);

    /// <summary>The rooms of the shortest way by corridor length, both ends included; ties go to fewer corridors. Empty when unreachable.</summary>
    public int[] ShortestPath(int from, int to) => CoreGraph.ShortestPath(from, to);

    /// <summary>The corridors of <see cref="ShortestPath"/>, in walking order.</summary>
    public int[] ShortestPathCorridors(int from, int to) => CoreGraph.ShortestPathCorridors(from, to);

    /// <summary>The room farthest from <paramref name="room"/> by corridor length, or -1.</summary>
    public int FarthestRoom(int room) => CoreGraph.FarthestRoom(room);

    /// <summary>The two rooms farthest apart by corridor length, as (lower index, higher index); (-1, -1) with fewer than 2 rooms. A start and an end.</summary>
    public Vector2I Diameter()
    {
        var (a, b) = CoreGraph.Diameter();
        return new Vector2I(a, b);
    }
}
