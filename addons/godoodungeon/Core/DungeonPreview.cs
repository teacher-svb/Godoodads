using System;
using System.Collections.Generic;

namespace Godoodads.Dungeon.Core;

/// <summary>What Preview Layout shows: the layout and corridors Generate would use, or why there are none.</summary>
public sealed record PreviewResult(DungeonLayout Layout, CorridorPlan Corridors, int LayoutAttempt, IReadOnlyList<AttemptFailure> Failures, IReadOnlyList<string> Errors)
{
    public bool IsValid => Layout != null && Corridors != null && Errors.Count == 0;
}

/// <summary>
/// The layout and corridors of a dungeon without the WFC solves (brief godoodungeon §5, Preview
/// Layout): the same layout attempts, seeds and router as <see cref="DungeonJob"/>, so the preview
/// matches what Generate writes unless a room or the corridors then fail to solve in that attempt.
/// Fast enough to run on the editor's main thread while settings change.
/// </summary>
public static class DungeonPreview
{
    public static PreviewResult Plan(DungeonRequest request)
    {
        var errors = LayoutValidator.Validate(request.Layout, request.RoomBoxes.Count);
        if (errors.Count > 0)
            return new PreviewResult(null, null, 0, Array.Empty<AttemptFailure>(), errors);

        var occupied = DungeonJob.OccupiedGrid(request);
        var failures = new List<AttemptFailure>();
        DungeonLayout layout = null;
        for (int attempt = 0; attempt < Math.Max(1, request.LayoutAttempts); attempt++)
        {
            layout = LayoutGenerator.Generate(request.Layout, request.Seed, attempt, request.Origin, occupied, request.RoomBoxes);
            if (!layout.IsValid)
            {
                failures.Add(new AttemptFailure(attempt, string.Join(" ", layout.Errors)));
                continue;
            }
            var plan = CorridorRouter.Route(layout, request.Layout, occupied, request.PathWander, LayoutSeeds.Corridors(request.Seed, attempt));
            if (plan.IsValid)
                return new PreviewResult(layout, plan, attempt, failures, Array.Empty<string>());
            failures.Add(new AttemptFailure(attempt, $"Corridors: {plan.Error}", plan.ErrorCell is { } at ? layout.GridCell(at) : null));
        }
        return new PreviewResult(layout, null, 0, failures, Array.Empty<string>());
    }
}
