using System.Linq;
using System.Collections.Generic;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// Places rooms one at a time at seeded random positions and sizes inside the bounds (brief
/// godoodungeon §4.3). A candidate is rejected when its footprint grown by the gap overlaps another
/// room or an occupied cell. A room that finds no place in its attempts is skipped and the next
/// rooms still try, since their random sizes may be smaller (decided at the M1 review); the layout
/// reports the shortfall.
/// </summary>
public static class RoomPlacer
{
    /// <summary>
    /// Places up to <see cref="LayoutSettings.RoomCount"/> rooms into <paramref name="layout"/>: the
    /// room boxes of <see cref="DungeonLayout.Boxes"/> first, where they are, then generated rooms
    /// for the rest of Room Count (brief §13.2), which keep the Gap from the boxes too.
    /// <paramref name="occupied"/> holds the hand-placed cells to avoid; null means none. It is not changed.
    /// </summary>
    public static void Place(DungeonLayout layout, LayoutSettings settings, long seed, OccupancyGrid occupied = null)
    {
        var random = new WfcRandom(seed, 0);
        var placed = layout.Boxes.Select(box => box.Rect).ToList();
        int generated = System.Math.Max(settings.RoomCount - layout.Boxes.Count, 0);
        if (layout.Boxes.Count > settings.RoomCount)
            layout.Warnings.Add($"There are {layout.Boxes.Count} room boxes but Room Count is {settings.RoomCount}: every box is used, and no room is generated.");
        for (int room = 0; room < generated; room++)
        {
            RoomRect? found = null;
            for (int attempt = 0; attempt < settings.AttemptsPerRoom && found == null; attempt++)
            {
                var candidate = Candidate(settings, random);
                if (candidate is { } rect && Fits(rect, settings, placed, occupied))
                    found = rect;
            }
            if (found != null)
                placed.Add(found.Value);
        }

        layout.Rooms.AddRange(placed);
        int wanted = layout.Boxes.Count + generated;
        if (placed.Count < wanted)
            layout.Warnings.Add($"Placed {placed.Count} of {wanted} rooms: {wanted - placed.Count} found no free place. "
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
