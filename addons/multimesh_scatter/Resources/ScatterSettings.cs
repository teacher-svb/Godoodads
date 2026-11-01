using Godot;
using Godot.Collections;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter;

public enum OutputMode
{
    /// <summary>Remove the current instances.</summary>
    Replace,
    /// <summary>Keep the current instances and add the new ones after them.</summary>
    Append,
}

public enum UvSet { Uv1, Uv2 }

public enum SlopeReferenceMode
{
    /// <summary>Measure the slope against the orientation's target direction.</summary>
    TargetDirection,
    /// <summary>Measure the slope against <see cref="ScatterSettings.SlopeCustomDirection"/>.</summary>
    Custom,
}

/// <summary>
/// All options of one scatter run. Shown in the scatter dialog's embedded inspector, stored on the
/// MultiMeshInstance3D after populating, and saveable as a .tres preset.
/// </summary>
[Tool, GlobalClass]
public partial class ScatterSettings : Resource
{
    /// <summary>Metadata key under which a MultiMeshInstance3D stores the settings of its last populate.</summary>
    public const string MetaKey = "godoodads_scatter_settings";

    // Main section: output mode first, then amount, seed and spacing. The foldouts follow in the
    // order the board chose (M5 review): Randomisation, Orientation, Normals, Placement Mask, Surface.
    [Export] public OutputMode Mode { get; set; } = OutputMode.Replace;
    [Export(PropertyHint.Range, "1,100000,1,or_greater")] public int Amount { get; set; } = 128;
    [Export] public int Seed { get; set; }
    [ExportToolButton("Randomise Seed", Icon = "RandomNumberGenerator")]
    public Callable RandomiseSeedButton => Callable.From(RandomiseSeed);

    [Export(PropertyHint.Range, "0,100,0.01,or_greater,suffix:m")]
    public float MinDistance
    {
        get => _minDistance;
        set
        {
            if (_minDistance > 0 == value > 0)
            {
                _minDistance = value;
                return;
            }
            _minDistance = value;
            NotifyPropertyListChanged();
        }
    }
    /// <summary>Candidates generated per instance; higher spreads better but is slower.</summary>
    [Export(PropertyHint.Range, "1,20,1")] public int CandidateFactor { get; set; } = 5;
    /// <summary>Also keep distance from instances of sibling MultiMeshInstance3D and MeshInstance3D nodes.</summary>
    [Export] public bool AvoidSiblings { get; set; } = true;

    [ExportGroup("Randomisation")]
    [Export(PropertyHint.Range, "0.001,100,0.001,or_greater")] public float BaseScale { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,10,0.01,or_greater")] public float RandomScale { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "0,360,0.1,degrees")] public float RandomYawDegrees { get; set; } = 360f;
    [Export(PropertyHint.Range, "0,90,0.1,degrees")] public float RandomTiltDegrees { get; set; }

    [ExportGroup("Orientation")]
    /// <summary>Target direction V (world space). Alignment 1 points the instances along it.</summary>
    [Export] public Vector3 TargetDirection { get; set; } = Vector3.Up;
    [ExportToolButton("+Y")] public Callable TargetUpButton => Callable.From(SetTargetUp);
    [ExportToolButton("-Y")] public Callable TargetDownButton => Callable.From(SetTargetDown);
    [ExportToolButton("+X")] public Callable TargetRightButton => Callable.From(SetTargetRight);
    [ExportToolButton("+Z")] public Callable TargetBackButton => Callable.From(SetTargetBack);
    /// <summary>0 follows the surface normal, 1 follows the target direction.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float Alignment { get; set; } = 1f;
    /// <summary>Per-instance random offset on Alignment, up to this amount either way.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float AlignmentRandomness { get; set; }
    /// <summary>Local axis of the instanced mesh that points "up".</summary>
    [Export] public MeshAxis MeshUpAxis { get; set; } = MeshAxis.PositiveY;

    [ExportGroup("Normals")]
    [Export]
    public NormalSource NormalSource
    {
        get => _normalSource;
        set
        {
            bool changed = _normalSource != value;
            _normalSource = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }
    /// <summary>Normal map texture; Godot expects OpenGL-style (Y+) maps, use Flip Green for DirectX-style ones.</summary>
    [Export] public Texture2D NormalMapTexture { get; set; }
    [Export] public UvSet NormalMapUvSet { get; set; } = UvSet.Uv1;
    [Export] public NormalMapSpace NormalMapSpace { get; set; } = NormalMapSpace.Tangent;
    [Export] public bool NormalMapFlipGreen { get; set; }
    /// <summary>0 ignores the map, 1 uses the full map normal.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float NormalMapStrength { get; set; } = 1f;

    [ExportGroup("Placement Mask")]
    /// <summary>Black and white texture sampled at each candidate's UV; empty disables the mask.</summary>
    [Export]
    public Texture2D MaskTexture
    {
        get => _maskTexture;
        set
        {
            bool changed = (_maskTexture == null) != (value == null);
            _maskTexture = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }
    [Export] public MaskChannel MaskChannel { get; set; } = MaskChannel.Luminance;
    [Export] public UvSet MaskUvSet { get; set; } = UvSet.Uv1;
    [Export]
    public MaskMode MaskMode
    {
        get => _maskMode;
        set
        {
            bool changed = _maskMode != value;
            _maskMode = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }
    /// <summary>Threshold mode: place only where the mask value is at least this.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float MaskThreshold { get; set; } = 0.5f;
    [Export] public bool MaskInvert { get; set; }

    [ExportGroup("Surface")]
    [Export] public SamplerMode Sampler { get; set; } = SamplerMode.AreaWeighted;
    [ExportSubgroup("Slope Filter")]
    /// <summary>Only place where the face normal is at least this many degrees from the slope reference.</summary>
    [Export(PropertyHint.Range, "0,180,0.1,degrees")] public float SlopeMinDegrees { get; set; }
    /// <summary>Only place where the face normal is at most this many degrees from the slope reference.</summary>
    [Export(PropertyHint.Range, "0,180,0.1,degrees")] public float SlopeMaxDegrees { get; set; } = 180f;
    [Export]
    public SlopeReferenceMode SlopeReference
    {
        get => _slopeReference;
        set
        {
            bool changed = _slopeReference != value;
            _slopeReference = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }
    [Export] public Vector3 SlopeCustomDirection { get; set; } = Vector3.Up;

    /// <summary>Surface node, relative to the MultiMeshInstance3D. Stored, not shown in the inspector.</summary>
    [Export] public NodePath SurfacePath { get; set; } = new();

    private float _minDistance;
    private SlopeReferenceMode _slopeReference;
    private Texture2D _maskTexture;
    private MaskMode _maskMode;
    private NormalSource _normalSource;

    // Named methods rather than lambdas: lambda names change between builds, which breaks reloads.
    private void SetTargetUp() => SetTarget(Vector3.Up);
    private void SetTargetDown() => SetTarget(Vector3.Down);
    private void SetTargetRight() => SetTarget(Vector3.Right);
    private void SetTargetBack() => SetTarget(Vector3.Back);

    private void SetTarget(Vector3 direction)
    {
        TargetDirection = direction;
        NotifyPropertyListChanged();
    }

    public void RandomiseSeed()
    {
        Seed = (int)(GD.Randi() & 0x7FFFFFFF);
        NotifyPropertyListChanged();
    }

    // Readable labels for enum dropdowns; the stored values stay the enum's integers.
    private static readonly System.Collections.Generic.Dictionary<string, string> EnumLabels = new()
    {
        [PropertyName.Sampler] = "Area Weighted:0,Uniform Per Triangle:1",
        [PropertyName.SlopeReference] = "Target Direction:0,Custom:1",
        [PropertyName.MaskUvSet] = "UV1:0,UV2:1",
        [PropertyName.NormalMapUvSet] = "UV1:0,UV2:1",
        [PropertyName.NormalSource] = "Face:0,Smooth:1,Normal Map:2",
        [PropertyName.MeshUpAxis] = "+X:0,-X:1,+Y:2,-Y:3,+Z:4,-Z:5",
    };

    public override void _ValidateProperty(Dictionary property)
    {
        if (EnumLabels.TryGetValue(property["name"].AsString(), out var labels))
            property["hint_string"] = labels;

        // Storage-only properties are saved but not shown in the inspector.
        string name = property["name"].AsString();
        if (name == PropertyName.SurfacePath
            || (_minDistance <= 0 && (name == PropertyName.CandidateFactor || name == PropertyName.AvoidSiblings))
            || (_slopeReference != SlopeReferenceMode.Custom && name == PropertyName.SlopeCustomDirection)
            || (_maskTexture == null && (name == PropertyName.MaskChannel || name == PropertyName.MaskUvSet
                || name == PropertyName.MaskMode || name == PropertyName.MaskThreshold || name == PropertyName.MaskInvert))
            || (_maskMode == MaskMode.Density && name == PropertyName.MaskThreshold)
            || (_normalSource != NormalSource.NormalMap && (name == PropertyName.NormalMapTexture || name == PropertyName.NormalMapUvSet
                || name == PropertyName.NormalMapSpace || name == PropertyName.NormalMapFlipGreen || name == PropertyName.NormalMapStrength)))
            property["usage"] = (int)PropertyUsageFlags.Storage;
    }

    public ScatterOptions ToOptions() => new()
    {
        Amount = Amount,
        Seed = Seed,
        Sampler = Sampler,
        TargetDirection = TargetDirection,
        NormalSource = NormalSource,
        Alignment = Alignment,
        AlignmentRandomness = AlignmentRandomness,
        MeshUpAxis = MeshUpAxis,
        SlopeMinDegrees = SlopeMinDegrees,
        SlopeMaxDegrees = SlopeMaxDegrees,
        SlopeReference = SlopeReference == SlopeReferenceMode.Custom ? SlopeCustomDirection : null,
        BaseScale = BaseScale,
        RandomScale = RandomScale,
        RandomYawDegrees = RandomYawDegrees,
        RandomTiltDegrees = RandomTiltDegrees,
        MinDistance = MinDistance,
        CandidateFactor = CandidateFactor,
    };
}
