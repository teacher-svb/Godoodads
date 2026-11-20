#if TOOLS
using Godot;
using Godoodads.Common;
using Godoodads.Common.Editor;

namespace Godoodads.Dungeon.Editor;

/// <summary>The *godoodungeon room boxes* entry of the viewport's gizmo menu: outlines and face handles of <see cref="DungeonRoomBox"/> nodes.</summary>
[Tool]
public partial class RoomBoxGizmoPlugin : GridMapBoxGizmoPlugin
{
    public override string _GetGizmoName() => "godoodungeon room boxes";

    protected override bool Handles(GridMapBox box) => box is DungeonRoomBox;
}
#endif
