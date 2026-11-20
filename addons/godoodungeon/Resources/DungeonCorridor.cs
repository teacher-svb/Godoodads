using Godot;
using Godot.Collections;

namespace Godoodads.Dungeon;

/// <summary>
/// A corridor of a <see cref="DungeonGraph"/>: one room graph edge, with the corridor routed for it
/// (brief godoodungeon §12.1). Cells are GridMap cells. Corridors that meet share cells, but a
/// junction is not an edge of the graph.
/// </summary>
[Tool, GlobalClass]
public partial class DungeonCorridor : Resource
{
    /// <summary>The corridor's position in <see cref="DungeonGraph.Corridors"/>, from 0.</summary>
    [Export] public int Index { get; set; }

    /// <summary>The room at the start of <see cref="Cells"/>; always lower than <see cref="RoomB"/>.</summary>
    [Export] public int RoomA { get; set; }

    [Export] public int RoomB { get; set; }

    /// <summary>The wall cell of room A's door this corridor leaves through.</summary>
    [Export] public Vector3I DoorA { get; set; }

    /// <summary>The wall cell of room B's door this corridor arrives at.</summary>
    [Export] public Vector3I DoorB { get; set; }

    /// <summary>True for an edge of the minimum spanning tree; false for an Extra Connection (a loop).</summary>
    [Export] public bool IsTreeEdge { get; set; }

    /// <summary>The corridor cells in walking order, from outside door A to outside door B, on the bounds' bottom layer.</summary>
    [Export] public Array<Vector3I> Cells { get; set; } = new();

    /// <summary>Free data for gameplay, such as <c>{"locked": true}</c>. The plugin never reads it; Generate replaces the whole graph.</summary>
    [Export] public Dictionary Data { get; set; } = new();

    /// <summary>The number of corridor cells from door to door.</summary>
    public int Length => Cells.Count;

    /// <summary>The room at the other end from <paramref name="room"/>.</summary>
    public int Other(int room) => room == RoomA ? RoomB : RoomA;
}
