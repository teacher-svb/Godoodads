using System;
using System.Collections.Generic;
using System.Linq;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// How much of a solved grid each tile covers, next to the share its weight asks for (brief §13.2).
/// Tiles are grouped, so identical copies of a tile count as one.
/// </summary>
public sealed class TileShares
{
    /// <summary>A measured share is flagged when it and the weight share differ by more than this factor.</summary>
    public const double FlagFactor = 2;

    public const int Thirds = 3;

    /// <summary>Per group: its share of the summed tile weights.</summary>
    public double[] WeightShare { get; }

    /// <summary>Per group: its share of all cells.</summary>
    public double[] MeasuredShare { get; }

    /// <summary>Per group and third (bottom, middle, top): its share of the cells in that third. Null for one layer.</summary>
    public double[][] ThirdShare { get; }

    private TileShares(double[] weightShare, double[] measuredShare, double[][] thirdShare)
    {
        WeightShare = weightShare;
        MeasuredShare = measuredShare;
        ThirdShare = thirdShare;
    }

    /// <summary>The third of the grid height a layer belongs to: 0 bottom, 1 middle, 2 top.</summary>
    public static int ThirdOf(int layer, int layers) => Math.Min(Thirds - 1, layer * Thirds / Math.Max(1, layers));

    /// <param name="groupOfTile">Per tile index of <paramref name="set"/>, its group (for example all copies of one tile).</param>
    public static TileShares Measure(GridTopology grid, CompiledTileSet set, int[] cells, int[] groupOfTile)
    {
        int groups = groupOfTile.Length == 0 ? 0 : groupOfTile.Max() + 1;

        var weight = new double[groups];
        for (int variant = 0; variant < set.Variants.Count; variant++)
            weight[groupOfTile[set.Variants[variant].TileIndex]] += set.VariantWeights[variant];
        double totalWeight = weight.Sum();

        int layers = grid.Size.Y;
        var counts = new int[groups];
        var thirdCounts = new int[groups, Thirds];
        var thirdTotals = new int[Thirds];
        int total = 0;
        for (int cell = 0; cell < cells.Length; cell++)
        {
            if (cells[cell] < 0)
                continue;
            int group = groupOfTile[set.Variants[cells[cell]].TileIndex];
            int third = ThirdOf(grid.Coordinates(cell).Y, layers);
            counts[group]++;
            thirdCounts[group, third]++;
            thirdTotals[third]++;
            total++;
        }

        var weightShare = weight.Select(w => totalWeight > 0 ? w / totalWeight : 0).ToArray();
        var measured = counts.Select(c => total > 0 ? c / (double)total : 0).ToArray();
        double[][] thirds = null;
        if (layers >= Thirds)
        {
            thirds = new double[groups][];
            for (int group = 0; group < groups; group++)
            {
                thirds[group] = new double[Thirds];
                for (int third = 0; third < Thirds; third++)
                    thirds[group][third] = thirdTotals[third] > 0 ? thirdCounts[group, third] / (double)thirdTotals[third] : 0;
            }
        }
        return new TileShares(weightShare, measured, thirds);
    }

    /// <summary>True when the measured share is more than <see cref="FlagFactor"/> times off, either way.</summary>
    public bool IsFlagged(int group)
    {
        double weight = WeightShare[group], measured = MeasuredShare[group];
        if (weight <= 0 || measured <= 0)
            return weight > 0 != measured > 0;
        return measured > weight * FlagFactor || weight > measured * FlagFactor;
    }

    /// <summary>One group per distinct key, in order of first appearance.</summary>
    public static int[] GroupByKey(IReadOnlyList<string> keys)
    {
        var groups = new Dictionary<string, int>();
        var result = new int[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            if (!groups.TryGetValue(keys[i], out int group))
                groups[keys[i]] = group = groups.Count;
            result[i] = group;
        }
        return result;
    }
}
