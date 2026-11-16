using System;
using System.Collections.Generic;
using Godot;
using Godoodads.Curves.Adapters;
using Godoodads.Curves.Core;

namespace Godoodads.Curves.Demo;

/// <summary>
/// Builds the godoocurves demo scenes (brief godoocurves §7, M4) with the addon's own code: a gallery
/// of every shape, a coaster appended piece by piece, and closed outlines in use. Every path stores
/// its <see cref="CurveSettings"/>, so the dialog can continue from it. Run it headless:
/// godot --headless --path . res://demo/godoocurves/tools/build_demo.tscn
/// </summary>
public partial class BuildCurvesDemo : Node
{
    public const string SceneDirectory = "res://demo/godoocurves";

    private static readonly Color Sky = new(0.78f, 0.85f, 0.92f);
    private static readonly Color Grass = new(0.36f, 0.55f, 0.3f);
    private static readonly Color Blue = new(0.25f, 0.55f, 0.85f);
    private static readonly Color Red = new(0.8f, 0.2f, 0.18f);
    private static readonly Color Steel = new(0.55f, 0.57f, 0.6f);
    private static readonly Color Yellow = new(0.95f, 0.8f, 0.15f);
    private static readonly Color White = new(0.92f, 0.92f, 0.9f);
    private static readonly Color Brick = new(0.6f, 0.32f, 0.22f);
    private static readonly Color Stone = new(0.6f, 0.58f, 0.55f);
    private static readonly Color Clay = new(0.8f, 0.45f, 0.25f);
    private static readonly Color Water = new(0.2f, 0.45f, 0.65f);

    /// <summary>A U-shaped channel open toward up, so up and tilt are easy to see.</summary>
    private static readonly Vector2[] Channel =
    {
        new(-1, 0), new(1, 0), new(1, 0.5f), new(0.85f, 0.5f), new(0.85f, 0.12f), new(-0.85f, 0.12f), new(-0.85f, 0.5f), new(-1, 0.5f),
    };

    /// <summary>A coaster deck with two rails on top.</summary>
    private static readonly Vector2[] Rails =
    {
        new(-0.75f, 0), new(0.75f, 0), new(0.75f, 0.32f), new(0.55f, 0.32f), new(0.55f, 0.14f), new(-0.55f, 0.14f), new(-0.55f, 0.32f), new(-0.75f, 0.32f),
    };

    private static Vector2[] Box(float halfWidth, float height) =>
        new[] { new Vector2(-halfWidth, 0), new Vector2(halfWidth, 0), new Vector2(halfWidth, height), new Vector2(-halfWidth, height) };

    public override void _Ready()
    {
        bool ok = Save(BuildShapes(), "shapes.tscn") & Save(BuildCoaster(), "coaster.tscn") & Save(BuildOutlines(), "outlines.tscn");
        GetTree().Quit(ok ? 0 : 1);
    }

    private static bool Save(Node3D root, string file)
    {
        SetOwners(root, root);
        var scene = new PackedScene();
        var error = scene.Pack(root);
        string path = $"{SceneDirectory}/{file}";
        if (error == Error.Ok)
            error = ResourceSaver.Save(scene, path);
        GD.Print(error == Error.Ok ? $"Saved {path}" : $"Could not save {path}: {error}");
        root.Free();
        return error == Error.Ok;
    }

    // ---- shapes.tscn ----------------------------------------------------------------------------

    /// <summary>Every shape at the dialog's defaults, four per row, 40 m apart.</summary>
    private static Node3D BuildShapes()
    {
        var root = World("Shapes", new Vector3(60, 90, 150), new Vector3(-35, 0, 0), ground: false);
        var kinds = Enum.GetValues<ShapeKind>();
        for (int i = 0; i < kinds.Length; i++)
        {
            var settings = new CurveSettings { Shape = kinds[i] };
            var path = PathFor(settings, CurveSettings.ShapeLabels[i], new Vector3(i % 4 * 40, 0, i / 4 * 40));
            path.AddChild(Profile(Channel, Blue, path.Curve.Closed));
            path.AddChild(new Label3D
            {
                Name = "Name", Text = CurveSettings.ShapeLabels[i], Position = new Vector3(0, 10, 0), FontSize = 96, PixelSize = 0.03f, OutlineSize = 24,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            });
            root.AddChild(path);
        }
        return root;
    }

    // ---- coaster.tscn ---------------------------------------------------------------------------

    /// <summary>
    /// A ride appended piece by piece, as Append mode does it: a lift and a drop made of vertical
    /// arcs and straights, banked turns, a loop, a wave, a corkscrew and an S-bend. A three-car train
    /// with an onboard camera rides it when the scene runs.
    /// </summary>
    private static Node3D BuildCoaster()
    {
        var root = World("Coaster", new Vector3(30, 55, 95), new Vector3(-32, 0, 0), ground: true);
        root.GetNode<Camera3D>("OverviewCamera").Current = false;   // running the scene rides the train
        var pieces = new List<CurveSettings>
        {
            Piece(ShapeKind.Straight, s => s.Length = 12),
            Piece(ShapeKind.Arc, s => { s.Radius = 20; s.Angle = 30; s.Bend = Bend.Up; }),
            Piece(ShapeKind.Straight, s => s.Length = 24),
            Piece(ShapeKind.Arc, s => { s.Radius = 20; s.Angle = 30; s.Bend = Bend.Down; }),
            Piece(ShapeKind.Straight, s => s.Length = 4),
            Piece(ShapeKind.Arc, s => { s.Radius = 15; s.Angle = 50; s.Bend = Bend.Down; }),
            Piece(ShapeKind.Straight, s => s.Length = 8),
            Piece(ShapeKind.Arc, s => { s.Radius = 15; s.Angle = 50; s.Bend = Bend.Up; }),
            Piece(ShapeKind.BankedTurn, s => { s.Radius = 15; s.Angle = 180; s.Bank = 40; s.Bend = Bend.Left; }),
            Piece(ShapeKind.Loop, s => { s.Length = 30; s.Diameter = 14; s.SideShift = 3; }),
            Piece(ShapeKind.Wave, s => { s.Length = 40; s.Amplitude = 3; s.Cycles = 2; s.Bend = Bend.Up; }),
            Piece(ShapeKind.Corkscrew, s => { s.Length = 30; s.Radius = 3.5f; s.Turns = 1; }),
            Piece(ShapeKind.BankedTurn, s => { s.Radius = 12; s.Angle = 180; s.Bank = 40; s.Bend = Bend.Right; }),
            Piece(ShapeKind.SBend, s => { s.Length = 24; s.Offset = 6; s.Bend = Bend.Left; }),
            Piece(ShapeKind.Straight, s => s.Length = 10),
        };
        var first = pieces[0];
        first.Mode = CurveMode.Replace;
        first.Start = new Vector3(0, 5, 0);
        Curve3D curve = null;
        foreach (var settings in pieces)
        {
            var shape = Shapes.Build(settings.Shape, settings.ToParameters());
            if (!shape.IsValid)
                throw new InvalidOperationException($"{settings.Shape}: {shape.Error}");
            if (settings.Mode == CurveMode.Replace)
            {
                settings.TryFrame(out var frame, out _);
                curve = CurveWriter.Create(shape.Curve.ToPath(frame));
                continue;
            }
            var written = CurveWriter.Append(curve, shape.Curve);
            if (!written.IsValid)
                throw new InvalidOperationException($"{settings.Shape}: {written.Error}");
            curve = written.Curve;
        }

        var track = new Path3D { Name = "Track", Curve = curve };
        CurveSettings.StoreOn(track, pieces[^1]);
        root.AddChild(track);
        track.AddChild(Profile(Rails, Red, false));
        AddPillars(root, curve);
        AddTrain(root, track, curve.GetBakedLength());
        return root;
    }

    private static CurveSettings Piece(ShapeKind kind, Action<CurveSettings> set)
    {
        var settings = new CurveSettings { Shape = kind, Mode = CurveMode.Append };
        set(settings);
        return settings;
    }

    /// <summary>Steel pillars from the ground up to the track, wherever it runs upright above the ground.</summary>
    private static void AddPillars(Node3D root, Curve3D curve)
    {
        var pillars = new Node3D { Name = "Pillars" };
        root.AddChild(pillars);
        var material = new StandardMaterial3D { AlbedoColor = Steel, Metallic = 0.6f, Roughness = 0.4f };
        float length = curve.GetBakedLength();
        int count = 0;
        for (float offset = 3; offset < length; offset += 6)
        {
            var transform = curve.SampleBakedWithRotation(offset, false, true);
            if (transform.Basis.Y.Y < 0.75f || transform.Origin.Y < 1)
                continue;
            float height = transform.Origin.Y - 0.05f;
            pillars.AddChild(new MeshInstance3D
            {
                Name = $"Pillar{++count}",
                Position = new Vector3(transform.Origin.X, height / 2, transform.Origin.Z),
                Mesh = new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.25f, Height = height, Material = material },
            });
        }
    }

    /// <summary>Three cars on PathFollow3D (rotation mode XYZ, so they follow the curve's up and tilt), the first with a camera.</summary>
    private static void AddTrain(Node3D root, Path3D track, float length)
    {
        const float seconds = 45, spacing = 2.6f;
        var animation = new Animation { Length = seconds, LoopMode = Animation.LoopModeEnum.Linear };
        var carMaterial = new StandardMaterial3D { AlbedoColor = Yellow };
        for (int car = 0; car < 3; car++)
        {
            var follow = new PathFollow3D
            {
                Name = $"Car{car + 1}",
                // XYZ, not Oriented: Oriented orients by world up and flips in loops (README).
                RotationMode = PathFollow3D.RotationModeEnum.Xyz,
                TiltEnabled = true,
                Loop = true,
            };
            track.AddChild(follow);
            follow.AddChild(new MeshInstance3D
            {
                Name = "Body", Position = new Vector3(0, 0.75f, 0),
                Mesh = new BoxMesh { Size = new Vector3(1.3f, 0.7f, 2.2f), Material = carMaterial },
            });
            if (car == 0)
                follow.AddChild(new Camera3D { Name = "RideCamera", Position = new Vector3(0, 1.7f, 0.3f), Fov = 80, Current = true });
            int t = animation.AddTrack(Animation.TrackType.Value);
            animation.TrackSetPath(t, $"Track/Car{car + 1}:progress");
            float start = (2 - car) * spacing;
            animation.TrackInsertKey(t, 0, start);
            animation.TrackInsertKey(t, seconds, start + length);
            animation.TrackSetInterpolationLoopWrap(t, false);
        }
        var library = new AnimationLibrary();
        library.AddAnimation("ride", animation);
        var player = new AnimationPlayer { Name = "TrainPlayer", Autoplay = "ride" };
        player.AddAnimationLibrary("", library);
        root.AddChild(player);
    }

    // ---- outlines.tscn --------------------------------------------------------------------------

    /// <summary>Closed shapes in use: a fence, a flower-bed border, a pond rim and a running track.</summary>
    private static Node3D BuildOutlines()
    {
        var root = World("Outlines", new Vector3(5, 55, 55), new Vector3(-45, 0, 0), ground: true);

        var fence = PathFor(new CurveSettings { Shape = ShapeKind.RoundedRectangle, Width = 40, Depth = 28, CornerRadius = 5, Bend = Bend.Left, Start = new Vector3(0, 0, 14) }, "Fence", Vector3.Zero);
        fence.AddChild(Profile(Box(0.06f, 1.3f), White, true));
        root.AddChild(fence);

        var bed = PathFor(new CurveSettings { Shape = ShapeKind.Polygon, Sides = 6, Radius = 4, CornerRadius = 0.6f, Bend = Bend.Left, Start = new Vector3(-10, 0, 3.4f) }, "FlowerBed", Vector3.Zero);
        bed.AddChild(Profile(Box(0.2f, 0.35f), Brick, true));
        root.AddChild(bed);

        var pond = PathFor(new CurveSettings { Shape = ShapeKind.Ellipse, RadiusForward = 6, RadiusSide = 4, Bend = Bend.Left, Start = new Vector3(8, 0, 4) }, "Pond", Vector3.Zero);
        pond.AddChild(Profile(Box(0.35f, 0.3f), Stone, true));
        pond.AddChild(new MeshInstance3D
        {
            Name = "Water", Position = new Vector3(8, 0.15f, 0),
            Mesh = new CylinderMesh { TopRadius = 1, BottomRadius = 1, Height = 0.02f, Material = new StandardMaterial3D { AlbedoColor = Water } },
            Scale = new Vector3(6, 1, 4),
        });
        root.AddChild(pond);

        var running = PathFor(new CurveSettings { Shape = ShapeKind.FigureEight, Radius = 4, Bend = Bend.Right, Start = new Vector3(0, 0, -40), Direction = Vector3.Right }, "RunningTrack", Vector3.Zero);
        running.AddChild(Profile(Box(0.8f, 0.04f), Clay, true));
        root.AddChild(running);
        return root;
    }

    // ---- helpers --------------------------------------------------------------------------------

    /// <summary>A path whose curve the settings generate in Replace mode, with the settings stored on it.</summary>
    private static Path3D PathFor(CurveSettings settings, string name, Vector3 position)
    {
        var shape = Shapes.Build(settings.Shape, settings.ToParameters());
        if (!shape.IsValid || !settings.TryFrame(out var frame, out string error))
            throw new InvalidOperationException($"{name}: {shape.Error}");
        var path = new Path3D { Name = name.Replace(" ", "").Replace("-", ""), Position = position, Curve = CurveWriter.Create(shape.Curve.ToPath(frame)) };
        CurveSettings.StoreOn(path, settings);
        return path;
    }

    /// <summary>A cross-section swept along the parent path, following its up and tilt.</summary>
    private static CsgPolygon3D Profile(Vector2[] polygon, Color colour, bool closed) => new()
    {
        Name = "Profile",
        Polygon = polygon,
        Mode = CsgPolygon3D.ModeEnum.Path,
        PathNode = "..",
        PathLocal = true,
        PathRotation = CsgPolygon3D.PathRotationEnum.PathFollow,
        PathIntervalType = CsgPolygon3D.PathIntervalTypeEnum.Distance,
        PathInterval = 0.25f,
        PathJoined = closed,
        Material = new StandardMaterial3D { AlbedoColor = colour },
    };

    private static Node3D World(string name, Vector3 cameraPosition, Vector3 cameraRotation, bool ground)
    {
        var root = new Node3D { Name = name };
        root.AddChild(new DirectionalLight3D { Name = "Sun", RotationDegrees = new Vector3(-50, -30, 0), ShadowEnabled = true });
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = Sky,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.6f, 0.62f, 0.66f),
        };
        root.AddChild(new WorldEnvironment { Name = "Environment", Environment = environment });
        root.AddChild(new Camera3D { Name = "OverviewCamera", Position = cameraPosition, RotationDegrees = cameraRotation, Current = true });
        if (ground)
            root.AddChild(new MeshInstance3D
            {
                Name = "Ground",
                Mesh = new PlaneMesh { Size = new Vector2(400, 400), Material = new StandardMaterial3D { AlbedoColor = Grass } },
            });
        return root;
    }

    private static void SetOwners(Node node, Node owner)
    {
        foreach (var child in node.GetChildren())
        {
            child.Owner = owner;
            SetOwners(child, owner);
        }
    }
}
