using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// The numbers the layout stage works from (brief godoodungeon §4.3). Sizes are in cells on the
/// layout grid (X and the GridMap's Z), room sizes with their walls.
/// </summary>
public sealed record LayoutSettings
{
    /// <summary>The dungeon bounds seen from above.</summary>
    public Vector2I Bounds { get; init; } = new(48, 48);

    public int RoomCount { get; init; } = 12;
    public Vector2I MinRoomSize { get; init; } = new(5, 5);
    public Vector2I MaxRoomSize { get; init; } = new(9, 9);

    /// <summary>Room height in GridMap layers. Used by the solve stage, not by the layout.</summary>
    public int RoomHeight { get; init; } = 1;

    /// <summary>Cells around each room that corridors may not use, except the lane outside each door. 0 lets corridors run along walls.</summary>
    public int WallClearance { get; init; } = 1;

    /// <summary>Free cells between two rooms. At least 2 × <see cref="WallClearance"/> + 1, so a corridor fits between their clearance rings.</summary>
    public int Gap { get; init; } = 3;

    /// <summary>Free cells between a room and the edge of the bounds.</summary>
    public int Margin { get; init; } = 1;

    /// <summary>The share, 0 to 1, of the room graph's non-tree edges that are added back as loops.</summary>
    public double ExtraConnections { get; init; } = 0.15;

    public int MaxDoorsPerRoom { get; init; } = 4;

    /// <summary>Random positions tried per room before the layout gives up on it.</summary>
    public int AttemptsPerRoom { get; init; } = 50;

    /// <summary>The smallest gap that keeps a corridor between the clearance rings of two rooms.</summary>
    public int MinimumGap => 2 * WallClearance + 1;
}

/// <summary>
/// Seeds for each part of a dungeon generation, all derived from the Generate seed with
/// <see cref="WfcRandom.Derive"/>, so every part has its own stream and a retry never repeats one.
/// </summary>
public static class LayoutSeeds
{
    private const long LayoutStream = 1;
    private const long RoomStream = 2;
    private const long CorridorStream = 3;

    /// <summary>The seed of a layout attempt (room placement, graph and doors).</summary>
    public static long Layout(long seed, int attempt) => WfcRandom.Derive(WfcRandom.Derive(seed, LayoutStream), attempt);

    /// <summary>The seed of one room's solve in a layout attempt. The solver derives its own restarts from it.</summary>
    public static long Room(long seed, int attempt, int room) =>
        WfcRandom.Derive(WfcRandom.Derive(WfcRandom.Derive(seed, RoomStream), attempt), room);

    /// <summary>The seed of the corridor solve in a layout attempt.</summary>
    public static long Corridors(long seed, int attempt) => WfcRandom.Derive(WfcRandom.Derive(seed, CorridorStream), attempt);
}
