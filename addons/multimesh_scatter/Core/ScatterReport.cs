using System.Collections.Generic;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// Turns a <see cref="ScatterResult"/> into status lines. Instance counts and attempt counts are
/// kept apart: filters reject tried positions, while a shortfall is counted in instances.
/// </summary>
public static class ScatterReport
{
    public enum Kind { Success, Warning, Info }

    public readonly record struct Line(string Text, Kind Kind);

    public static List<Line> Describe(ScatterResult r, int seed, int appendedTo, long milliseconds)
    {
        bool complete = r.Placed >= r.Requested;
        var lines = new List<Line>
        {
            new($"Placed {r.Placed} / {r.Requested}. Seed {seed}"
                + (appendedTo > 0 ? $", appended after {appendedTo} existing" : "")
                + $". {milliseconds} ms.", complete ? Kind.Success : Kind.Warning),
        };

        int missing = r.Requested - r.Placed;
        int missingByAttempts = missing - r.RemovedBySpacing;
        if (r.SlopeAreaFraction <= 0f)
            lines.Add(new("No part of the surface is within the slope range. Widen Slope Min/Max Degrees.", Kind.Warning));
        else
        {
            if (r.RemovedBySpacing > 0)
                lines.Add(new($"{r.RemovedBySpacing} did not fit at this minimum distance. Lower Min Distance or the amount.", Kind.Warning));
            if (missingByAttempts > 0)
                lines.Add(new($"{missingByAttempts} missing: the attempt limit ({r.Attempts} tried positions) was reached before enough positions passed the filters.", Kind.Warning));
        }

        foreach (var (filter, rejected) in r.Rejected)
            lines.Add(new($"{Capitalise(filter)} rejected {rejected} of {r.Attempts} tried positions ({Percent(rejected, r.Attempts)}).", Kind.Info));
        if (r.SlopeAreaFraction is > 0f and < 1f)
            lines.Add(new($"Slope filter kept {r.SlopeAreaFraction * 100f:0}% of the surface.", Kind.Info));
        return lines;
    }

    private static string Percent(int part, int whole) => whole > 0 ? $"{100.0 * part / whole:0}%" : "0%";

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
