using System;
using System.Collections.Generic;
using Godot;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Adapters;

/// <summary>
/// The solver's starting state, read from the cells already in a GridMap. Replaced counts cells a
/// previous generation wrote, which are free again; Edited counts those changed since and Pinned
/// those pinned, which are both kept (and included in Kept).
/// </summary>
public sealed record FixedCells(int[] Initial, int Kept, int Ignored, int Replaced = 0, int Edited = 0, int Pinned = 0);

/// <summary>Cell changes as flat (x, y, z, item, orientation) records, so undo stores plain ints.</summary>
public sealed record CellChanges(int[] Do, int[] Undo)
{
    public const int Stride = 5;
}

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
    /// </summary>
    public static FixedCells ReadFixed(GridMap map, Vector3I origin, GridTopology grid, TileSetBinding binding, GeneratedRecord previous = null)
    {
        var result = AllFree(grid);
        int kept = 0, ignored = 0, replaced = 0, edited = 0, pinned = 0;
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var position = origin + grid.Coordinates(cell);
            int item = map.GetCellItem(position);
            if (item == GridMap.InvalidCellItem)
                continue;
            if (previous != null)
            {
                bool matches = previous.Matches(position, item, map.GetCellItemOrientation(position));
                if (matches && !previous.IsPinned(position))
                {
                    replaced++;
                    continue;
                }
                if (matches)
                    pinned++;
                else if (previous.Contains(position))
                    edited++;
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
        return result with { Kept = kept, Ignored = ignored, Replaced = replaced, Edited = edited, Pinned = pinned };
    }

    /// <summary>
    /// The record after this generation: the previous one outside the bounds, plus the non-empty
    /// cells written to free cells inside them.
    /// </summary>
    public static GeneratedRecord RecordAfter(GeneratedRecord previous, Vector3I origin, GridTopology grid, TileSetBinding binding, int[] initial, int[] solved)
    {
        var record = new GeneratedRecord();
        if (previous != null)
            foreach (var (position, written) in previous.Cells)
                record.Set(position, written.Item, written.Orientation, written.Pinned);
        record.RemoveInside(origin, grid.Size, keepPinned: true);

        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (initial[cell] != WfcSolver.Free)
                continue;
            var variant = binding.Compiled.Variants[solved[cell]];
            int item = binding.Items[variant.TileIndex];
            if (item != WfcTile.EmptyItem)
                record.Set(origin + grid.Coordinates(cell), item, GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
        }
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

    /// <summary>
    /// Pins the recorded cells inside a box that still hold what was generated, or unpins the pinned
    /// ones. Returns the new record and how many cells changed.
    /// </summary>
    public static (GeneratedRecord Record, int Changed) Pin(GridMap map, Vector3I origin, Vector3I size, bool pin)
    {
        var record = ReadRecord(map);
        int changed = record.SetPinnedInside(origin, size, pin,
            (position, written) => map.GetCellItem(position) == written.Item && map.GetCellItemOrientation(position) == written.Orientation);
        return (record, changed);
    }

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
