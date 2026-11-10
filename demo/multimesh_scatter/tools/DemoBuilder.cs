using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.Demo;

/// <summary>
/// Rebuilds the per-mode demo scenes with the same pipeline as the editor dialog. Run
/// <c>build_demos.tscn</c> (headless is fine); it saves the scenes and quits.
/// </summary>
public partial class DemoBuilder : Node
{
    private const string Dir = "res://demo/multimesh_scatter/";
    private const string Assets = Dir + "assets/";
    private const string Presets = "res://addons/multimesh_scatter/Presets/";

    public override void _Ready()
    {
        Build("demo_follow_surface.tscn", Sphere(), Preset("follow_surface.tres", s =>
        {
            s.Amount = 1500;
            s.MinDistance = 0.15f;
        }), new Vector3(0, 3, 9));

        Build("demo_mask.tscn", Terrain(), Settings(s =>
        {
            s.Amount = 4000;
            s.MinDistance = 0.08f;
            s.MaskTexture = GD.Load<Texture2D>(Assets + "mask_paths.png");
        }), new Vector3(0, 13, 13));

        Build("demo_normal_map.tscn", Terrain(), Settings(s =>
        {
            s.Amount = 2500;
            s.MinDistance = 0.2f;
            s.RandomScale = 0f;
            s.RandomTiltDegrees = 0f;
            s.Alignment = 0f;
            s.NormalSource = NormalSource.NormalMap;
            s.NormalMapTexture = GD.Load<Texture2D>(Assets + "normal_bumps.png");
        }), new Vector3(0, 13, 13));

        Build("demo_combined.tscn", Terrain(), Preset("grass_on_terrain.tres", s =>
        {
            s.Amount = 5000;
            s.MinDistance = 0.06f;
            s.MaskTexture = GD.Load<Texture2D>(Assets + "mask_paths.png");
            s.NormalSource = NormalSource.NormalMap;
            s.NormalMapTexture = GD.Load<Texture2D>(Assets + "normal_bumps.png");
        }), new Vector3(0, 13, 13));

        Build("demo_legacy.tscn", Plateau(), Preset("upward_faces_legacy.tres", s =>
        {
            s.Amount = 1500;
            s.MinDistance = 0.12f;
        }), new Vector3(6, 7, 9));

        // Brief §10.3: grass on a GridMap's open tops and ramps, not on paving, with a path
        // mask projected from above.
        Build("demo_gridmap.tscn", Tiles(), Settings(s =>
        {
            s.Amount = 6000;
            s.MinDistance = 0.12f;
            s.SlopeMaxDegrees = 50f;
            s.GridMapItems = new[] { "Paving" };
            s.GridMapItemMode = GridMapItemMode.Exclude;
            s.MaskTexture = GD.Load<Texture2D>(Assets + "mask_paths.png");
            s.MaskProjection = MaskProjection.TopDown;
            s.MaskInvert = true;   // the mask is white on the paths; grass grows everywhere else
        }), new Vector3(0, 20, 22));

        GetTree().Quit();
    }

    private static ScatterSettings Settings(System.Action<ScatterSettings> configure)
    {
        var settings = new ScatterSettings();
        configure(settings);
        return settings;
    }

    private static ScatterSettings Preset(string file, System.Action<ScatterSettings> configure)
    {
        var settings = (ScatterSettings)GD.Load<ScatterSettings>(Presets + file).Duplicate();
        configure(settings);
        return settings;
    }

    private static MeshInstance3D Terrain() => new()
    {
        Name = "Terrain",
        Mesh = new PlaneMesh { Size = new Vector2(20, 20), SubdivideWidth = 31, SubdivideDepth = 31, Material = Ground() },
    };

    private static MeshInstance3D Sphere() => new()
    {
        Name = "Sphere",
        Mesh = new SphereMesh { Radius = 2.5f, Height = 5f, Material = Ground() },
    };

    /// <summary>A cone with steep sides on a low box: only the box top and the gentle cone top qualify.</summary>
    private static MeshInstance3D Plateau()
    {
        var box = new BoxMesh { Size = new Vector3(8, 1, 8) };
        var cone = new CylinderMesh { TopRadius = 0.6f, BottomRadius = 2.5f, Height = 3f, RadialSegments = 32 };
        var tool = new SurfaceTool();
        tool.AppendFrom(box, 0, new Transform3D(Basis.Identity, new Vector3(0, -0.5f, 0)));
        tool.AppendFrom(cone, 0, new Transform3D(Basis.Identity, new Vector3(0, 1.5f, 0)));
        var mesh = tool.Commit();
        mesh.SurfaceSetMaterial(0, Ground());
        return new MeshInstance3D { Name = "Plateau", Mesh = mesh };
    }

    public const string TileLibraryPath = Assets + "gridmap_tiles.tres";
    public const int MapSize = 12;

    /// <summary>
    /// A small terrain of 2 m tiles: a ground layer with a paving strip, a plateau with ramps up
    /// to it, and a few blocks standing on the ground. Its centre is at the origin.
    /// </summary>
    private static GridMap Tiles()
    {
        var library = new MeshLibrary();
        library.CreateItem(0);
        library.SetItemName(0, "Block");
        library.SetItemMesh(0, new BoxMesh { Size = new Vector3(2, 2, 2), Material = Grassy() });
        library.CreateItem(1);
        library.SetItemName(1, "Paving");
        library.SetItemMesh(1, new BoxMesh { Size = new Vector3(2, 2, 2), Material = new StandardMaterial3D { AlbedoColor = new Color(0.62f, 0.6f, 0.56f), Roughness = 1f } });
        library.CreateItem(2);
        library.SetItemName(2, "Ramp");
        library.SetItemMesh(2, new PrismMesh { Size = new Vector3(2, 2, 2), LeftToRight = 1f, Material = Grassy() });
        library.TakeOverPath(TileLibraryPath);
        var saved = ResourceSaver.Save(library, TileLibraryPath);
        if (saved != Error.Ok)
            GD.PushError($"Could not save {TileLibraryPath}: {saved}");

        var map = new GridMap { Name = "Tiles", MeshLibrary = library, CellSize = new Vector3(2, 2, 2), Position = new Vector3(-MapSize, 0, -MapSize) };
        for (int x = 0; x < MapSize; x++)
            for (int z = 0; z < MapSize; z++)
                map.SetCellItem(new Vector3I(x, 0, z), z == 5 ? 1 : 0);
        for (int x = 7; x < MapSize; x++)
            for (int z = 0; z < 5; z++)
                map.SetCellItem(new Vector3I(x, 1, z), 0);
        for (int z = 0; z < 5; z++)
            map.SetCellItem(new Vector3I(6, 1, z), 2);   // rises towards +X onto the plateau
        foreach (var cell in new[] { new Vector3I(2, 1, 8), new Vector3I(3, 1, 8), new Vector3I(9, 1, 9) })
            map.SetCellItem(cell, 0);
        return map;
    }

    private static StandardMaterial3D Grassy() => new() { AlbedoColor = new Color(0.3f, 0.42f, 0.22f), Roughness = 1f };

    private static StandardMaterial3D Ground() => new() { AlbedoColor = new Color(0.36f, 0.29f, 0.2f), Roughness = 1f };

    private void Build(string file, Node3D surfaceNode, ScatterSettings settings, Vector3 cameraPosition)
    {
        var root = new Node3D { Name = file.GetBaseName().ToPascalCase() };
        AddChild(root); // global transforms need the tree
        var sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() };
        root.AddChild(new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Sky, Sky = sky, AmbientLightSource = Godot.Environment.AmbientSource.Sky },
        });
        root.AddChild(new DirectionalLight3D
        {
            Name = "Sun",
            ShadowEnabled = true,
            Transform = new Transform3D(Basis.FromEuler(new Vector3(Mathf.DegToRad(-50), Mathf.DegToRad(30), 0)), Vector3.Zero),
        });
        root.AddChild(new Camera3D
        {
            Name = "Camera3D",
            Transform = new Transform3D(Basis.Identity, cameraPosition).LookingAt(Vector3.Zero, Vector3.Up),
        });
        root.AddChild(surfaceNode);

        var blade = new PrismMesh
        {
            Size = new Vector3(0.08f, 0.45f, 0.04f),
            Material = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.7f, 0.25f), CullMode = BaseMaterial3D.CullModeEnum.Disabled },
        };
        var scatter = new MultiMeshInstance3D
        {
            Name = "Scatter",
            Multimesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = blade },
        };
        root.AddChild(scatter);

        // Same steps as the editor dialog, minus undo.
        bool tangents = settings.NormalSource == NormalSource.NormalMap;
        var surface = surfaceNode is GridMap map
            ? SurfaceSource.FromGridMap(map, settings.ToGridMapSelection(), tangents, out var error, out _, out _)
            : SurfaceSource.FromNode(surfaceNode, tangents, out error, out _);
        var options = settings.ToOptions();
        var filters = new System.Collections.Generic.List<ICandidateFilter>();
        if (settings.MaskTexture != null)
            filters.Add(new MaskFilter(MaskSource.FromTexture(settings.MaskTexture, settings.MaskChannel, out error),
                settings.MaskMode, settings.MaskThreshold, settings.MaskInvert, settings.MaskUvSet == UvSet.Uv2,
                settings.MaskProjection == MaskProjection.TopDown ? MaskFilter.TopDownArea(surface.Positions) : null));
        if (settings.NormalSource == NormalSource.NormalMap)
            options.NormalMap = new NormalMapOptions
            {
                Map = NormalMapSource.FromTexture(settings.NormalMapTexture, settings.NormalMapSpace, settings.NormalMapFlipGreen, out error),
                Space = settings.NormalMapSpace,
                Strength = settings.NormalMapStrength,
            };
        if (error != null)
            GD.PushError($"{file}: {error}");
        var result = ScatterPipeline.Run(surface, options, filters);
        scatter.Multimesh = MultiMeshWriter.Build(scatter.Multimesh, result.Transforms, append: false);

        settings.SurfacePath = new NodePath("../" + surfaceNode.Name);
        scatter.SetMeta(ScatterSettings.MetaKey, settings);

        foreach (var node in root.FindChildren("*", "", true, false))
            node.Owner = root;
        var scene = new PackedScene();
        scene.Pack(root);
        var saveError = ResourceSaver.Save(scene, Dir + file);
        GD.Print($"DEMO {file}: placed {result.Placed} / {result.Requested}, save {saveError}");
        RemoveChild(root);
        root.Free();
    }
}
