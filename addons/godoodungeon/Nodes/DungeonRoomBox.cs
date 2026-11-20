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

    /// <summary>
    /// Where Add Room Box puts a new box (brief §13.1): centred in the bounds at Max Room Size on X
    /// and Z (at most the bounds) and Room Height.
    /// </summary>
    public static (Vector3I Origin, Vector3I Size) CentredIn(Vector3I boundsOrigin, Vector3I boundsSize, Vector2I maxRoomSize, int roomHeight)
    {
        var size = new Vector3I(Mathf.Clamp(maxRoomSize.X, 1, Mathf.Max(boundsSize.X, 1)), Mathf.Clamp(roomHeight, 1, Mathf.Max(boundsSize.Y, 1)),
            Mathf.Clamp(maxRoomSize.Y, 1, Mathf.Max(boundsSize.Z, 1)));
        var origin = boundsOrigin + new Vector3I((boundsSize.X - size.X) / 2, 0, (boundsSize.Z - size.Z) / 2);
        return (origin, size);
    }

    /// <summary>
    /// Generated boxes pink, Built ones orange: the colours Preview Layout uses for them, apart from
    /// the cyan of generated rooms, the violet bounds, yellow doors and green corridors.
    /// </summary>
    public override Color BoxColour => Mode == RoomBoxMode.Built ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.45f, 0.8f);
}
