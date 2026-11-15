using System.Collections.Generic;
using Godot;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Checks layout settings before anything is placed (brief godoodungeon §4.8). Each message names
/// the setting as the dialog shows it, and says what to change.
/// </summary>
public static class LayoutValidator
{
    public const int MinimumRoomSide = 3;

    public static List<string> Validate(LayoutSettings settings)
    {
        var errors = new List<string>();
        if (settings.Bounds.X < 1 || settings.Bounds.Y < 1)
            errors.Add("The bounds are empty. Give them at least one cell on X and Z.");
        if (settings.RoomCount < 2)
            errors.Add($"Room Count is {settings.RoomCount}. A dungeon needs at least 2 rooms.");
        if (settings.RoomHeight < 1)
            errors.Add($"Room Height is {settings.RoomHeight}. Rooms need at least 1 layer.");

        var min = settings.MinRoomSize;
        var max = settings.MaxRoomSize;
        if (min.X < MinimumRoomSide || min.Y < MinimumRoomSide)
            errors.Add($"Min Room Size is {min.X} × {min.Y}. Rooms need at least {MinimumRoomSide} cells on X and Z, walls included, so a door fits between two corners.");
        if (min.X > max.X || min.Y > max.Y)
            errors.Add($"Min Room Size ({min.X} × {min.Y}) is larger than Max Room Size ({max.X} × {max.Y}) on {(min.X > max.X ? "X" : "Z")}.");

        if (settings.WallClearance < 0)
            errors.Add($"Wall Clearance is {settings.WallClearance}. Use 0 or more.");
        if (settings.Margin < 0)
            errors.Add($"Margin is {settings.Margin}. Use 0 or more.");
        if (settings.WallClearance >= 0 && settings.Gap < settings.MinimumGap)
            errors.Add($"Gap is {settings.Gap}, but Wall Clearance {settings.WallClearance} needs a gap of at least {settings.MinimumGap}: "
                + "the clearance on both sides of a corridor, the corridor itself, and room for a door's corridor piece. Raise Gap or lower Wall Clearance.");
        if (settings.ExtraConnections < 0 || settings.ExtraConnections > 1)
            errors.Add($"Extra Connections is {settings.ExtraConnections}. Use a share from 0 to 1.");
        if (settings.MaxDoorsPerRoom < 1)
            errors.Add($"Max Doors Per Room is {settings.MaxDoorsPerRoom}. Every room needs at least 1 door.");
        if (settings.AttemptsPerRoom < 1)
            errors.Add($"Attempts Per Room is {settings.AttemptsPerRoom}. Use at least 1.");

        // Only meaningful once the numbers above are sane.
        if (errors.Count == 0)
        {
            int needX = min.X + 2 * settings.Margin, needY = min.Y + 2 * settings.Margin;
            if (needX > settings.Bounds.X || needY > settings.Bounds.Y)
                errors.Add($"The smallest room ({min.X} × {min.Y}) with a margin of {settings.Margin} needs {needX} × {needY} cells, "
                    + $"but the bounds are {settings.Bounds.X} × {settings.Bounds.Y} on X and Z. Enlarge the bounds or lower Min Room Size or Margin.");
        }
        return errors;
    }
}

/// <summary>
/// The layout stage in one call (brief godoodungeon §4.3): validate the settings, place rooms, build
/// the room graph and place the doors, all from the seed of one layout attempt.
/// </summary>
public static class LayoutGenerator
{
    /// <param name="seed">The Generate seed. Each attempt derives its own stream with <see cref="LayoutSeeds.Layout"/>.</param>
    /// <param name="attempt">The layout attempt, from 0. A retry after a failed solve uses the next one.</param>
    /// <param name="origin">The GridMap cell of the bounds origin, for messages.</param>
    /// <param name="occupied">Hand-placed cells to avoid, on the layout grid. Null means none.</param>
    public static DungeonLayout Generate(LayoutSettings settings, long seed, int attempt = 0, Vector3I origin = default, OccupancyGrid occupied = null)
    {
        var layout = new DungeonLayout { Origin = origin };
        layout.Errors.AddRange(LayoutValidator.Validate(settings));
        if (!layout.IsValid)
            return layout;

        long layoutSeed = LayoutSeeds.Layout(seed, attempt);
        RoomPlacer.Place(layout, settings, layoutSeed, occupied);
        if (layout.Rooms.Count < 2)
        {
            layout.Errors.Add($"Only {layout.Rooms.Count} room(s) fit in the bounds; a dungeon needs at least 2. "
                + "Enlarge the bounds, or lower Min Room Size, Gap or Margin, or clear hand-placed cells from the bounds.");
            return layout;
        }
        RoomGraph.Build(layout, settings.ExtraConnections, layoutSeed);
        DoorPlacer.Place(layout, settings, occupied);
        return layout;
    }
}
