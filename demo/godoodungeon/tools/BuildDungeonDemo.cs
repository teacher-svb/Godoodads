using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Adapters;

namespace Godoodads.Dungeon.Demo;

/// <summary>
/// Builds the godoodungeon demo (brief godoodungeon §7, M4): a MeshLibrary of simple dungeon pieces,
/// the room and corridor tile sets, and their marker scenes. Walls stand inside the cell, on its outer
/// edge (§6.1). Run it headless:
/// godot --headless --path . res://demo/godoodungeon/tools/build_demo.tscn
/// </summary>
public partial class BuildDungeonDemo : Node
{
    public const string AssetDirectory = "res://demo/godoodungeon/assets";
    public const string LibraryPath = AssetDirectory + "/dungeon_library.tres";
    public const string RoomTileSetPath = AssetDirectory + "/dungeon_room_tileset.tres";
    public const string CorridorTileSetPath = AssetDirectory + "/dungeon_corridor_tileset.tres";
    public const string TorchPath = AssetDirectory + "/torch.tscn";
    public const string CorridorTorchPath = AssetDirectory + "/corridor_torch.tscn";
    public const string SpawnPath = AssetDirectory + "/spawn_point.tscn";

    public const string SceneDirectory = "res://demo/godoodungeon";

    /// <summary>The brief's target size (§3): 64 × 64 cells and 20 rooms.</summary>
    public static readonly Vector3I DemoBounds = new(64, 1, 64);

    /// <summary>Hand-placed obstacles of dungeon_obstacles.tscn: statues, and a pool of 6 × 4 cells.</summary>
    public static readonly Vector3I[] Statues = { new(12, 0, 12), new(50, 0, 14), new(31, 0, 31), new(14, 0, 50), new(49, 0, 49) };
    public static readonly Rect2I Pool = new(28, 44, 6, 4);

    public const string EdgeSocket = "outside";
    public const string PathSocket = "path";

    private static readonly Color Flagstone = new(0.46f, 0.44f, 0.41f);
    private static readonly Color FlagstoneDark = new(0.36f, 0.34f, 0.32f);
    private static readonly Color Crack = new(0.22f, 0.21f, 0.2f);
    private static readonly Color Stone = new(0.62f, 0.59f, 0.54f);
    private static readonly Color StoneDark = new(0.48f, 0.45f, 0.41f);
    private static readonly Color Wood = new(0.42f, 0.27f, 0.15f);
    private static readonly Color WoodLight = new(0.58f, 0.40f, 0.22f);
    private static readonly Color Iron = new(0.25f, 0.25f, 0.27f);
    private static readonly Color Banner = new(0.55f, 0.1f, 0.12f);
    private static readonly Color Gold = new(0.85f, 0.65f, 0.2f);
    private static readonly Color CorridorFloor = new(0.33f, 0.31f, 0.29f);
    private static readonly Color Water = new(0.15f, 0.35f, 0.55f);
    private static readonly Color Marble = new(0.82f, 0.8f, 0.76f);

    /// <summary>A module: MeshLibrary item name, sockets (+X, -X, +Y, -Y, +Z, -Z), rotation, mesh, weight and marker.</summary>
    private sealed record Module(string Name, string[] Sockets, TileRotation Rotation, Action<Block> Build, float Weight = 1, string Marker = null);

    private static string[] S(params string[] sockets) => sockets;

    private const string Out = EdgeSocket + "+";

    // Rooms: "in" between interior cells, "edge" along a wall, "outside+" out of the room (brief §4.2).
    // Rooms are one layer tall, so the vertical faces are "v". The door has weight 0: only the layout places doors.
    private static readonly Module[] RoomModules =
    {
        new("Floor", S("in", "in", "v", "v", "in", "in"), TileRotation.None, b => b.Floor(Flagstone), 12),
        new("Floor Cracked", S("in", "in", "v", "v", "in", "in"), TileRotation.FourWay, BuildCrackedFloor, 3),
        new("Floor Spawn", S("in", "in", "v", "v", "in", "in"), TileRotation.None, b => b.Floor(Flagstone), 0.25f, SpawnPath),
        new("Crate", S("in", "in", "v", "v", "in", "in"), TileRotation.FourWay, BuildCrate, 0.8f),
        new("Barrel", S("in", "in", "v", "v", "in", "in"), TileRotation.None, BuildBarrel, 0.8f),
        new("Pillar", S("in", "in", "v", "v", "in", "in"), TileRotation.None, BuildPillar, 0.3f),
        new("Wall", S(Out, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, b => { b.Floor(Flagstone); b.WallX(Stone); }, 4),
        new("Wall Torch", S(Out, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, BuildWallSconce, 0.6f, TorchPath),
        new("Wall Banner", S(Out, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, BuildWallBanner, 0.6f),
        new("Corner", S(Out, "edge", "v", "v", Out, "edge"), TileRotation.FourWay, BuildCorner),
        new("Door", S(Out, "in", "v", "v", "edge", "edge"), TileRotation.FourWay, BuildDoor, 0),
    };

    // Corridors: "path" on the open sides, "x" on the closed ones and on the empty tile. The router
    // only makes straights, bends, T pieces and crosses, so there is no dead end (card dgn-m4-demo-meshlibrary).
    private static readonly Module[] CorridorModules =
    {
        new("Corridor", S(PathSocket, PathSocket, "v", "v", "x", "x"), TileRotation.FourWay, b => BuildCorridor(b, true, true, false, false), 3),
        new("Corridor Torch", S(PathSocket, PathSocket, "v", "v", "x", "x"), TileRotation.FourWay, BuildCorridorSconce, 0.4f, CorridorTorchPath),
        new("Corridor Bend", S(PathSocket, "x", "v", "v", PathSocket, "x"), TileRotation.FourWay, b => BuildCorridor(b, true, false, true, false)),
        new("Corridor T", S(PathSocket, PathSocket, "v", "v", PathSocket, "x"), TileRotation.FourWay, b => BuildCorridor(b, true, true, true, false)),
        new("Corridor Cross", S(PathSocket, PathSocket, "v", "v", PathSocket, PathSocket), TileRotation.None, b => BuildCorridor(b, true, true, true, true)),
        new("Empty", S("x", "x", "v", "v", "x", "x"), TileRotation.None, null, 20),
    };

    /// <summary>Items in the library but in no tile set: hand-placed obstacles for the obstacles scene.</summary>
    private static readonly Module[] ObstacleModules =
    {
        new("Statue", null, TileRotation.None, BuildStatue),
        new("Pool", null, TileRotation.None, BuildPool),
    };

    public override void _Ready()
    {
        DirAccess.MakeDirRecursiveAbsolute(AssetDirectory);
        var markers = new Dictionary<string, PackedScene>
        {
            [TorchPath] = SaveScene(BuildTorch(new Vector3(0.42f, 0.45f, 0), "Torch"), TorchPath),
            [CorridorTorchPath] = SaveScene(BuildTorch(new Vector3(0, 0.45f, -0.45f), "CorridorTorch"), CorridorTorchPath),
            [SpawnPath] = SaveScene(BuildSpawn(), SpawnPath),
        };

        var library = new MeshLibrary();
        var items = new Dictionary<string, int>();
        foreach (var module in RoomModules.Concat(CorridorModules).Concat(ObstacleModules))
        {
            if (module.Build == null)
                continue;
            var block = new Block();
            module.Build(block);
            int id = items.Count;
            library.CreateItem(id);
            library.SetItemName(id, module.Name);
            library.SetItemMesh(id, block.Commit());
            items[module.Name] = id;
        }
        library.TakeOverPath(LibraryPath);
        Check(ResourceSaver.Save(library, LibraryPath), LibraryPath);

        var room = TileSet(library, RoomModules, items, markers);
        room.ExternalSockets.Add(EdgeSocket + "-");   // the room bounds supply it (brief §4.2)
        room.TakeOverPath(RoomTileSetPath);
        Check(ResourceSaver.Save(room, RoomTileSetPath), RoomTileSetPath);
        var corridor = TileSet(library, CorridorModules, items, markers);
        corridor.TakeOverPath(CorridorTileSetPath);
        Check(ResourceSaver.Save(corridor, CorridorTileSetPath), CorridorTileSetPath);

        // The tile sets must pass godoodungeon's check and the WFC tile set check without warnings.
        var settings = new DungeonSettings
        {
            RoomTileSet = room, CorridorTileSet = corridor, Door = room.Tiles.Single(t => t.ResourceName == "Door").Item,
            RoomEdgeSocket = EdgeSocket, PathSocket = PathSocket,
        };
        var check = DungeonTileSets.Check(settings);
        foreach (string error in check.Errors)
            GD.PushError($"Dungeon demo: {error}");
        foreach (string warning in check.Warnings.Concat(check.Room?.Warnings() ?? new()).Concat(check.Corridor?.Warnings() ?? new()))
            GD.PushWarning($"Dungeon demo: {warning}");
        SaveDemoScene("dungeon_free", library, settings, 1, null);
        SaveDemoScene("dungeon_obstacles", library, settings, 2, map =>
        {
            foreach (var cell in Statues)
                map.SetCellItem(cell, items["Statue"]);
            for (int x = Pool.Position.X; x < Pool.End.X; x++)
                for (int z = Pool.Position.Y; z < Pool.End.Y; z++)
                    map.SetCellItem(new Vector3I(x, 0, z), items["Pool"]);
        });
        GD.Print($"Dungeon demo: built {library.GetItemList().Length} items, {room.Tiles.Count} room tiles, {corridor.Tiles.Count} corridor tiles "
            + $"and 2 scenes; {check.Errors.Count} errors.");
        GetTree().Quit();
    }

    /// <summary>A demo scene: an empty GridMap with the dungeon settings stored on it, a sun and a sky. Press Generate in the editor.</summary>
    private static void SaveDemoScene(string name, MeshLibrary library, DungeonSettings template, long seed, Action<GridMap> place)
    {
        var root = new Node3D { Name = name.ToPascalCase() };
        var map = new GridMap { Name = "GridMap", MeshLibrary = library };
        Own(root, map);
        var settings = (DungeonSettings)template.Duplicate();
        settings.BoundsSize = DemoBounds;
        settings.RoomCount = 20;
        settings.Seed = seed;
        DungeonSettings.StoreOn(map, settings);
        place?.Invoke(map);

        Own(root, new DirectionalLight3D { Name = "Sun", ShadowEnabled = true, RotationDegrees = new Vector3(-60, -30, 0), LightEnergy = 0.6f });
        Own(root, new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightEnergy = 0.5f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
            },
        });
        var packed = new PackedScene();
        Check(packed.Pack(root), name);
        Check(ResourceSaver.Save(packed, $"{SceneDirectory}/{name}.tscn"), name);
        root.Free();
    }

    private static WfcTileSet TileSet(MeshLibrary library, Module[] modules, Dictionary<string, int> items, Dictionary<string, PackedScene> markers)
    {
        var tileSet = new WfcTileSet { Library = library };
        foreach (var module in modules)
        {
            var s = module.Sockets;
            tileSet.Tiles.Add(new WfcTile
            {
                ResourceName = module.Name,
                Item = module.Build == null ? WfcTile.EmptyItem : items[module.Name],
                Rotation = module.Rotation,
                Weight = module.Weight,
                Marker = module.Marker == null ? null : markers[module.Marker],
                SocketPosX = s[Directions.PosX], SocketNegX = s[Directions.NegX],
                SocketPosY = s[Directions.PosY], SocketNegY = s[Directions.NegY],
                SocketPosZ = s[Directions.PosZ], SocketNegZ = s[Directions.NegZ],
            });
        }
        return tileSet;
    }

    private static PackedScene SaveScene(Node3D root, string path)
    {
        var packed = new PackedScene();
        Check(packed.Pack(root), path);
        packed.TakeOverPath(path);
        Check(ResourceSaver.Save(packed, path), path);
        root.Free();
        return packed;
    }

    private static void Check(Error error, string what)
    {
        if (error != Error.Ok)
            GD.PushError($"Dungeon demo: could not build {what}: {error}.");
    }

    private static void Own(Node root, Node child)
    {
        root.AddChild(child);
        child.Owner = root;
    }

    /// <summary>A warm light with a small flame, at <paramref name="at"/> in cell space (the cell is 2 units wide).</summary>
    private static Node3D BuildTorch(Vector3 at, string name)
    {
        var root = new Node3D { Name = name };
        Own(root, new OmniLight3D { Name = "Light", Position = at, LightColor = new Color(1f, 0.7f, 0.4f), LightEnergy = 1.6f, OmniRange = 5f });
        Own(root, new MeshInstance3D
        {
            Name = "Flame", Position = at,
            Mesh = new SphereMesh { Radius = 0.09f, Height = 0.18f, RadialSegments = 8, Rings = 4 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.6f, 0.25f), EmissionEnabled = true,
                Emission = new Color(1f, 0.55f, 0.2f), EmissionEnergyMultiplier = 3f,
            },
        });
        return root;
    }

    /// <summary>A spawn point: a Marker3D a little above the floor with a glowing disc, so it shows in the editor.</summary>
    private static Node3D BuildSpawn()
    {
        var root = new Node3D { Name = "SpawnPoint" };
        Own(root, new Marker3D { Name = "Spawn", Position = new Vector3(0, -0.6f, 0) });
        Own(root, new MeshInstance3D
        {
            Name = "Disc", Position = new Vector3(0, -0.78f, 0),
            Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.02f, RadialSegments = 16 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.3f, 0.9f, 0.5f), EmissionEnabled = true,
                Emission = new Color(0.2f, 0.8f, 0.4f), EmissionEnergyMultiplier = 1.5f,
            },
        });
        return root;
    }

    private static void BuildCrackedFloor(Block b)
    {
        b.Floor(FlagstoneDark);
        b.Box(new(-0.7f, -0.8f, -0.05f), new(0.2f, -0.79f, 0.02f), Crack);
        b.Box(new(0.1f, -0.8f, -0.4f), new(0.17f, -0.79f, 0.02f), Crack);
    }

    private static void BuildCrate(Block b)
    {
        b.Floor(Flagstone);
        b.Box(new(-0.55f, -0.8f, -0.55f), new(0.25f, 0f, 0.25f), WoodLight);
        b.Box(new(-0.57f, -0.42f, -0.57f), new(0.27f, -0.38f, 0.27f), Wood);
    }

    private static void BuildBarrel(Block b)
    {
        b.Floor(Flagstone);
        b.Box(new(-0.35f, -0.8f, -0.35f), new(0.35f, 0.1f, 0.35f), Wood);
        b.Box(new(-0.37f, -0.55f, -0.37f), new(0.37f, -0.5f, 0.37f), Iron);
        b.Box(new(-0.37f, -0.15f, -0.37f), new(0.37f, -0.1f, 0.37f), Iron);
    }

    private static void BuildPillar(Block b)
    {
        b.Floor(Flagstone);
        b.Box(new(-0.4f, -0.8f, -0.4f), new(0.4f, -0.65f, 0.4f), StoneDark);
        b.Box(new(-0.28f, -0.65f, -0.28f), new(0.28f, 0.85f, 0.28f), Stone);
        b.Box(new(-0.4f, 0.85f, -0.4f), new(0.4f, 1f, 0.4f), StoneDark);
    }

    private static void BuildWallSconce(Block b)
    {
        b.Floor(Flagstone);
        b.WallX(Stone);
        b.Box(new(0.48f, 0.2f, -0.06f), new(0.6f, 0.38f, 0.06f), Iron);
    }

    private static void BuildWallBanner(Block b)
    {
        b.Floor(Flagstone);
        b.WallX(Stone);
        b.Box(new(0.56f, -0.3f, -0.35f), new(0.6f, 0.8f, 0.35f), Banner);
        b.Box(new(0.55f, 0.1f, -0.1f), new(0.56f, 0.4f, 0.1f), Gold);
    }

    private static void BuildCorner(Block b)
    {
        b.Floor(Flagstone);
        b.WallX(Stone);
        b.WallZ(Stone);
        b.Box(new(0.55f, -0.8f, 0.55f), new(1f, 1.05f, 1f), StoneDark);
    }

    /// <summary>A wall on the +X edge with an archway and a wooden frame.</summary>
    private static void BuildDoor(Block b)
    {
        b.Floor(Flagstone);
        b.Box(new(0.6f, -0.8f, -1), new(1, 1, -0.55f), Stone);
        b.Box(new(0.6f, -0.8f, 0.55f), new(1, 1, 1), Stone);
        b.Box(new(0.6f, 0.45f, -0.55f), new(1, 1, 0.55f), StoneDark);
        b.Box(new(0.55f, -0.8f, -0.6f), new(1.02f, 0.5f, -0.5f), Wood);
        b.Box(new(0.55f, -0.8f, 0.5f), new(1.02f, 0.5f, 0.6f), Wood);
        b.Box(new(0.55f, 0.4f, -0.6f), new(1.02f, 0.5f, 0.6f), Wood);
    }

    /// <summary>A corridor floor with walls on the closed sides (+X, -X, +Z, -Z open or not).</summary>
    private static void BuildCorridor(Block b, bool px, bool nx, bool pz, bool nz)
    {
        b.Box(new(-1, -1, -1), new(1, -0.8f, 1), CorridorFloor);
        if (!px)
            b.Box(new(0.6f, -0.8f, -1), new(1, 1, 1), StoneDark);
        if (!nx)
            b.Box(new(-1, -0.8f, -1), new(-0.6f, 1, 1), StoneDark);
        if (!pz)
            b.Box(new(-1, -0.8f, 0.6f), new(1, 1, 1), StoneDark);
        if (!nz)
            b.Box(new(-1, -0.8f, -1), new(1, 1, -0.6f), StoneDark);
    }

    private static void BuildCorridorSconce(Block b)
    {
        BuildCorridor(b, true, true, false, false);
        b.Box(new(-0.06f, 0.2f, -0.6f), new(0.06f, 0.38f, -0.48f), Iron);
    }

    private static void BuildStatue(Block b)
    {
        b.Box(new(-0.7f, -1, -0.7f), new(0.7f, -0.5f, 0.7f), StoneDark);
        b.Box(new(-0.3f, -0.5f, -0.3f), new(0.3f, 0.6f, 0.3f), Marble);
        b.Box(new(-0.2f, 0.6f, -0.2f), new(0.2f, 1f, 0.2f), Marble);
    }

    private static void BuildPool(Block b)
    {
        b.Box(new(-1, -1, -1), new(1, -0.85f, 1), StoneDark);
        b.Box(new(-0.85f, -0.85f, -0.85f), new(0.85f, -0.82f, 0.85f), Water);
        b.Box(new(-1, -0.85f, -1), new(1, -0.6f, -0.85f), Stone);
        b.Box(new(-1, -0.85f, 0.85f), new(1, -0.6f, 1), Stone);
        b.Box(new(-1, -0.85f, -0.85f), new(-0.85f, -0.6f, 0.85f), Stone);
        b.Box(new(0.85f, -0.85f, -0.85f), new(1, -0.6f, 0.85f), Stone);
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
            var c = new Vector3[8];
            for (int i = 0; i < 8; i++)
                c[i] = new Vector3((i & 1) != 0 ? max.X : min.X, (i & 2) != 0 ? max.Y : min.Y, (i & 4) != 0 ? max.Z : min.Z);
            Face(c, 1, 3, 7, 5, Vector3.Right, colour);
            Face(c, 0, 4, 6, 2, Vector3.Left, colour);
            Face(c, 2, 6, 7, 3, Vector3.Up, colour);
            Face(c, 0, 1, 5, 4, Vector3.Down, colour);
            Face(c, 4, 5, 7, 6, Vector3.Back, colour);
            Face(c, 0, 2, 3, 1, Vector3.Forward, colour);
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
