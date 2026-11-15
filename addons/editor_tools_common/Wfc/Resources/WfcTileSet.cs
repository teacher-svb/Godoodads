using Godot;
using Godot.Collections;

namespace Godoodads.Common.Wfc;

/// <summary>Socket rules for the items of a MeshLibrary. The library itself is never modified.</summary>
[Tool, GlobalClass]
public partial class WfcTileSet : Resource
{
    [Export] public MeshLibrary Library { get; set; }
    [Export] public Array<WfcTile> Tiles { get; set; } = new();

    /// <summary>
    /// Optional. Sockets that something outside the tiles supplies, such as a WFC Generator boundary
    /// socket or the 'outside-' of a godoodungeon room's bounds. The tile set check and preview count
    /// them as partners, so faces that only ever meet them are not flagged. Generation ignores them.
    /// </summary>
    [Export] public Array<string> ExternalSockets { get; set; } = new();
}
