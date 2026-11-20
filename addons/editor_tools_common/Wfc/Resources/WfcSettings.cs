using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Common.Wfc;

/// <summary>All options of one generation. Shown in the dialog and stored on the GridMap afterwards.</summary>
[Tool, GlobalClass]
public partial class WfcSettings : Resource
{
    /// <summary>Metadata key under which a GridMap stores the settings of its last generation.</summary>
    public const string MetaKey = "godoodads_wfc_settings";

    /// <summary>GridMap metadata with the cells the last generation wrote (a compressed <see cref="Core.GeneratedRecord"/>).</summary>
    public const string GeneratedKey = "godoodads_wfc_generated";

    [Export] public WfcTileSet TileSet { get; set; }

    private bool _randomiseSeed;

    /// <summary>Generate picks a new seed first. Off keeps the seed below, so Generate repeats its result.</summary>
    [Export]
    public bool RandomiseSeed
    {
        get => _randomiseSeed;
        set
        {
            bool changed = _randomiseSeed != value;
            _randomiseSeed = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }

    /// <summary>The seed of the next Generate, or of the last one while Randomise Seed is on.</summary>
    [Export] public long Seed { get; set; }

    /// <summary>Removes the cells in the bounds first. Otherwise only the previous generation's cells are replaced.</summary>
    [Export] public bool ClearBoundsFirst { get; set; }

    /// <summary>
    /// Clear Bounds First and the Clear Bounds button keep the cells you placed, and generated cells
    /// you edited since. Everything else generated goes. Off clears everything. Cells in pin boxes always stay.
    /// </summary>
    [Export] public bool KeepPlacedCells { get; set; }

    /// <summary>Raised by the Clear Bounds button. The WFC plugin clears the bounds by the rules above.</summary>
    [Signal] public delegate void ClearRequestedEventHandler();

    [ExportToolButton("Clear Bounds", Icon = "Clear")]
    public Callable ClearBoundsButton => Callable.From(RequestClear);

    private void RequestClear() => EmitSignal(SignalName.ClearRequested);

    [ExportGroup("Bounds")]
    [Export] public Vector3I BoundsOrigin { get; set; }
    [Export] public Vector3I BoundsSize { get; set; } = new(24, 8, 24);

    /// <summary>Raised by the Add Pin Box button. The godoowave plugin adds a WfcPinBox child to the GridMap (brief §16.1).</summary>
    [Signal] public delegate void AddPinBoxRequestedEventHandler();

    /// <summary>
    /// Adds a pin box to the GridMap: a child node whose cells Generate and Clear never change.
    /// Drag its handles, add more, or disable or delete one to let Generate replace its cells.
    /// </summary>
    [ExportGroup("Pin Boxes")]
    [ExportToolButton("Add Pin Box", Icon = "Pin")]
    public Callable AddPinBoxButton => Callable.From(RequestAddPinBox);

    private void RequestAddPinBox() => EmitSignal(SignalName.AddPinBoxRequested);

    /// <summary>The origin of a box as the viewport handles number them. Box 0 is the bounds, the only box the settings hold.</summary>
    public Vector3I BoxOrigin(int box) => BoundsOrigin;

    /// <summary>The size of a box. Box 0 is the bounds.</summary>
    public Vector3I BoxSize(int box) => BoundsSize;

    /// <summary>
    /// Sets the bounds (box 0). The do and undo target of a handle drag, so undo changes these
    /// settings and not the ones the dialog has open by then. Pin boxes are nodes since 1.5 (brief §16).
    /// </summary>
    public void SetBox(int box, Vector3I origin, Vector3I size)
    {
        BoundsOrigin = origin;
        BoundsSize = size;
        NotifyPropertyListChanged();
    }

    /// <summary>
    /// Sockets imagined just outside each face of the bounds, for example "rock" below.
    /// Empty leaves the face unconstrained.
    /// </summary>
    [ExportGroup("Boundary", "Boundary")]
    [Export] public string BoundaryPosX { get; set; } = "";
    [Export] public string BoundaryNegX { get; set; } = "";
    [Export] public string BoundaryPosY { get; set; } = "";
    [Export] public string BoundaryNegY { get; set; } = "";
    [Export] public string BoundaryPosZ { get; set; } = "";
    [Export] public string BoundaryNegZ { get; set; } = "";

    /// <summary>
    /// The socket name of path faces, such as "path". Placed cells with path faces are joined by
    /// routes before solving, and path tiles appear nowhere else. Empty turns paths off.
    /// </summary>
    [ExportGroup("Paths", "Path")]
    [Export] public string PathSocket { get; set; } = "";

    /// <summary>0 gives the shortest routes, 1 the most meandering. The seed decides the route.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float PathWander { get; set; } = 0.3f;

    public bool HasPaths => !string.IsNullOrWhiteSpace(PathSocket);

    /// <summary>Cells shown per frame while solving. 0 shows only the final result.</summary>
    [ExportGroup("Animation")]
    [Export(PropertyHint.Range, "0,20000,50,or_greater")] public int CellsPerFrame { get; set; } = 1000;

    [ExportGroup("Solver Limits")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")] public int MaxBacktracks { get; set; } = 2000;
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int MaxRestarts { get; set; } = 20;

    /// <summary>The settings stored on a GridMap, or null. An empty entry left by 1.1 reads as none.</summary>
    public static WfcSettings StoredOn(GridMap map) =>
        map.HasMeta(MetaKey) ? map.GetMeta(MetaKey).As<WfcSettings>() : null;

    /// <summary>Stores settings on a GridMap. Null removes the entry, so undo leaves nothing behind.</summary>
    public static void StoreOn(GridMap map, WfcSettings settings)
    {
        if (settings == null)
            map.RemoveMeta(MetaKey);
        else
            map.SetMeta(MetaKey, settings);
    }

    /// <summary>The boundary sockets per direction, null where empty. Invalid labels are added to errors.</summary>
    public Socket?[] ParseBoundary(List<string> errors)
    {
        var labels = new[] { BoundaryPosX, BoundaryNegX, BoundaryPosY, BoundaryNegY, BoundaryPosZ, BoundaryNegZ };
        var boundary = new Socket?[Directions.Count];
        for (int direction = 0; direction < Directions.Count; direction++)
        {
            string label = labels[direction]?.Trim();
            if (string.IsNullOrEmpty(label))
                continue;
            if (Socket.TryParse(label, Directions.IsVertical(direction), out var socket, out string error))
                boundary[direction] = socket;
            else
                errors.Add($"Boundary {Directions.Name(direction)}: {error}");
        }
        return boundary;
    }

    /// <summary>Sets a new random seed, different from the current one and from any in <paramref name="avoid"/>.</summary>
    public void PickNewSeed(ICollection<long> avoid = null)
    {
        long previous = Seed;
        long seed;
        do
            seed = GD.Randi();
        while (seed == previous || (avoid != null && avoid.Contains(seed)));
        Seed = seed;
        NotifyPropertyListChanged();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        string name = property["name"].AsString();
        var usage = (PropertyUsageFlags)property["usage"].AsInt64();
        if (name == PropertyName.Seed && RandomiseSeed)
            property["usage"] = (long)(usage | PropertyUsageFlags.ReadOnly);
    }
}
