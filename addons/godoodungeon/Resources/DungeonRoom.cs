using Godot;
using Godot.Collections;

namespace Godoodads.Dungeon;

/// <summary>
/// A room of a <see cref="DungeonGraph"/> (brief godoodungeon §12.2). Cells are GridMap cells.
/// Generate fills everything but <see cref="Data"/>, which is for gameplay code.
/// </summary>
[Tool, GlobalClass]
public partial class DungeonRoom : Resource
{
    /// <summary>The room's position in <see cref="DungeonGraph.Rooms"/>, from 0.</summary>
    [Export] public int Index { get; set; }

    /// <summary>The GridMap cell of the room's lowest corner, walls included.</summary>
    [Export] public Vector3I Position { get; set; }

    /// <summary>The room's extent in cells, walls included. Y is the room height in layers.</summary>
    [Export] public Vector3I Size { get; set; }

    /// <summary>The wall cells holding the room's doors, on its bottom layer.</summary>
    [Export] public Array<Vector3I> DoorCells { get; set; } = new();

    /// <summary>Per door (same index as <see cref="DoorCells"/>), one step out of the room through it.</summary>
    [Export] public Array<Vector3I> DoorDirections { get; set; } = new();

    /// <summary>The corridors that start or end at this room, as indices in <see cref="DungeonGraph.Corridors"/>.</summary>
    [Export] public int[] Corridors { get; set; } = System.Array.Empty<int>();

    /// <summary>Free data for gameplay, such as <c>{"role": "boss"}</c>. The plugin never reads it; Generate replaces the whole graph.</summary>
    [Export] public Dictionary Data { get; set; } = new();

    /// <summary>The number status messages and Preview Layout show: <see cref="Index"/> + 1.</summary>
    public int Number => Index + 1;

    /// <summary>The cell nearest the room's centre on its bottom layer (rounded down on even sizes).</summary>
    public Vector3I CentreCell => new(Position.X + Size.X / 2, Position.Y, Position.Z + Size.Z / 2);

    /// <summary>True when the cell lies in the room's footprint on X and Z, walls included. The layer is ignored.</summary>
    public bool HasCell(Vector3I cell) =>
        cell.X >= Position.X && cell.X < Position.X + Size.X && cell.Z >= Position.Z && cell.Z < Position.Z + Size.Z;
}
