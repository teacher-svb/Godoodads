using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Demo;

/// <summary>
/// The M0 prototype of godoodungeon (card dgn-m0-prototype): a hand-built layout of three rooms,
/// filled with the existing WFC solver only. Each room is one solve with "outside" boundary sockets
/// and its doors placed; then one corridor solve over the whole footprint joins the doors with
/// guided paths. Room cells are not in the corridor tile set, so that solve excludes them.
/// Run it headless:
/// godot --headless --path . res://demo/godoodungeon/prototype/build_prototype.tscn
/// </summary>
public partial class BuildDungeonPrototype : Node
{
    private const string Directory = "res://demo/godoodungeon/prototype";
    private const string LibraryPath = Directory + "/prototype_library.tres";
    private const string RoomTileSetPath = Directory + "/prototype_room_tileset.tres";
    private const string CorridorTileSetPath = Directory + "/prototype_corridor_tileset.tres";
    private const string EdgeSocket = "outside";
    // One-sided: tiles face out with "outside+", the room bounds answer with "outside-", so two
    // outward faces can never meet inside a room.
    private const string EdgeOut = EdgeSocket + "+";
    private const string EdgeBoundary = EdgeSocket + "-";
    private const string PathSocket = "path";

    private static readonly Vector3I Footprint = new(24, 1, 24);

    private static readonly Color FloorColour = new(0.55f, 0.52f, 0.48f);
    private static readonly Color WallColour = new(0.78f, 0.74f, 0.66f);
    private static readonly Color DoorColour = new(0.50f, 0.34f, 0.20f);
    private static readonly Color CrateColour = new(0.62f, 0.45f, 0.25f);
    private static readonly Color CorridorFloor = new(0.40f, 0.38f, 0.36f);
    private static readonly Color CorridorWall = new(0.60f, 0.57f, 0.52f);

    private sealed record Module(string Name, char Symbol, string[] Sockets, TileRotation Rotation, Action<Block> Build, float Weight = 1);

    // Socket order: +X, -X, +Y, -Y, +Z, -Z. Rooms are one layer, so vertical faces are always "v".
    // Room tiles face out of the room with "outside+", and every room's side faces have the boundary
    // socket "outside-", so the edge cells must be walls, corners or doors. Walls sit inside the
    // cell, on its outer edge. The door has weight 0: it is only used where it is placed.
    private static readonly Module[] RoomModules =
    {
        new("Floor", '.', S("in", "in", "v", "v", "in", "in"), TileRotation.None, b => b.Floor(FloorColour), 12),
        new("Crate", 'c', S("in", "in", "v", "v", "in", "in"), TileRotation.None, BuildCrate, 1.5f),
        new("Wall", '#', S(EdgeOut, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, b => { b.Floor(FloorColour); b.WallX(WallColour); }),
        new("Corner", '+', S(EdgeOut, "edge", "v", "v", EdgeOut, "edge"), TileRotation.FourWay, b => { b.Floor(FloorColour); b.WallX(WallColour); b.WallZ(WallColour); }),
        new("Door", 'D', S(EdgeOut, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, BuildDoor, 0),
    };

    // Corridor tiles: "path" on the open sides, "x" everywhere else, so path faces can only meet
    // path faces. The empty tile fills the space between corridors. Corridor walls stand on the
    // cell's outer edges of the closed sides, so a corridor along a room doubles the room's wall.
    private static readonly Module[] CorridorModules =
    {
        new("Corridor End", 'e', S(PathSocket, "x", "v", "v", "x", "x"), TileRotation.FourWay, b => BuildCorridor(b, true, false, false, false)),
        new("Corridor Straight", '=', S(PathSocket, PathSocket, "v", "v", "x", "x"), TileRotation.FourWay, b => BuildCorridor(b, true, true, false, false)),
        new("Corridor Corner", 'L', S(PathSocket, "x", "v", "v", PathSocket, "x"), TileRotation.FourWay, b => BuildCorridor(b, true, false, true, false)),
        new("Corridor T", 'T', S(PathSocket, PathSocket, "v", "v", PathSocket, "x"), TileRotation.FourWay, b => BuildCorridor(b, true, true, true, false)),
        new("Corridor Cross", 'X', S(PathSocket, PathSocket, "v", "v", PathSocket, PathSocket), TileRotation.None, b => BuildCorridor(b, true, true, true, true)),
        new("Empty", ' ', S("x", "x", "v", "v", "x", "x"), TileRotation.None, null, 20),
    };

    /// <summary>A room: its bounds (walls included) and its doors, each with the direction it faces out.</summary>
    private sealed record Room(Vector3I Origin, Vector3I Size, (Vector3I Cell, int Out)[] Doors, long Seed);

    private static readonly Room[] Rooms =
    {
        new(new(1, 0, 1), new(6, 1, 5), new[] { (new Vector3I(6, 0, 3), Directions.PosX), (new Vector3I(3, 0, 5), Directions.PosZ) }, 1),
        new(new(14, 0, 2), new(7, 1, 6), new[] { (new Vector3I(14, 0, 4), Directions.NegX), (new Vector3I(17, 0, 7), Directions.PosZ) }, 2),
        new(new(5, 0, 14), new(8, 1, 7), new[] { (new Vector3I(8, 0, 14), Directions.NegZ), (new Vector3I(12, 0, 17), Directions.PosX) }, 3),
    };

    private const long CorridorSeed = 7;

    private static string[] S(params string[] sockets) => sockets;

    public override void _Ready()
    {
        DirAccess.MakeDirRecursiveAbsolute(Directory);
        var library = BuildLibrary(out var roomItems, out var corridorItems);
        library.TakeOverPath(LibraryPath);
        Check(ResourceSaver.Save(library, LibraryPath), LibraryPath);

        var roomSet = BuildTileSet(library, RoomModules, roomItems);
        roomSet.ExternalSockets.Add(EdgeBoundary);   // the room bounds supply it
        roomSet.TakeOverPath(RoomTileSetPath);
        Check(ResourceSaver.Save(roomSet, RoomTileSetPath), RoomTileSetPath);
        var corridorSet = BuildTileSet(library, CorridorModules, corridorItems);
        corridorSet.TakeOverPath(CorridorTileSetPath);
        Check(ResourceSaver.Save(corridorSet, CorridorTileSetPath), CorridorTileSetPath);

        var map = new GridMap { Name = "GridMap", MeshLibrary = library };
        var room = TileSetBinding.Create(roomSet, Directions.All3D);
        var corridor = TileSetBinding.Create(corridorSet, Directions.All3D);
        foreach (var error in room.Errors.Concat(corridor.Errors))
            GD.PushError($"Prototype: {error}");

        PlaceDoorsAndStubs(map, room, corridor, roomItems, corridorItems);
        SaveScene("prototype_layout", map, corridorSet);

        bool solved = true;
        for (int i = 0; i < Rooms.Length; i++)
            solved &= SolveRoom(map, room, Rooms[i], i);
        solved &= SolveCorridors(map, corridor, out var record);
        if (record != null)
            GridMapCells.WriteRecord(map, record.Encode());

        GD.Print(Ascii(map, room, corridor));
        if (solved)
            GD.Print(Reachability(map, room, corridor));
        SaveScene("prototype_dungeon", map, corridorSet);
        map.Free();
        GD.Print(solved ? "Dungeon prototype: built." : "Dungeon prototype: built, but a solve failed (see above).");
        GetTree().Quit();
    }

    /// <summary>The doors, turned to face out, and a straight corridor piece outside each one.</summary>
    private static void PlaceDoorsAndStubs(GridMap map, TileSetBinding room, TileSetBinding corridor, int[] roomItems, int[] corridorItems)
    {
        int door = Array.FindIndex(RoomModules, m => m.Name == "Door");
        int straight = Array.FindIndex(CorridorModules, m => m.Name == "Corridor Straight");
        foreach (var r in Rooms)
            foreach (var (cell, outward) in r.Doors)
            {
                int doorTurns = TurnsWhere(room, door, v => v.Sockets[outward].Name == EdgeSocket && v.Sockets[Directions.Opposite(outward)].Name == "in");
                map.SetCellItem(cell, roomItems[door], GridMapOrientation.FromQuarterTurns(doorTurns));
                int stubTurns = TurnsWhere(corridor, straight, v => v.Sockets[Directions.Opposite(outward)].Name == PathSocket);
                map.SetCellItem(cell + Offset(outward), corridorItems[straight], GridMapOrientation.FromQuarterTurns(stubTurns));
            }
    }

    private static int TurnsWhere(TileSetBinding binding, int tile, Func<TileVariant, bool> condition)
    {
        for (int turns = 0; turns < 4; turns++)
        {
            int variant = binding.Compiled.FindVariant(tile, turns);
            if (variant >= 0 && condition(binding.Compiled.Variants[variant]))
                return turns;
        }
        throw new InvalidOperationException($"No rotation of tile {tile} fits.");
    }

    private static bool SolveRoom(GridMap map, TileSetBinding binding, Room room, int index)
    {
        var grid = GridTopology.Create3D(room.Size);
        var cells = GridMapCells.ReadFixed(map, room.Origin, grid, binding);
        var errors = new List<string>();
        var boundary = new WfcSettings { BoundaryPosX = EdgeBoundary, BoundaryNegX = EdgeBoundary, BoundaryPosZ = EdgeBoundary, BoundaryNegZ = EdgeBoundary }
            .ParseBoundary(errors);
        var job = new SolveJob(grid, binding.Compiled, cells.Initial, new SolveOptions { Seed = room.Seed, Boundary = boundary });
        return Run(job, map, room.Origin, grid, binding, cells, $"Room {index + 1}", out _);
    }

    private static bool SolveCorridors(GridMap map, TileSetBinding binding, out GeneratedRecord record)
    {
        var grid = GridTopology.Create3D(Footprint);
        var cells = GridMapCells.ReadFixed(map, Vector3I.Zero, grid, binding);
        GD.Print($"Corridors: {cells.Kept} placed corridor cells, {cells.Ignored} room cells excluded.");
        var job = new SolveJob(grid, binding.Compiled, cells.Initial, new SolveOptions { Seed = CorridorSeed }, paths: new PathRequest(PathSocket, 0.3));
        bool ok = Run(job, map, Vector3I.Zero, grid, binding, cells, "Corridors", out var result);
        record = ok ? GridMapCells.RecordAfter(null, Vector3I.Zero, grid, binding, cells.Initial, result.Cells) : null;
        if (job.Plan is { } plan)
            GD.Print($"Corridors: {plan.RouteCount} routes over {plan.RouteCellCount} cells, {job.RouteAttempts} route attempt(s).");
        return ok;
    }

    private static bool Run(SolveJob job, GridMap map, Vector3I origin, GridTopology grid, TileSetBinding binding, FixedCells cells, string what, out SolveResult result)
    {
        job.Start();
        job.Wait(-1);
        result = job.Result;
        if (job.Error != null)
            GD.PushError($"{what}: {job.Error}");
        if (job.Plan is { IsValid: false } plan)
            foreach (var error in plan.Errors)
                GD.PushError($"{what}: {error.Message}");
        if (result?.Status != SolveStatus.Solved)
        {
            GD.PushError($"{what}: {result?.Status.ToString() ?? "no result"} {result?.Failure?.ToString() ?? ""}");
            return false;
        }
        GridMapCells.Apply(map, GridMapCells.Diff(map, origin, grid, binding, cells.Initial, result.Cells).Do);
        GD.Print($"{what}: solved {grid.CellCount} cells ({result.Backtracks} backtracks, {result.Restarts} restarts).");
        return true;
    }

    /// <summary>A top-down map, +X to the right and +Z down.</summary>
    private static string Ascii(GridMap map, TileSetBinding room, TileSetBinding corridor)
    {
        var text = new StringBuilder("\n");
        for (int z = 0; z < Footprint.Z; z++)
        {
            for (int x = 0; x < Footprint.X; x++)
            {
                int item = map.GetCellItem(new Vector3I(x, 0, z));
                var module = ModuleOf(item, room, corridor);
                text.Append(module?.Symbol ?? (item == GridMap.InvalidCellItem ? ' ' : '?'));
            }
            text.Append('\n');
        }
        return text.ToString();
    }

    /// <summary>Walks from the first door through room interiors and corridors and counts the rooms reached.</summary>
    private static string Reachability(GridMap map, TileSetBinding room, TileSetBinding corridor)
    {
        string SocketAt(Vector3I cell, int direction, out string tileName)
        {
            int item = map.GetCellItem(cell);
            tileName = null;
            foreach (var binding in new[] { room, corridor })
            {
                int tile = binding.TileForItem(item);
                if (tile < 0)
                    continue;
                int variant = binding.Compiled.FindVariant(tile, GridMapOrientation.ToQuarterTurns(map.GetCellItemOrientation(cell)));
                tileName = binding.TileNames[tile];
                return binding.Compiled.Variants[variant].Sockets[direction].Name;
            }
            return null;
        }

        bool Passable(Vector3I from, int direction)
        {
            var to = from + Offset(direction);
            string a = SocketAt(from, direction, out string fromTile), b = SocketAt(to, Directions.Opposite(direction), out string toTile);
            if (a == null || b == null)
                return false;
            if ((a == "in" || a == "edge" || a == PathSocket) && a == b)
                return true;
            // A door opens onto the corridor piece outside it.
            return (fromTile == "Door" && a == EdgeSocket && b == PathSocket) || (toTile == "Door" && b == EdgeSocket && a == PathSocket);
        }

        int RoomOf(Vector3I cell) => Array.FindIndex(Rooms, r =>
            cell.X >= r.Origin.X && cell.X < r.Origin.X + r.Size.X && cell.Z >= r.Origin.Z && cell.Z < r.Origin.Z + r.Size.Z);

        var start = Rooms[0].Doors[0].Cell;
        var seen = new HashSet<Vector3I> { start };
        var queue = new Queue<Vector3I>(seen);
        var horizontal = new[] { Directions.PosX, Directions.NegX, Directions.PosZ, Directions.NegZ };
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (int direction in horizontal)
            {
                var next = cell + Offset(direction);
                if (!seen.Contains(next) && Passable(cell, direction))
                {
                    seen.Add(next);
                    queue.Enqueue(next);
                }
            }
        }
        var reached = seen.Select(RoomOf).Where(r => r >= 0).Distinct().Count();
        int doors = Rooms.Sum(r => r.Doors.Length);
        int doorsReached = Rooms.SelectMany(r => r.Doors).Count(d => seen.Contains(d.Cell));
        return $"Reachability: {reached} of {Rooms.Length} rooms and {doorsReached} of {doors} doors reached from room 1.";
    }

    private static Module ModuleOf(int item, TileSetBinding room, TileSetBinding corridor)
    {
        int tile = room.TileForItem(item);
        if (tile >= 0)
            return RoomModules[tile];
        tile = corridor.TileForItem(item);
        return tile >= 0 ? CorridorModules[tile] : null;
    }

    private static Vector3I Offset(int direction) => direction switch
    {
        Directions.PosX => new(1, 0, 0),
        Directions.NegX => new(-1, 0, 0),
        Directions.PosY => new(0, 1, 0),
        Directions.NegY => new(0, -1, 0),
        Directions.PosZ => new(0, 0, 1),
        _ => new(0, 0, -1),
    };

    private static MeshLibrary BuildLibrary(out int[] roomItems, out int[] corridorItems)
    {
        var library = new MeshLibrary();
        int next = 0;
        int[] Add(Module[] modules) => modules.Select(module =>
        {
            if (module.Build == null)
                return WfcTile.EmptyItem;
            var block = new Block();
            module.Build(block);
            library.CreateItem(next);
            library.SetItemName(next, module.Name);
            library.SetItemMesh(next, block.Commit());
            return next++;
        }).ToArray();
        roomItems = Add(RoomModules);
        corridorItems = Add(CorridorModules);
        return library;
    }

    private static WfcTileSet BuildTileSet(MeshLibrary library, Module[] modules, int[] items)
    {
        var tileSet = new WfcTileSet { Library = library };
        for (int i = 0; i < modules.Length; i++)
        {
            var s = modules[i].Sockets;
            tileSet.Tiles.Add(new WfcTile
            {
                ResourceName = modules[i].Name, Item = items[i], Rotation = modules[i].Rotation, Weight = modules[i].Weight,
                SocketPosX = s[Directions.PosX], SocketNegX = s[Directions.NegX],
                SocketPosY = s[Directions.PosY], SocketNegY = s[Directions.NegY],
                SocketPosZ = s[Directions.PosZ], SocketNegZ = s[Directions.NegZ],
            });
        }
        return tileSet;
    }

    private static void SaveScene(string name, GridMap map, WfcTileSet corridorSet)
    {
        var root = new Node3D { Name = name.ToPascalCase() };
        var copy = (GridMap)map.Duplicate();
        root.AddChild(copy);
        copy.Owner = root;
        // Opening the WFC dialog on this GridMap regenerates the corridors: the record holds only them.
        copy.SetMeta(WfcSettings.MetaKey, new WfcSettings
        {
            TileSet = corridorSet, Seed = CorridorSeed, BoundsSize = Footprint, PathSocket = PathSocket,
        });

        var sun = new DirectionalLight3D { Name = "Sun", ShadowEnabled = true, RotationDegrees = new Vector3(-60, -30, 0) };
        root.AddChild(sun);
        sun.Owner = root;
        var environment = new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
            },
        };
        root.AddChild(environment);
        environment.Owner = root;

        var packed = new PackedScene();
        Check(packed.Pack(root), name);
        Check(ResourceSaver.Save(packed, $"{Directory}/{name}.tscn"), name);
        root.Free();
    }

    private static void Check(Error error, string what)
    {
        if (error != Error.Ok)
            GD.PushError($"Dungeon prototype: could not build {what}: {error}.");
    }

    private static void BuildCrate(Block b)
    {
        b.Floor(FloorColour);
        b.Box(new(-0.45f, -0.8f, -0.45f), new(0.45f, 0.1f, 0.45f), CrateColour);
    }

    /// <summary>A wall on the +X edge with an opening and a lintel.</summary>
    private static void BuildDoor(Block b)
    {
        b.Floor(FloorColour);
        b.Box(new(0.6f, -0.8f, -1), new(1, 1, -0.5f), WallColour);
        b.Box(new(0.6f, -0.8f, 0.5f), new(1, 1, 1), WallColour);
        b.Box(new(0.6f, 0.5f, -0.5f), new(1, 1, 0.5f), DoorColour);
    }

    /// <summary>A corridor floor with walls on the closed sides (+X, -X, +Z, -Z open or not).</summary>
    private static void BuildCorridor(Block b, bool px, bool nx, bool pz, bool nz)
    {
        b.Box(new(-1, -1, -1), new(1, -0.8f, 1), CorridorFloor);
        if (!px)
            b.Box(new(0.6f, -0.8f, -1), new(1, 1, 1), CorridorWall);
        if (!nx)
            b.Box(new(-1, -0.8f, -1), new(-0.6f, 1, 1), CorridorWall);
        if (!pz)
            b.Box(new(-1, -0.8f, 0.6f), new(1, 1, 1), CorridorWall);
        if (!nz)
            b.Box(new(-1, -0.8f, -1), new(1, 1, -0.6f), CorridorWall);
    }

    /// <summary>Builds one mesh from coloured boxes.</summary>
    private sealed class Block
    {
        private readonly SurfaceTool _tool = new();

        public Block() => _tool.Begin(Mesh.PrimitiveType.Triangles);

        public ArrayMesh Commit()
        {
            var mesh = _tool.Commit();
            mesh.SurfaceSetMaterial(0, new StandardMaterial3D { VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled });
            return mesh;
        }

        public void Floor(Color colour) => Box(new(-1, -1, -1), new(1, -0.8f, 1), colour);
        public void WallX(Color colour) => Box(new(0.6f, -0.8f, -1), new(1, 1, 1), colour);
        public void WallZ(Color colour) => Box(new(-1, -0.8f, 0.6f), new(1, 1, 1), colour);

        public void Box(Vector3 min, Vector3 max, Color colour)
        {
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
                corners[i] = new Vector3((i & 1) != 0 ? max.X : min.X, (i & 2) != 0 ? max.Y : min.Y, (i & 4) != 0 ? max.Z : min.Z);
            Face(corners, 1, 3, 7, 5, Vector3.Right, colour);
            Face(corners, 0, 4, 6, 2, Vector3.Left, colour);
            Face(corners, 2, 6, 7, 3, Vector3.Up, colour);
            Face(corners, 0, 1, 5, 4, Vector3.Down, colour);
            Face(corners, 4, 5, 7, 6, Vector3.Back, colour);
            Face(corners, 0, 2, 3, 1, Vector3.Forward, colour);
        }

        private void Face(Vector3[] c, int a, int b, int d, int e, Vector3 normal, Color colour)
        {
            foreach (int i in new[] { a, b, d, a, d, e })
            {
                _tool.SetColor(colour);
                _tool.SetNormal(normal);
                _tool.AddVertex(c[i]);
            }
        }
    }
}
