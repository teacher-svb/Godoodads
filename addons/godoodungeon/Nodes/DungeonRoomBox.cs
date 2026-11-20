using Godot;
using Godot.Collections;
using Godoodads.Common;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon;

/// <summary>
/// A room box (brief godoodungeon §13): a child of the GridMap that fixes a room where you put it.
/// Generated boxes are solved with the room tile set; Built boxes keep the cells you built and get
/// corridors to the Door tiles on their wall ring. The box counts towards Room Count. No runtime
/// behaviour.
/// </summary>
[Tool, GlobalClass]
public partial class DungeonRoomBox : GridMapBox
{
    private RoomBoxMode _mode;

    /// <summary>Generated: the room is solved inside the box. Built: your cells stay and only corridors join them.</summary>
    [Export]
    public RoomBoxMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            UpdateGizmos();
        }
    }

    /// <summary>Copied to the room's Data in the dungeon graph on every Generate, such as <c>{"role": "boss"}</c>.</summary>
    [Export] public Dictionary Data { get; set; } = new();

    /// <summary>Generated boxes in the preview's room colour, Built ones in a warm stone colour.</summary>
    public override Color BoxColour => Mode == RoomBoxMode.Built ? new Color(1f, 0.6f, 0.25f) : new Color(0.3f, 0.8f, 1f);
}
