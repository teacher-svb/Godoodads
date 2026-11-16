using System;
using Godot;
using Godoodads.Curves.Adapters;
using Godoodads.Curves.Core;

namespace Godoodads.Curves.Demo;

/// <summary>
/// Builds the godoocurves M2 review scene (card crv-m2-review): every shape at its default settings on
/// its own Path3D, and a track appended from pieces with a cart that follows its tilt. A U-shaped
/// channel along each path shows which way is up. Run it headless:
/// godot --headless --path . res://demo/godoocurves/tools/build_review.tscn
/// </summary>
public partial class BuildCurvesReview : Node
{
    public const string ScenePath = "res://demo/godoocurves/m2_review.tscn";

    private static readonly Color ShapeColour = new(0.25f, 0.55f, 0.85f);
    private static readonly Color TrackColour = new(0.85f, 0.45f, 0.2f);
    private static readonly Color CartColour = new(0.9f, 0.85f, 0.2f);

    /// <summary>The channel's cross-section: a floor and two low walls, open toward up.</summary>
    private static readonly Vector2[] Channel =
    {
        new(-1, 0), new(1, 0), new(1, 0.5f), new(0.85f, 0.5f), new(0.85f, 0.12f), new(-0.85f, 0.12f), new(-0.85f, 0.5f), new(-1, 0.5f),
    };

    public override void _Ready()
    {
        var root = new Node3D { Name = "M2Review" };
        AddEnvironment(root);
        AddShapes(root);
        AddTrack(root);
        SetOwners(root, root);

        var scene = new PackedScene();
        var error = scene.Pack(root);
        if (error == Error.Ok)
            error = ResourceSaver.Save(scene, ScenePath);
        GD.Print(error == Error.Ok ? $"Saved {ScenePath}" : $"Could not save {ScenePath}: {error}");
        root.Free();
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private static void AddEnvironment(Node3D root)
    {
        root.AddChild(new DirectionalLight3D { Name = "Sun", RotationDegrees = new Vector3(-50, -30, 0), ShadowEnabled = true });
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.82f, 0.86f, 0.9f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.6f, 0.62f, 0.66f),
        };
        root.AddChild(new WorldEnvironment { Name = "Environment", Environment = environment });
        root.AddChild(new Camera3D { Name = "Camera", Position = new Vector3(60, 90, 150), RotationDegrees = new Vector3(-35, 0, 0), Current = true });
    }

    /// <summary>Every shape at the dialog's default settings, four per row, 40 m apart.</summary>
    private static void AddShapes(Node3D root)
    {
        var shapes = new Node3D { Name = "Shapes" };
        root.AddChild(shapes);
        var kinds = Enum.GetValues<ShapeKind>();
        for (int i = 0; i < kinds.Length; i++)
        {
            var settings = new CurveSettings { Shape = kinds[i] };
            var result = Shapes.Build(kinds[i], settings.ToParameters());
            if (!result.IsValid)
            {
                GD.PushError($"{kinds[i]}: {result.Error}");
                continue;
            }
            var path = new Path3D
            {
                Name = CurveSettings.ShapeLabels[i].Replace(" ", "").Replace("-", ""),
                Position = new Vector3(i % 4 * 40, 0, i / 4 * 40),
                Curve = CurveWriter.Create(result.Curve.ToPath(ShapeFrame.Default)),
            };
            CurveSettings.StoreOn(path, settings);
            path.AddChild(ChannelAlong(ShapeColour));
            path.AddChild(new Label3D
            {
                Name = "Name", Text = CurveSettings.ShapeLabels[i], Position = new Vector3(0, 8, 0), FontSize = 96,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            });
            shapes.AddChild(path);
        }
    }

    /// <summary>A track appended from pieces, as Append mode will do it, with a cart that runs along it.</summary>
    private static void AddTrack(Node3D root)
    {
        var pieces = new (ShapeKind Kind, ShapeParameters P)[]
        {
            (ShapeKind.Straight, new() { Length = 12 }),
            (ShapeKind.BankedTurn, new() { Radius = 12, AngleDegrees = 90, BankDegrees = 35, Bend = Bend.Left }),
            (ShapeKind.Loop, new() { Length = 24, Diameter = 12, SideShift = 3 }),
            (ShapeKind.Wave, new() { Length = 40, Amplitude = 4, Cycles = 2, Bend = Bend.Up }),
            (ShapeKind.Corkscrew, new() { Length = 24, Radius = 3, Turns = 1 }),
            (ShapeKind.Helix, new() { Radius = 10, Turns = 1, Rise = 10, Bend = Bend.Left }),
            (ShapeKind.SBend, new() { Length = 20, Offset = 6, Bend = Bend.Right }),
            (ShapeKind.Straight, new() { Length = 12 }),
        };
        var curve = new Curve3D();
        foreach (var (kind, p) in pieces)
        {
            var shape = Shapes.Build(kind, p);
            var written = CurveWriter.Append(curve, shape.Curve);
            if (!written.IsValid)
            {
                GD.PushError($"{kind}: {written.Error}");
                return;
            }
            curve = written.Curve;
        }
        var track = new Path3D { Name = "Track", Position = new Vector3(0, 0, -90), Curve = curve };
        root.AddChild(track);
        track.AddChild(ChannelAlong(TrackColour));

        var cart = new PathFollow3D { Name = "Cart", RotationMode = PathFollow3D.RotationModeEnum.Oriented, TiltEnabled = true, Loop = true };
        track.AddChild(cart);
        var body = new MeshInstance3D
        {
            Name = "Body", Position = new Vector3(0, 0.55f, 0),
            Mesh = new BoxMesh { Size = new Vector3(1.4f, 0.7f, 2.2f), Material = new StandardMaterial3D { AlbedoColor = CartColour } },
        };
        cart.AddChild(body);
        body.AddChild(new MeshInstance3D
        {
            Name = "UpFin", Position = new Vector3(0, 0.8f, 0),
            Mesh = new BoxMesh { Size = new Vector3(0.15f, 0.9f, 1.2f), Material = new StandardMaterial3D { AlbedoColor = TrackColour } },
        });

        var animation = new Animation { Length = 20, LoopMode = Animation.LoopModeEnum.Linear };
        int t = animation.AddTrack(Animation.TrackType.Value);
        animation.TrackSetPath(t, "Track/Cart:progress_ratio");
        animation.TrackInsertKey(t, 0, 0f);
        animation.TrackInsertKey(t, 20, 1f);
        var library = new AnimationLibrary();
        library.AddAnimation("run", animation);
        var player = new AnimationPlayer { Name = "CartPlayer", Autoplay = "run" };
        player.AddAnimationLibrary("", library);
        root.AddChild(player);
    }

    private static CsgPolygon3D ChannelAlong(Color colour) => new()
    {
        Name = "Channel",
        Polygon = Channel,
        Mode = CsgPolygon3D.ModeEnum.Path,
        PathNode = "..",
        PathLocal = true,
        PathRotation = CsgPolygon3D.PathRotationEnum.PathFollow,
        PathIntervalType = CsgPolygon3D.PathIntervalTypeEnum.Distance,
        PathInterval = 0.25f,
        PathJoined = false,
        Material = new StandardMaterial3D { AlbedoColor = colour },
    };

    private static void SetOwners(Node node, Node owner)
    {
        foreach (var child in node.GetChildren())
        {
            child.Owner = owner;
            SetOwners(child, owner);
        }
    }
}
