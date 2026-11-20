namespace Godoodads.Dungeon.Core;

/// <summary>How a room box makes its room (brief godoodungeon §13).</summary>
public enum RoomBoxMode
{
    /// <summary>The box fixes the room's position and size; the room is solved with the room tile set.</summary>
    Generated,

    /// <summary>The cells you built inside the box stay; the dungeon only connects corridors to its doors.</summary>
    Built,
}
