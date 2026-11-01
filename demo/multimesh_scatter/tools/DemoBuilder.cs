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

    private static StandardMaterial3D Ground() => new() { AlbedoColor = new Color(0.36f, 0.29f, 0.2f), Roughness = 1f };

    private void Build(string file, MeshInstance3D surfaceNode, ScatterSettings settings, Vector3 cameraPosition)
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
        var surface = SurfaceSource.FromNode(surfaceNode, settings.NormalSource == NormalSource.NormalMap, out var error, out _);
        var options = settings.ToOptions();
        var filters = new System.Collections.Generic.List<ICandidateFilter>();
        if (settings.MaskTexture != null)
            filters.Add(new MaskFilter(MaskSource.FromTexture(settings.MaskTexture, settings.MaskChannel, out error),
                settings.MaskMode, settings.MaskThreshold, settings.MaskInvert, settings.MaskUvSet == UvSet.Uv2));
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
