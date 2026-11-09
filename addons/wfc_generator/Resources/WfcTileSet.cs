using Godot;
using Godot.Collections;

namespace Godoodads.WfcGenerator;

/// <summary>Socket rules for the items of a MeshLibrary. The library itself is never modified.</summary>
[Tool, GlobalClass]
public partial class WfcTileSet : Resource
{
    [Export] public MeshLibrary Library { get; set; }
    [Export] public Array<WfcTile> Tiles { get; set; } = new();
}
