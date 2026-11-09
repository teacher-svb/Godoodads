using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Demo;

/// <summary>
/// Builds the stacked hillside ruin demo: MeshLibrary, tile set and scenes. Run it headless:
/// godot --headless --path . res://demo/wfc_generator/tools/build_demo.tscn
/// </summary>
public partial class BuildWfcDemo : Node
{
    public const string AssetDirectory = "res://demo/wfc_generator/assets";
    public const string LibraryPath = AssetDirectory + "/ruin_library.tres";
    public const string TileSetPath = AssetDirectory + "/ruin_tileset.tres";
    private const string SceneDirectory = "res://demo/wfc_generator";

    private const int AboveGroundCopies = 40;
    private const int SkyCopies = 40;
    private const int GrassVariants = 16;

    private static readonly Vector3I DemoBounds = new(32, 12, 32);

    private static readonly Color RockColour = new(0.42f, 0.40f, 0.39f);
    private static readonly Color GrassColour = new(0.36f, 0.60f, 0.27f);
    private static readonly Color GrassDark = new(0.27f, 0.50f, 0.23f);
    private static readonly Color SlopeColour = new(0.47f, 0.58f, 0.30f);
    private static readonly Color PavingColour = new(0.68f, 0.64f, 0.56f);
    private static readonly Color StoneColour = new(0.78f, 0.74f, 0.66f);
    private static readonly Color StoneDark = new(0.62f, 0.58f, 0.52f);
    private static readonly Color WoodColour = new(0.50f, 0.34f, 0.20f);

    private sealed record Module(string Name, string[] Sockets, TileRotation Rotation, Action<BlockBuilder> Build, int Copies = 1);

    // Socket order: +X, -X, +Y, -Y, +Z, -Z.
    // "ground" is a surface you can build on: grass, paving, wall tops and bridge decks.
    // Air sits on "air" and rises forever. AboveGround air is the one layer that may rest on "ground".
    private static readonly Module[] BaseModules =
    {
        new("Rock", S("solid", "solid", "rock", "rock", "solid", "solid"), TileRotation.None, b => b.Slope(1, 1, 1, 1, RockColour)),
        new("Grass", S("solid", "solid", "ground", "rock", "solid", "solid"), TileRotation.None, b => b.Slope(1, 1, 1, 1, GrassColour)),
        new("Paving", S("solid", "solid", "ground", "rock", "solid", "solid"), TileRotation.None, b => b.Slope(1, 1, 1, 1, PavingColour)),
        new("Ramp", S("solid", "open", "air", "rock", "side+", "side-"), TileRotation.FourWay, b => b.Slope(1, 1, -1, -1, SlopeColour)),
        new("Outer Corner", S("side-", "open", "air", "rock", "side+", "open"), TileRotation.FourWay, b => b.Slope(1, -1, -1, -1, SlopeColour)),
        new("Inner Corner", S("solid", "side+", "air", "rock", "solid", "side-"), TileRotation.FourWay, b => b.Slope(1, 1, 1, -1, SlopeColour)),

        new("Wall", S("wall", "wall", "air", "ground", "open", "open"), TileRotation.FourWay, b => BuildWall(b, -1, 1)),
        new("Wall Arch", S("wall", "wall", "air", "ground", "open", "open"), TileRotation.FourWay, BuildArch),
        new("Broken Wall", S("wall", "wall", "air", "ground", "open", "open"), TileRotation.FourWay, BuildBrokenWall),
        new("Wall End", S("wall", "open", "air", "ground", "open", "open"), TileRotation.FourWay, b => BuildWall(b, -0.4f, 1)),
        new("Wall Corner", S("wall", "open", "air", "ground", "wall", "open"), TileRotation.FourWay, BuildCorner),
        new("Tower Base", S("open", "open", "tower", "ground", "open", "open"), TileRotation.None, BuildTowerBase),
        new("Tower Top", S("open", "open", "air", "tower", "open", "open"), TileRotation.None, BuildTowerTop),
        new("Pillar", S("open", "open", "pier", "ground", "open", "open"), TileRotation.None, b => b.Box(new(-0.35f, -1, -0.35f), new(0.35f, 1, 0.35f), StoneColour)),
        new("Stairs", S("landing", "open", "air", "ground", "open", "open"), TileRotation.FourWay, BuildStairs),
        new("Plinth", S("open", "landing", "air", "ground", "open", "open"), TileRotation.FourWay, b => b.Box(new(-1, -1, -1), new(1, 1, 1), StoneDark)),
        // A bridge is always head, deck, deck, head. A fixed length keeps both supports within local reach.
        new("Bridge Head", S("span+", "open", "air", "pier", "open", "open"), TileRotation.FourWay, BuildBridgeHead),
        new("Bridge Deck", S("mid", "span-", "air", "air", "open", "open"), TileRotation.FourWay, BuildDeck),

        // No weights in v1, so copies of the empty tiles are what keep most of the map open.
        new("Air", S("open", "open", "air", "air", "open", "open"), TileRotation.None, null, SkyCopies),
        new("Air Above Ground", S("open", "open", "air", "ground", "open", "open"), TileRotation.None, null, AboveGroundCopies),
    };

    // Variety that also makes flat ground as likely as the many rotations of the slopes.
    private static readonly Module[] Modules = BaseModules.Concat(Enumerable.Range(0, GrassVariants).Select(i =>
        new Module($"Grass {i + 1}", S("solid", "solid", "ground", "rock", "solid", "solid"), TileRotation.None, b => BuildGrass(b, i)))).ToArray();

    private static string[] S(params string[] sockets) => sockets;

    public override void _Ready()
    {
        DirAccess.MakeDirRecursiveAbsolute(AssetDirectory);
        var library = BuildLibrary(out var items);
        library.TakeOverPath(LibraryPath);
        Check(ResourceSaver.Save(library, LibraryPath), LibraryPath);

        var tileSet = BuildTileSet(library, items);
        tileSet.TakeOverPath(TileSetPath);
        Check(ResourceSaver.Save(tileSet, TileSetPath), TileSetPath);

        SaveScene("hillside_free", library, tileSet, 1, false);
        SaveScene("hillside_ground", library, tileSet, 2, true);
        GD.Print("WFC demo: built.");
        GetTree().Quit();
    }

    private static MeshLibrary BuildLibrary(out int[] items)
    {
        var library = new MeshLibrary();
        items = new int[Modules.Length];
        int next = 0;
        for (int i = 0; i < Modules.Length; i++)
        {
            if (Modules[i].Build == null)
            {
                items[i] = WfcTile.EmptyItem;
                continue;
            }
            var builder = new BlockBuilder();
            Modules[i].Build(builder);
            library.CreateItem(next);
            library.SetItemName(next, Modules[i].Name);
            library.SetItemMesh(next, builder.Commit());
            items[i] = next++;
        }
        return library;
    }

    private static WfcTileSet BuildTileSet(MeshLibrary library, int[] items)
    {
        var tileSet = new WfcTileSet { Library = library };
        for (int i = 0; i < Modules.Length; i++)
        {
            var s = Modules[i].Sockets;
            for (int copy = 0; copy < Modules[i].Copies; copy++)
                tileSet.Tiles.Add(new WfcTile
                {
                    ResourceName = Modules[i].Name,
                    Item = items[i],
                    Rotation = Modules[i].Rotation,
                    SocketPosX = s[Directions.PosX], SocketNegX = s[Directions.NegX],
                    SocketPosY = s[Directions.PosY], SocketNegY = s[Directions.NegY],
                    SocketPosZ = s[Directions.PosZ], SocketNegZ = s[Directions.NegZ],
                });
        }
        return tileSet;
    }

    private static void SaveScene(string name, MeshLibrary library, WfcTileSet tileSet, long seed, bool ground)
    {
        var root = new Node3D { Name = name.ToPascalCase() };
        var map = new GridMap { Name = "GridMap", MeshLibrary = library };
        root.AddChild(map);
        map.Owner = root;

        if (ground)
        {
            int rock = library.FindItemByName("Rock");
            for (int z = 0; z < DemoBounds.Z; z++)
                for (int x = 0; x < DemoBounds.X; x++)
                    map.SetCellItem(new Vector3I(x, 0, z), rock);
        }

        map.SetMeta(WfcSettings.MetaKey, new WfcSettings { TileSet = tileSet, Seed = seed, BoundsSize = DemoBounds });

        var sun = new DirectionalLight3D { Name = "Sun", ShadowEnabled = true, RotationDegrees = new Vector3(-55, -35, 0) };
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
        Check(ResourceSaver.Save(packed, $"{SceneDirectory}/{name}.tscn"), name);
        root.Free();
    }

    private static void Check(Error error, string what)
    {
        if (error != Error.Ok)
            GD.PushError($"WFC demo: could not build {what}: {error}.");
    }

    private static void BuildGrass(BlockBuilder b, int variant)
    {
        float tint = (variant % 4) * 0.03f;
        b.Slope(1, 1, 1, 1, GrassColour + new Color(tint, tint, 0, 0));
        var random = new Random(variant + 11);
        for (int i = 0; i < variant % 4; i++)
        {
            float x = (float)random.NextDouble() * 1.4f - 0.8f, z = (float)random.NextDouble() * 1.4f - 0.8f;
            b.Box(new(x, 1, z), new(x + 0.25f, 1.25f, z + 0.25f), GrassDark);
        }
    }

    private static void BuildWall(BlockBuilder b, float fromX, float toX) =>
        b.Box(new(fromX, -1, -0.4f), new(toX, 1, 0.4f), StoneColour);

    private static void BuildArch(BlockBuilder b)
    {
        b.Box(new(-1, -1, -0.4f), new(-0.55f, 1, 0.4f), StoneColour);
        b.Box(new(0.55f, -1, -0.4f), new(1, 1, 0.4f), StoneColour);
        b.Box(new(-0.55f, 0.4f, -0.4f), new(0.55f, 1, 0.4f), StoneDark);
    }

    private static void BuildBrokenWall(BlockBuilder b)
    {
        b.Box(new(-1, -1, -0.4f), new(1, -0.2f, 0.4f), StoneColour);
        b.Box(new(-1, -0.2f, -0.4f), new(0.1f, 0.3f, 0.4f), StoneColour);
        b.Box(new(-1, 0.3f, -0.4f), new(-0.45f, 0.75f, 0.4f), StoneDark);
    }

    private static void BuildCorner(BlockBuilder b)
    {
        b.Box(new(-0.4f, -1, -0.4f), new(1, 1, 0.4f), StoneColour);
        b.Box(new(-0.4f, -1, 0.4f), new(0.4f, 1, 1), StoneColour);
    }

    private static void BuildTowerBase(BlockBuilder b) =>
        b.Box(new(-0.8f, -1, -0.8f), new(0.8f, 1, 0.8f), StoneColour);

    private static void BuildTowerTop(BlockBuilder b)
    {
        b.Box(new(-0.8f, -1, -0.8f), new(0.8f, 0.2f, 0.8f), StoneColour);
        b.Box(new(-0.95f, 0.2f, -0.95f), new(0.95f, 0.55f, 0.95f), StoneDark);
    }

    private static void BuildStairs(BlockBuilder b)
    {
        const int steps = 4;
        float width = 2f / steps;
        for (int i = 0; i < steps; i++)
            b.Box(new(-1 + i * width, -1, -1), new(-1 + (i + 1) * width, -1 + (i + 1) * width, 1), i % 2 == 0 ? StoneColour : StoneDark);
    }

    private static void BuildBridgeHead(BlockBuilder b)
    {
        b.Box(new(-0.4f, -1, -0.4f), new(0.4f, 0.7f, 0.4f), StoneDark);
        b.Box(new(-0.7f, 0.7f, -0.7f), new(1, 1, 0.7f), WoodColour);
    }

    private static void BuildDeck(BlockBuilder b)
    {
        b.Box(new(-1, 0.7f, -0.7f), new(1, 1, 0.7f), WoodColour);
        b.Box(new(-1, 1, -0.7f), new(1, 1.25f, -0.6f), StoneDark);
        b.Box(new(-1, 1, 0.6f), new(1, 1.25f, 0.7f), StoneDark);
    }

    /// <summary>Builds one mesh from coloured convex parts.</summary>
    private sealed class BlockBuilder
    {
        private readonly SurfaceTool _tool = new();

        public BlockBuilder() => _tool.Begin(Mesh.PrimitiveType.Triangles);

        public ArrayMesh Commit()
        {
            var mesh = _tool.Commit();
            mesh.SurfaceSetMaterial(0, new StandardMaterial3D { VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled });
            return mesh;
        }

        public void Box(Vector3 min, Vector3 max, Color colour) => Hexahedron(
            new[] { new Vector3(max.X, max.Y, max.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, max.Y, min.Z) },
            new[] { new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, min.Y, min.Z) },
            colour);

        /// <summary>A full cell whose top corners have heights ordered (+X+Z, +X-Z, -X+Z, -X-Z).</summary>
        public void Slope(float a, float b, float c, float d, Color colour) => Hexahedron(
            new Vector3[] { new(1, a, 1), new(1, b, -1), new(-1, c, 1), new(-1, d, -1) },
            new Vector3[] { new(1, -1, 1), new(1, -1, -1), new(-1, -1, 1), new(-1, -1, -1) },
            colour);

        private void Hexahedron(Vector3[] t, Vector3[] l, Color colour)
        {
            var centre = Vector3.Zero;
            foreach (var point in t)
                centre += point;
            foreach (var point in l)
                centre += point;
            centre /= 8f;

            Quad(t[0], t[1], t[3], t[2], centre, colour);
            Quad(l[0], l[2], l[3], l[1], centre, colour);
            Quad(t[0], l[0], l[1], t[1], centre, colour);
            Quad(t[2], t[3], l[3], l[2], centre, colour);
            Quad(t[0], t[2], l[2], l[0], centre, colour);
            Quad(t[1], l[1], l[3], t[3], centre, colour);
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 centre, Color colour)
        {
            Triangle(a, b, c, centre, colour);
            Triangle(a, c, d, centre, colour);
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 centre, Color colour)
        {
            var normal = (c - a).Cross(b - a);
            if (normal.LengthSquared() < 1e-6f)
                return; // A side collapsed to a line at a low corner.
            if (normal.Dot((a + b + c) / 3f - centre) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            normal = normal.Normalized();
            foreach (var point in new[] { a, b, c })
            {
                _tool.SetColor(colour);
                _tool.SetNormal(normal);
                _tool.AddVertex(point);
            }
        }
    }
}
