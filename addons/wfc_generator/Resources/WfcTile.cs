using Godot;
using Godot.Collections;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator;

/// <summary>One module and the sockets on its six faces.</summary>
[Tool, GlobalClass]
public partial class WfcTile : Resource
{
    public const int EmptyItem = -1;

    /// <summary>MeshLibrary item id, or -1 for an empty (air) tile.</summary>
    [Export(PropertyHint.Range, "-1,4096,1,or_greater")] public int Item { get; set; } = EmptyItem;

    [Export] public TileRotation Rotation { get; set; } = TileRotation.None;

    /// <summary>
    /// How often the solver chooses this tile, relative to the others. Shared by its rotations.
    /// 0 means never generated, but the tile can still be placed by hand.
    /// </summary>
    [Export(PropertyHint.Range, "0,100,0.01,or_greater")] public float Weight { get; set; } = 1f;

    /// <summary>
    /// Optional. Multiplies the weight by height: the curve's start is the bottom layer of the
    /// bounds and its end the top layer. 0 at a height means never generated there.
    /// </summary>
    [Export] public Curve HeightCurve { get; set; }

    /// <summary>
    /// Optional scene placed at every cell this tile is generated in, such as a light or a spawn
    /// point. It may be set on an empty tile too.
    /// </summary>
    [Export] public PackedScene Marker { get; set; }

    private const int HeightSamples = 64;

    [ExportGroup("Sockets")]
    [Export] public string SocketPosX { get; set; } = "";
    [Export] public string SocketNegX { get; set; } = "";
    [Export] public string SocketPosY { get; set; } = "";
    [Export] public string SocketNegY { get; set; } = "";
    [Export] public string SocketPosZ { get; set; } = "";
    [Export] public string SocketNegZ { get; set; } = "";

    public TileDefinition ToDefinition() =>
        new(new[] { SocketPosX, SocketNegX, SocketPosY, SocketNegY, SocketPosZ, SocketNegZ }, Rotation, Weight, SampleHeightCurve());

    /// <summary>The height curve as evenly spaced samples over its domain, or null without a curve.</summary>
    public double[] SampleHeightCurve()
    {
        if (HeightCurve == null)
            return null;
        var samples = new double[HeightSamples];
        float from = HeightCurve.MinDomain, to = HeightCurve.MaxDomain;
        for (int i = 0; i < HeightSamples; i++)
            samples[i] = HeightCurve.Sample(from + (to - from) * i / (HeightSamples - 1f));
        return samples;
    }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() == PropertyName.Rotation)
            property["hint_string"] = "None:0,Half Turn (2 rotations):1,Quarter Turns (4 rotations):2";
    }
}
