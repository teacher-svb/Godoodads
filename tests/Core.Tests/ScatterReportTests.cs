using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class ScatterReportTests
{
    private static ScatterResult Result(int placed, int requested, int attempts = 0, int spacing = 0,
        Dictionary<string, int> rejected = null, float slope = 1f) => new()
    {
        Transforms = new Transform3D[placed],
        Requested = requested,
        Attempts = attempts,
        RemovedBySpacing = spacing,
        Rejected = rejected ?? new(),
        SlopeAreaFraction = slope,
    };

    [Fact]
    public void Density_rejections_are_reported_as_tried_positions_not_instances()
    {
        // The board's case: 2048 placed, 4124 tries, 2076 rejected by a 50% grey mask.
        var lines = ScatterReport.Describe(Result(2048, 2048, 4124, rejected: new() { ["mask"] = 2076 }), 803429914, 0, 6);
        Assert.Equal("Placed 2048 / 2048. Seed 803429914. 6 ms.", lines[0].Text);
        Assert.Equal(ScatterReport.Kind.Success, lines[0].Kind);
        Assert.Equal("Mask rejected 2076 of 4124 tried positions (50%).", lines[1].Text);
        Assert.Equal(ScatterReport.Kind.Info, lines[1].Kind);
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void Shortfall_is_split_into_spacing_and_attempt_limit()
    {
        var lines = ScatterReport.Describe(Result(812, 1000, 20000, spacing: 68, rejected: new() { ["mask"] = 19000 }), 1, 0, 10);
        Assert.Equal(ScatterReport.Kind.Warning, lines[0].Kind);
        Assert.Contains(lines, l => l.Text.StartsWith("68 did not fit at this minimum distance"));
        Assert.Contains(lines, l => l.Text.StartsWith("120 missing: the attempt limit (20000 tried positions)"));
    }

    [Fact]
    public void Slope_and_append_details()
    {
        var lines = ScatterReport.Describe(Result(10, 10, 10, slope: 1f / 6), 3, 50, 1);
        Assert.Equal("Placed 10 / 10. Seed 3, appended after 50 existing. 1 ms.", lines[0].Text);
        Assert.Equal("Slope filter kept 17% of the surface.", lines.Last().Text);

        var none = ScatterReport.Describe(Result(0, 10, slope: 0f), 3, 0, 1);
        Assert.StartsWith("No part of the surface is within the slope range", none[1].Text);
        Assert.Equal(2, none.Count);
    }
}
