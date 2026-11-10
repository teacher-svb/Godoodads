using System.Collections.Generic;
using Godot;
using Godoodads.WfcGenerator.Core;

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

    /// <summary>Raised by the pin buttons. The WFC plugin pins or unpins the cells in the pin box.</summary>
    [Signal] public delegate void PinRequestedEventHandler(bool pin);

    /// <summary>
    /// Generated cells inside this box can be pinned, so the next Generate keeps them.
    /// A size of 0 hides the box.
    /// </summary>
    [ExportGroup("Pin Box", "Pin")]
    [Export] public Vector3I PinOrigin { get; set; }
    [Export] public Vector3I PinSize { get; set; }
    [ExportToolButton("Place Around Bounds", Icon = "ToolScale")]
    public Callable PinPlaceButton => Callable.From(PlacePinBox);
    [ExportToolButton("Pin Generated Cells", Icon = "Pin")]
    public Callable PinCellsButton => Callable.From(RequestPin);
    [ExportToolButton("Unpin", Icon = "Unlinked")]
    public Callable PinUnpinButton => Callable.From(RequestUnpin);

    public bool HasPinBox => PinSize.X > 0 && PinSize.Y > 0 && PinSize.Z > 0;

    public void PlacePinBox()
    {
        PinOrigin = BoundsOrigin;
        PinSize = BoundsSize;
        NotifyPropertyListChanged();
    }

    private void RequestPin() => EmitSignal(SignalName.PinRequested, true);

    private void RequestUnpin() => EmitSignal(SignalName.PinRequested, false);

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

    public void RandomiseSeed()
    {
        Seed = GD.Randi();
        NotifyPropertyListChanged();
    }
}
