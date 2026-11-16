using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godoodads.Curves.Core;

namespace Godoodads.Curves;

/// <summary>Whether Generate replaces the path's curve or appends the shape at its end (brief godoocurves §4.2).</summary>
public enum CurveMode
{
    Replace,
    Append,
}

/// <summary>
/// All options of one Generate. Shown in the dialog and stored on the Path3D afterwards, so reopening
/// the dialog continues where it left off (brief godoocurves §4.3, §5). Only the settings the chosen
/// shape uses are shown.
/// </summary>
[Tool, GlobalClass]
public partial class CurveSettings : Resource
{
    /// <summary>Metadata key under which a Path3D stores the settings of its last Generate.</summary>
    public const string MetaKey = "godoodads_curve_settings";

    private CurveMode _mode;
    private ShapeKind _shape;

    /// <summary>Replace the curve, or append the shape at its end so pieces join smoothly.</summary>
    [Export]
    public CurveMode Mode
    {
        get => _mode;
        set
        {
            bool changed = _mode != value;
            _mode = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }

    [Export]
    public ShapeKind Shape
    {
        get => _shape;
        set
        {
            bool changed = _shape != value;
            _shape = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }

    // ---- The shape's settings (brief §4.1); _ValidateProperty shows the ones the shape uses ----

    [ExportGroup("Shape Settings")]
    [Export(PropertyHint.Range, "0.01,1000,0.01,or_greater")] public float Length { get; set; } = 10;
    [Export(PropertyHint.Range, "-500,500,0.01,or_less,or_greater")] public float Height { get; set; } = 5;
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float Radius { get; set; } = 5;
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float Diameter { get; set; } = 6;
    [Export(PropertyHint.Range, "-100,100,0.01,or_less,or_greater")] public float SideShift { get; set; }
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float Amplitude { get; set; } = 3;
    [Export(PropertyHint.Range, "1,64,1,or_greater")] public int Cycles { get; set; } = 2;
    [Export(PropertyHint.Range, "0.25,64,0.25,or_greater")] public float Turns { get; set; } = 2;
    [Export(PropertyHint.Range, "1,360,1,degrees")] public float Angle { get; set; } = 90;
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float Offset { get; set; } = 3;
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float RadiusForward { get; set; } = 6;
    [Export(PropertyHint.Range, "0.01,500,0.01,or_greater")] public float RadiusSide { get; set; } = 3;
    [Export(PropertyHint.Range, "-500,500,0.01,or_less,or_greater")] public float Rise { get; set; } = 5;
    [Export(PropertyHint.Range, "-89,89,1,degrees")] public float Bank { get; set; } = 30;
    [Export(PropertyHint.Range, "0.01,1000,0.01,or_greater")] public float Width { get; set; } = 10;
    [Export(PropertyHint.Range, "0.01,1000,0.01,or_greater")] public float Depth { get; set; } = 6;
    [Export(PropertyHint.Range, "0,100,0.01,or_greater")] public float CornerRadius { get; set; } = 1;
    [Export(PropertyHint.Range, "3,64,1,or_greater")] public int Sides { get; set; } = 6;

    /// <summary>Which way the shape turns, seen heading along Direction: its plane is Direction and this side.</summary>
    [Export] public Bend Bend { get; set; } = Bend.Right;

    // ---- Placement in Replace mode (brief §4.2) ----

    [ExportGroup("Placement")]
    [Export] public Vector3 Start { get; set; }
    [Export] public Vector3 Direction { get; set; } = Vector3.Right;
    [Export] public Vector3 Up { get; set; } = Vector3.Up;

    /// <summary>The settings each shape uses, by property name (brief §4.1).</summary>
    public static readonly IReadOnlyDictionary<ShapeKind, string[]> UsedSettings = new System.Collections.Generic.Dictionary<ShapeKind, string[]>
    {
        [ShapeKind.Straight] = new[] { nameof(Length) },
        [ShapeKind.Parabolic] = new[] { nameof(Length), nameof(Height) },
        [ShapeKind.Circle] = new[] { nameof(Radius), nameof(Bend) },
        [ShapeKind.Semicircle] = new[] { nameof(Radius), nameof(Bend) },
        [ShapeKind.Loop] = new[] { nameof(Length), nameof(Diameter), nameof(SideShift) },
        [ShapeKind.Wave] = new[] { nameof(Length), nameof(Amplitude), nameof(Cycles), nameof(Bend) },
        [ShapeKind.Spiral] = new[] { nameof(Length), nameof(Radius), nameof(Turns) },
        [ShapeKind.Arc] = new[] { nameof(Radius), nameof(Angle), nameof(Bend) },
        [ShapeKind.SBend] = new[] { nameof(Length), nameof(Offset), nameof(Bend) },
        [ShapeKind.Ellipse] = new[] { nameof(RadiusForward), nameof(RadiusSide), nameof(Bend) },
        [ShapeKind.Helix] = new[] { nameof(Radius), nameof(Turns), nameof(Rise), nameof(Bend) },
        [ShapeKind.Corkscrew] = new[] { nameof(Length), nameof(Radius), nameof(Turns) },
        [ShapeKind.BankedTurn] = new[] { nameof(Radius), nameof(Angle), nameof(Bank), nameof(Bend) },
        [ShapeKind.RoundedRectangle] = new[] { nameof(Width), nameof(Depth), nameof(CornerRadius), nameof(Bend) },
        [ShapeKind.Polygon] = new[] { nameof(Sides), nameof(Radius), nameof(CornerRadius), nameof(Bend) },
        [ShapeKind.FigureEight] = new[] { nameof(Radius), nameof(Bend) },
    };

    private static readonly HashSet<string> ShapeSettings = UsedSettings.Values.SelectMany(n => n).ToHashSet();

    private static readonly HashSet<string> PlacementSettings = new() { nameof(Start), nameof(Direction), nameof(Up) };

    /// <summary>Readable names for the Shape drop-down, in the order of <see cref="ShapeKind"/>.</summary>
    public static readonly string[] ShapeLabels =
    {
        "Straight", "Parabolic", "Circle", "Semicircle", "Loop", "Wave", "Spiral", "Arc", "S-bend", "Ellipse",
        "Helix", "Corkscrew", "Banked Turn", "Rounded Rectangle", "Polygon", "Figure Eight",
    };

    /// <summary>The Core parameters for these settings.</summary>
    public ShapeParameters ToParameters() => new()
    {
        Length = Length,
        Height = Height,
        Radius = Radius,
        Diameter = Diameter,
        SideShift = SideShift,
        Amplitude = Amplitude,
        Cycles = Cycles,
        Turns = Turns,
        AngleDegrees = Angle,
        Offset = Offset,
        RadiusForward = RadiusForward,
        RadiusSide = RadiusSide,
        Rise = Rise,
        BankDegrees = Bank,
        Width = Width,
        Depth = Depth,
        CornerRadius = CornerRadius,
        Sides = Sides,
        Bend = Bend,
    };

    /// <summary>The frame of Replace mode, from Start, Direction and Up.</summary>
    public bool TryFrame(out ShapeFrame frame, out string error) => ShapeFrame.TryCreate(Start, Direction, Up, out frame, out error);

    /// <summary>Why Generate cannot run with these settings, or null.</summary>
    public string Check()
    {
        var parameters = ToParameters();
        if (Shapes.Check(Shape, parameters) is { } shapeError)
            return shapeError;
        if (Mode == CurveMode.Append && Shapes.IsClosed(Shape, parameters))
            return $"{ShapeLabels[(int)Shape]} is a closed shape, so it cannot be appended. Use Replace.";
        if (Mode == CurveMode.Replace && !TryFrame(out _, out string frameError))
            return frameError;
        return null;
    }

    /// <summary>The settings stored on a Path3D, or null.</summary>
    public static CurveSettings StoredOn(Path3D path) =>
        path.HasMeta(MetaKey) ? path.GetMeta(MetaKey).As<CurveSettings>() : null;

    /// <summary>Stores settings on a Path3D. Null removes the entry, so undo leaves nothing behind.</summary>
    public static void StoreOn(Path3D path, CurveSettings settings)
    {
        if (settings == null)
            path.RemoveMeta(MetaKey);
        else
            path.SetMeta(MetaKey, settings);
    }

    public override void _ValidateProperty(Dictionary property)
    {
        string name = property["name"].AsString();
        var usage = (PropertyUsageFlags)property["usage"].AsInt64();
        if (name == PropertyName.Shape)
        {
            property["hint"] = (long)PropertyHint.Enum;
            property["hint_string"] = string.Join(",", ShapeLabels.Select((label, i) => $"{label}:{i}"));
        }
        else if (name == PropertyName.Bend && Shape is ShapeKind.Helix or ShapeKind.BankedTurn)
        {
            // These turn around the up axis.
            property["hint"] = (long)PropertyHint.Enum;
            property["hint_string"] = $"Left:{(int)Bend.Left},Right:{(int)Bend.Right}";
        }
        if (ShapeSettings.Contains(name) && !UsedSettings[Shape].Contains(name))
            property["usage"] = (long)(usage & ~PropertyUsageFlags.Editor);
        else if (PlacementSettings.Contains(name) && Mode == CurveMode.Append)
            property["usage"] = (long)(usage & ~PropertyUsageFlags.Editor);
    }
}
