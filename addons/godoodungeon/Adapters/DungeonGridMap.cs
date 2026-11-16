using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon.Adapters;

/// <summary>
/// Reads and writes a dungeon on a GridMap (brief godoodungeon §4.6, §4.7). The cells a dungeon wrote
/// are kept as a generated record in the GridMap's metadata, under a key separate from WFC
/// Generator's. Occupied cells in the bounds that are not unchanged record cells are hand-placed:
/// rooms and corridors avoid them and they are never written. Undo is up to the caller: every write
/// comes as <see cref="CellChanges"/> with the cells to restore.
/// </summary>
public static class DungeonGridMap
{
    /// <summary>GridMap metadata with the cells the last dungeon wrote (a compressed <see cref="GeneratedRecord"/>).</summary>
    public const string RecordKey = "godoodads_dungeon_generated";

    public const string MarkerContainerName = "DungeonMarkers";
    public const string MarkerTag = "godoodads_dungeon_marker";

    public static byte[] StoredRecord(GridMap map) => map.HasMeta(RecordKey) ? map.GetMeta(RecordKey).AsByteArray() : Array.Empty<byte>();

    public static GeneratedRecord ReadRecord(GridMap map) => GeneratedRecord.Decode(StoredRecord(map));

    /// <summary>Stores a record. Empty data removes the entry, so undo leaves nothing behind.</summary>
    public static void WriteRecord(GridMap map, byte[] data)
    {
        if (data == null || data.Length == 0)
            map.RemoveMeta(RecordKey);
        else
            map.SetMeta(RecordKey, data);
    }

    /// <summary>
    /// A cheap value that changes when any cell of the map or the stored record changes, so Preview
    /// Layout can tell when the hand-placed cells may have changed. Independent of cell order.
    /// Only compared within one editor session: it uses <see cref="HashCode"/>.
    /// </summary>
    public static long Fingerprint(GridMap map)
    {
        long cells = 0;
        var used = map.GetUsedCells();
        foreach (Vector3I cell in used)
            cells += HashCode.Combine(cell, map.GetCellItem(cell), map.GetCellItemOrientation(cell));
        var hash = new HashCode();
        hash.Add(cells);
        hash.Add(used.Count);
        hash.AddBytes(StoredRecord(map));
        return hash.ToHashCode();
    }

    /// <summary>Occupied cells in the box that the last dungeon did not write, or that changed since.</summary>
    public static HashSet<Vector3I> ReadHandPlaced(GridMap map, Vector3I origin, Vector3I size, GeneratedRecord record)
    {
        var handPlaced = new HashSet<Vector3I>();
        foreach (Vector3I cell in map.GetUsedCells())
        {
            var local = cell - origin;
            if (local.X < 0 || local.Y < 0 || local.Z < 0 || local.X >= size.X || local.Y >= size.Y || local.Z >= size.Z)
                continue;
            if (!record.Matches(cell, map.GetCellItem(cell), map.GetCellItemOrientation(cell)))
                handPlaced.Add(cell);
        }
        return handPlaced;
    }

    /// <summary>The job request for these settings, checked tile sets and hand-placed cells.</summary>
    public static DungeonRequest Request(DungeonSettings settings, DungeonTileSets tileSets, IReadOnlySet<Vector3I> handPlaced) => new()
    {
        Layout = settings.ToLayoutSettings(),
        Origin = settings.BoundsOrigin,
        RoomTiles = tileSets.Room.Compiled,
        DoorTile = tileSets.DoorTile,
        EdgeSocket = settings.RoomEdgeSocket,
        CorridorTiles = tileSets.Corridor.Compiled,
        PathSocket = settings.PathSocket,
        PathWander = settings.PathWander,
        Seed = settings.Seed,
        LayoutAttempts = settings.LayoutAttempts,
        MaxBacktracks = settings.MaxBacktracks,
        MaxRestarts = settings.MaxRestarts,
        HandPlaced = handPlaced,
    };

    /// <summary>
    /// The cells a solved dungeon writes: every non-excluded cell of every solve, as a MeshLibrary
    /// item and orientation (the empty item clears the cell). Rooms come first; the corridor solve
    /// excludes them, so no cell is written twice.
    /// </summary>
    public static Dictionary<Vector3I, (int Item, int Orientation)> Cells(DungeonResult result, TileSetBinding room, TileSetBinding corridor)
    {
        var cells = new Dictionary<Vector3I, (int, int)>();
        foreach (var solve in result.Solves)
        {
            var binding = solve.IsCorridor ? corridor : room;
            for (int cell = 0; cell < solve.Input.Grid.CellCount; cell++)
            {
                if (solve.Input.Initial[cell] == WfcSolver.Excluded || solve.Cells[cell] < 0)
                    continue;
                var variant = binding.Compiled.Variants[solve.Cells[cell]];
                int item = binding.Items[variant.TileIndex];
                int orientation = item == WfcTile.EmptyItem ? 0 : GridMapOrientation.FromQuarterTurns(variant.QuarterTurns);
                cells[solve.Input.GridMapCell(cell)] = (item, orientation);
            }
        }
        return cells;
    }

    /// <summary>
    /// The changes that replace the previous dungeon with a new one: the previous record's cells that
    /// are still unchanged are cleared, then the new cells are written. Cells already right are
    /// skipped. Also the new record (every non-empty cell written).
    /// </summary>
    public static (CellChanges Changes, GeneratedRecord Record) Write(GridMap map, GeneratedRecord previous,
        IReadOnlyDictionary<Vector3I, (int Item, int Orientation)> cells)
    {
        var target = new Dictionary<Vector3I, (int Item, int Orientation)>();
        foreach (var (position, written) in previous.Cells)
            if (previous.Matches(position, map.GetCellItem(position), map.GetCellItemOrientation(position)))
                target[position] = ((int)GridMap.InvalidCellItem, 0);
        foreach (var (position, value) in cells)
            target[position] = value;

        var record = new GeneratedRecord();
        foreach (var (position, value) in cells)
            if (value.Item != WfcTile.EmptyItem)
                record.Set(position, value.Item, value.Orientation);
        return (Diff(map, target), record);
    }

    /// <summary>The changes of Clear Dungeon: the record's cells that are still unchanged are cleared; edited cells stay.</summary>
    public static CellChanges Clear(GridMap map, GeneratedRecord record)
    {
        var target = new Dictionary<Vector3I, (int Item, int Orientation)>();
        foreach (var (position, written) in record.Cells)
            if (record.Matches(position, map.GetCellItem(position), map.GetCellItemOrientation(position)))
                target[position] = ((int)GridMap.InvalidCellItem, 0);
        return Diff(map, target);
    }

    private static CellChanges Diff(GridMap map, Dictionary<Vector3I, (int Item, int Orientation)> target)
    {
        var apply = new List<int>();
        var revert = new List<int>();
        foreach (var (position, (item, orientation)) in target.OrderBy(t => t.Key.Y).ThenBy(t => t.Key.Z).ThenBy(t => t.Key.X))
        {
            int oldItem = map.GetCellItem(position);
            int oldOrientation = map.GetCellItemOrientation(position);
            bool bothEmpty = item == GridMap.InvalidCellItem && oldItem == GridMap.InvalidCellItem;
            if (bothEmpty || (item == oldItem && orientation == oldOrientation))
                continue;
            apply.AddRange(new[] { position.X, position.Y, position.Z, item, orientation });
            revert.AddRange(new[] { position.X, position.Y, position.Z, oldItem, Math.Max(oldOrientation, 0) });
        }
        return new CellChanges(apply.ToArray(), revert.ToArray());
    }

    /// <summary>The marker scenes for every written cell whose tile has one (brief WFC §12.4), doors and lanes included.</summary>
    public static List<MarkerPlacement> PlanMarkers(GridMap map, DungeonResult result, TileSetBinding room, TileSetBinding corridor)
    {
        var placements = new List<MarkerPlacement>();
        foreach (var solve in result.Solves)
        {
            var binding = solve.IsCorridor ? corridor : room;
            for (int cell = 0; cell < solve.Input.Grid.CellCount; cell++)
            {
                if (solve.Input.Initial[cell] == WfcSolver.Excluded || solve.Cells[cell] < 0)
                    continue;
                var variant = binding.Compiled.Variants[solve.Cells[cell]];
                var scene = binding.Markers[variant.TileIndex];
                if (scene == null)
                    continue;
                var position = solve.Input.GridMapCell(cell);
                var basis = map.GetBasisWithOrthogonalIndex(GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
                placements.Add(new MarkerPlacement(position, scene, new Transform3D(basis, map.MapToLocal(position))));
            }
        }
        return placements;
    }

    public static Node3D MarkerContainer(GridMap map) => map.GetNodeOrNull<Node3D>(MarkerContainerName);

    /// <summary>
    /// The dungeon markers to remove: tagged markers whose cell, wherever the marker is now, the
    /// new dungeon writes or the previous one wrote and still holds. Markers on hand-placed or edited
    /// cells stay with them.
    /// </summary>
    public static List<Node3D> ReplacedMarkers(GridMap map, GeneratedRecord previous, ICollection<Vector3I> written)
    {
        var replaced = new List<Node3D>();
        var container = MarkerContainer(map);
        if (container == null)
            return replaced;
        foreach (var child in container.GetChildren())
        {
            if (child is not Node3D marker || !marker.HasMeta(MarkerTag))
                continue;
            var cell = map.LocalToMap(container.Transform * marker.Position);
            if (written.Contains(cell) || previous.Matches(cell, map.GetCellItem(cell), map.GetCellItemOrientation(cell)))
                replaced.Add(marker);
        }
        return replaced;
    }

    /// <summary>Instances a placement as a tagged dungeon marker, not yet in the tree. Null when the scene's root is not a Node3D.</summary>
    public static Node3D InstantiateMarker(MarkerPlacement placement)
    {
        var node = placement.Scene.Instantiate();
        if (node is not Node3D marker)
        {
            node.Free();
            return null;
        }
        marker.Transform = placement.Transform;
        marker.SetMeta(MarkerTag, true);
        return marker;
    }
}
