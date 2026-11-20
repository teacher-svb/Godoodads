using System;
using System.Collections.Generic;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Common.Wfc.Adapters;

/// <summary>
/// The solver's starting state, read from the cells already in a GridMap. Replaced counts cells a
/// previous generation wrote, which are free again; Edited counts those changed since, which are
/// kept (and included in Kept). PinBoxCells counts the bounds cells inside pin boxes (WFC brief
/// §16.2), empty ones included; their occupied cells are also in Kept or Ignored.
/// </summary>
public sealed record FixedCells(int[] Initial, int Kept, int Ignored, int Replaced = 0, int Edited = 0, int PinBoxCells = 0);

/// <summary>Cell changes as flat (x, y, z, item, orientation) records, so undo stores plain ints.</summary>
public sealed record CellChanges(int[] Do, int[] Undo)
{
    public const int Stride = 5;
}

/// <summary>
/// What the Clear Bounds button removes. Initial marks the cells that stay as Excluded and every
/// other cell as Free, in the same form as a solve's starting state (for markers). Kept counts the
/// occupied cells kept because you placed them (Keep Placed Cells); PinBoxCells counts the bounds
/// cells inside pin boxes, which always stay.
/// </summary>
public sealed record ClearedCells(CellChanges Changes, int[] Initial, int Cleared, int Kept, int PinBoxCells = 0);

/// <summary>Reads and writes GridMap cells for a solve inside bounds starting at an origin.</summary>
public static class GridMapCells
{
    /// <summary>All cells free, for a solve that replaces everything in the bounds.</summary>
    public static FixedCells AllFree(GridTopology grid)
    {
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);
        return new FixedCells(initial, 0, 0);
    }

    /// <summary>
    /// Occupied cells become fixed, except cells that still hold what the previous generation wrote
    /// (<paramref name="previous"/>), which are free again. Cells whose item or orientation has no
    /// variant are excluded, which leaves them in place and places no constraint on their neighbours.
    /// Without <paramref name="keepPlaced"/> (Clear Bounds First, Keep Placed Cells off) every
    /// occupied cell is free. Cells inside <paramref name="pinBoxes"/> are never free (brief §16.2):
    /// occupied ones are fixed or excluded as above, and empty ones are excluded, so they stay empty.
    /// </summary>
    public static FixedCells ReadFixed(GridMap map, Vector3I origin, GridTopology grid, TileSetBinding binding, GeneratedRecord previous = null,
        IReadOnlyList<CellBox> pinBoxes = null, bool keepPlaced = true)
    {
        var result = AllFree(grid);
        int kept = 0, ignored = 0, replaced = 0, edited = 0, inBoxes = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var position = origin + grid.Coordinates(cell);
            bool pinned = CellBox.AnyContains(pinBoxes, position);
            if (pinned)
                inBoxes++;
            int item = map.GetCellItem(position);
            if (item == GridMap.InvalidCellItem)
            {
                if (pinned)
                    result.Initial[cell] = WfcSolver.Excluded;
                continue;
            }
            if (!pinned)
            {
                if (!keepPlaced)
                {
                    replaced++;
                    continue;
                }
                if (previous != null)
                {
                    if (previous.Matches(position, item, map.GetCellItemOrientation(position)))
                    {
                        replaced++;
                        continue;
                    }
                    if (previous.Contains(position))
                        edited++;
                }
            }

            int tile = binding.TileForItem(item);
            int turns = GridMapOrientation.ToQuarterTurns(map.GetCellItemOrientation(position));
            int variant = tile >= 0 && turns >= 0 ? binding.Compiled.FindVariant(tile, turns) : -1;
            result.Initial[cell] = variant >= 0 ? variant : WfcSolver.Excluded;
            if (variant >= 0)
                kept++;
            else
                ignored++;
        }
        return result with { Kept = kept, Ignored = ignored, Replaced = replaced, Edited = edited, PinBoxCells = inBoxes };
    }

    /// <summary>
    /// The record after this generation: the previous one outside the bounds and inside pin boxes
    /// (generated cells there stay generated, §16.2), plus the non-empty cells written to free cells.
    /// Other entries inside the bounds go, so a cell you edited and kept counts as yours from now on.
    /// </summary>
    public static GeneratedRecord RecordAfter(GeneratedRecord previous, Vector3I origin, GridTopology grid, TileSetBinding binding, int[] initial, int[] solved,
        IReadOnlyList<CellBox> pinBoxes = null)
    {
        var record = new GeneratedRecord();
        if (previous != null)
            foreach (var (position, written) in previous.Cells)
                record.Set(position, written.Item, written.Orientation);
        record.RemoveInside(origin, grid.Size, pinBoxes);

        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (initial[cell] != WfcSolver.Free)
                continue;
            var position = origin + grid.Coordinates(cell);
            record.Remove(position);
            var variant = binding.Compiled.Variants[solved[cell]];
            int item = binding.Items[variant.TileIndex];
            if (item != WfcTile.EmptyItem)
                record.Set(position, item, GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
        }
        return record;
    }

    /// <summary>
    /// The cells the Clear Bounds button removes, by the rules of Clear Bounds First: every occupied
    /// cell, or with <paramref name="keepPlaced"/> only the cells that still hold what a generation
    /// wrote, so hand-placed and edited cells stay. Cells inside <paramref name="pinBoxes"/> always
    /// stay (§16.2). Needs no tile set.
    /// </summary>
    public static ClearedCells Clear(GridMap map, Vector3I origin, GridTopology grid, GeneratedRecord record, bool keepPlaced,
        IReadOnlyList<CellBox> pinBoxes = null)
    {
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);
        var apply = new List<int>();
        var revert = new List<int>();
        int cleared = 0, kept = 0, inBoxes = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var position = origin + grid.Coordinates(cell);
            if (CellBox.AnyContains(pinBoxes, position))
            {
                initial[cell] = WfcSolver.Excluded;
                inBoxes++;
                continue;
            }
            int item = map.GetCellItem(position);
            if (item == GridMap.InvalidCellItem)
                continue;
            int orientation = map.GetCellItemOrientation(position);
            bool generated = record != null && record.Matches(position, item, orientation);
            if (keepPlaced && !generated)
            {
                initial[cell] = WfcSolver.Excluded;
                kept++;
                continue;
            }
            Append(apply, position, (int)GridMap.InvalidCellItem, 0);
            Append(revert, position, item, Math.Max(orientation, 0));
            cleared++;
        }
        return new ClearedCells(new CellChanges(apply.ToArray(), revert.ToArray()), initial, cleared, kept, inBoxes);
    }

    /// <summary>The record after a Clear: no entries inside the bounds except inside pin boxes, and the rest unchanged.</summary>
    public static GeneratedRecord RecordAfterClear(GeneratedRecord previous, Vector3I origin, Vector3I size, IReadOnlyList<CellBox> pinBoxes = null)
    {
        var record = new GeneratedRecord();
        if (previous != null)
            foreach (var (position, written) in previous.Cells)
                record.Set(position, written.Item, written.Orientation);
        record.RemoveInside(origin, size, pinBoxes);
        return record;
    }

    /// <summary>Stores an encoded record. Null or empty data removes it, so undo leaves no empty entry.</summary>
    public static void WriteRecord(GridMap map, byte[] data)
    {
        if (data == null || data.Length == 0)
            map.RemoveMeta(WfcSettings.GeneratedKey);
        else
            map.SetMeta(WfcSettings.GeneratedKey, data);
    }

    /// <summary>The encoded record stored on a GridMap, or an empty array.</summary>
    public static byte[] StoredRecord(GridMap map) =>
        map.HasMeta(WfcSettings.GeneratedKey) ? map.GetMeta(WfcSettings.GeneratedKey).AsByteArray() ?? System.Array.Empty<byte>() : System.Array.Empty<byte>();

    /// <summary>The record stored on a GridMap, or an empty one.</summary>
    public static GeneratedRecord ReadRecord(GridMap map) =>
        GeneratedRecord.Decode(StoredRecord(map));

    /// <summary>The changes that write a solution to free cells. Cells already correct are skipped.</summary>
    public static CellChanges Diff(GridMap map, Vector3I origin, GridTopology grid, TileSetBinding binding, int[] initial, int[] solved)
    {
        var apply = new List<int>();
        var revert = new List<int>();
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (initial[cell] != WfcSolver.Free)
                continue;

            var variant = binding.Compiled.Variants[solved[cell]];
            int item = binding.Items[variant.TileIndex];
            int orientation = item == WfcTile.EmptyItem ? 0 : GridMapOrientation.FromQuarterTurns(variant.QuarterTurns);

            var position = origin + grid.Coordinates(cell);
            int oldItem = map.GetCellItem(position);
            int oldOrientation = map.GetCellItemOrientation(position);
            bool bothEmpty = item == GridMap.InvalidCellItem && oldItem == GridMap.InvalidCellItem;
            if (bothEmpty || (item == oldItem && orientation == oldOrientation))
                continue;

            Append(apply, position, item, orientation);
            Append(revert, position, oldItem, Math.Max(oldOrientation, 0));
        }
        return new CellChanges(apply.ToArray(), revert.ToArray());
    }

    public static void Apply(GridMap map, int[] changes)
    {
        for (int i = 0; i < changes.Length; i += CellChanges.Stride)
            map.SetCellItem(new Vector3I(changes[i], changes[i + 1], changes[i + 2]), changes[i + 3], changes[i + 4]);
    }

    private static void Append(List<int> list, Vector3I position, int item, int orientation)
    {
        list.Add(position.X);
        list.Add(position.Y);
        list.Add(position.Z);
        list.Add(item);
        list.Add(orientation);
    }
}
