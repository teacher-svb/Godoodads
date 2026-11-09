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

    [ExportGroup("Sockets")]
    [Export] public string SocketPosX { get; set; } = "";
    [Export] public string SocketNegX { get; set; } = "";
    [Export] public string SocketPosY { get; set; } = "";
    [Export] public string SocketNegY { get; set; } = "";
    [Export] public string SocketPosZ { get; set; } = "";
    [Export] public string SocketNegZ { get; set; } = "";

    public TileDefinition ToDefinition() =>
        new(new[] { SocketPosX, SocketNegX, SocketPosY, SocketNegY, SocketPosZ, SocketNegZ }, Rotation);

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() == PropertyName.Rotation)
            property["hint_string"] = "None:0,2-way:1,4-way:2";
    }
}
