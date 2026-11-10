using Godot;

namespace Godoodads.WfcGenerator;

/// <summary>All options of one generation. Shown in the dialog and stored on the GridMap afterwards.</summary>
[Tool, GlobalClass]
public partial class WfcSettings : Resource
{
    /// <summary>Metadata key under which a GridMap stores the settings of its last generation.</summary>
    public const string MetaKey = "godoodads_wfc_settings";

    /// <summary>GridMap metadata with the cells the last generation wrote (a compressed <see cref="Core.GeneratedRecord"/>).</summary>
    public const string GeneratedKey = "godoodads_wfc_generated";

    [Export] public WfcTileSet TileSet { get; set; }
    [Export] public long Seed { get; set; }
    [ExportToolButton("Randomise Seed", Icon = "RandomNumberGenerator")]
    public Callable RandomiseSeedButton => Callable.From(RandomiseSeed);

    /// <summary>Removes everything in the bounds first. Otherwise occupied cells stay fixed.</summary>
    [Export] public bool ClearBoundsFirst { get; set; }

    [ExportGroup("Bounds")]
    [Export] public Vector3I BoundsOrigin { get; set; }
    [Export] public Vector3I BoundsSize { get; set; } = new(24, 8, 24);

    /// <summary>Cells shown per frame while solving. 0 shows only the final result.</summary>
    [ExportGroup("Animation")]
    [Export(PropertyHint.Range, "0,20000,50,or_greater")] public int CellsPerFrame { get; set; } = 1000;

    [ExportGroup("Solver Limits")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")] public int MaxBacktracks { get; set; } = 2000;
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int MaxRestarts { get; set; } = 20;

    public void RandomiseSeed()
    {
        Seed = GD.Randi();
        NotifyPropertyListChanged();
    }
}
