using System.Collections.Generic;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Places rooms one at a time at seeded random positions and sizes inside the bounds (brief
/// godoodungeon §4.3). A candidate is rejected when its footprint grown by the gap overlaps another
/// room or an occupied cell. When a room finds no place in its attempts, placement stops there and
/// the layout reports the shortfall.
/// </summary>
public static class RoomPlacer
{
    /// <summary>
    /// Places up to <see cref="LayoutSettings.RoomCount"/> rooms into <paramref name="layout"/>.
    /// <paramref name="occupied"/> holds the hand-placed cells to avoid; null means none. It is not changed.
    /// </summary>
    public static void Place(DungeonLayout layout, LayoutSettings settings, long seed, OccupancyGrid occupied = null)
    {
        var random = new WfcRandom(seed, 0);
        var placed = new List<RoomRect>();
        for (int room = 0; room < settings.RoomCount; room++)
        {
            RoomRect? found = null;
            for (int attempt = 0; attempt < settings.AttemptsPerRoom && found == null; attempt++)
            {
                var candidate = Candidate(settings, random);
                if (candidate is { } rect && Fits(rect, settings, placed, occupied))
                    found = rect;
            }
            if (found == null)
                break;
            placed.Add(found.Value);
        }

        layout.Rooms.AddRange(placed);
        if (placed.Count < settings.RoomCount)
            layout.Warnings.Add($"Placed {placed.Count} of {settings.RoomCount} rooms: no free place was found for the next one. "
                + "Enlarge the bounds, or lower the room count, the room sizes or the gap.");
    }

    /// <summary>A random size in range and a random position that keeps the margin, or null when that size cannot fit at all.</summary>
    private static RoomRect? Candidate(LayoutSettings settings, WfcRandom random)
    {
        var size = new Vector2I(Between(random, settings.MinRoomSize.X, settings.MaxRoomSize.X),
            Between(random, settings.MinRoomSize.Y, settings.MaxRoomSize.Y));
        int maxX = settings.Bounds.X - settings.Margin - size.X;
        int maxY = settings.Bounds.Y - settings.Margin - size.Y;
        if (maxX < settings.Margin || maxY < settings.Margin)
            return null;
        var position = new Vector2I(Between(random, settings.Margin, maxX), Between(random, settings.Margin, maxY));
        return new RoomRect(position, size);
    }

    private static bool Fits(RoomRect rect, LayoutSettings settings, List<RoomRect> placed, OccupancyGrid occupied)
    {
        foreach (var other in placed)
            if (rect.Overlaps(other, settings.Gap))
                return false;
        if (occupied != null)
            foreach (var cell in rect.Grow(settings.Gap).Cells())
                if (occupied.InBounds(cell) && occupied.IsTaken(cell))
                    return false;
        return true;
    }

    /// <summary>A whole number from <paramref name="min"/> to <paramref name="max"/>, both included.</summary>
    private static int Between(WfcRandom random, int min, int max) =>
        max <= min ? min : min + random.Next(max - min + 1);
}
