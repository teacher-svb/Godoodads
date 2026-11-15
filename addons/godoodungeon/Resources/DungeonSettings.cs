using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using Godoodads.Common.Wfc;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon;

/// <summary>
/// All options of one dungeon generation (brief godoodungeon §4). Shown in the dialog and stored on
/// the GridMap afterwards, so a dungeon can be reproduced from the scene file.
/// </summary>
[Tool, GlobalClass]
public partial class DungeonSettings : Resource
{
    /// <summary>Metadata key under which a GridMap stores the settings of its last dungeon.</summary>
    public const string MetaKey = "godoodads_dungeon_settings";

    // ---- Seed and actions (ungrouped, at the top) ----------------------------------------

    private bool _randomiseSeed;

    /// <summary>Generate picks a new seed first. Off keeps the seed, so Generate repeats its result.</summary>
    [Export]
    public bool RandomiseSeed
    {
        get => _randomiseSeed;
        set
        {
            bool changed = _randomiseSeed != value;
            _randomiseSeed = value;
            if (changed)
                NotifyPropertyListChanged();
        }
    }

    [Export] public long Seed { get; set; }

    /// <summary>Raised by the Clear Dungeon button. The godoodungeon plugin removes the last dungeon's cells.</summary>
    [Signal] public delegate void ClearRequestedEventHandler();

    /// <summary>Removes the cells the last dungeon wrote, as one undo step. Cells you placed or edited stay.</summary>
    [ExportToolButton("Clear Dungeon", Icon = "Clear")]
    public Callable ClearDungeonButton => Callable.From(RequestClear);

    private void RequestClear() => EmitSignal(SignalName.ClearRequested);

    // ---- Tile sets -------------------------------------------------------------------------

    private WfcTileSet _roomTileSet;

    /// <summary>Floors, walls, corners, doors and props. Must use the same MeshLibrary as the corridor tile set and the GridMap.</summary>
    [ExportGroup("Tile Sets")]
    [Export]
    public WfcTileSet RoomTileSet
    {
        get => _roomTileSet;
        set
        {
            _roomTileSet = value;
            NotifyPropertyListChanged(); // the Door Tile list follows the room tile set
        }
    }

    /// <summary>Corridor pieces and an empty tile.</summary>
    [Export] public WfcTileSet CorridorTileSet { get; set; }

    /// <summary>Socket name of the faces that point out of a room: tiles use name+, the room bounds name- (brief §4.2).</summary>
    [Export] public string RoomEdgeSocket { get; set; } = "outside";

    /// <summary>The room tile placed at every door. Its one face with the edge socket is the door's outside.</summary>
    [Export] public int DoorTile { get; set; } = -1;

    /// <summary>Socket name of the corridor tiles' open faces.</summary>
    [Export] public string PathSocket { get; set; } = "path";

    /// <summary>0 gives the shortest corridors, 1 the most meandering.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float PathWander { get; set; } = 0.3f;

    // ---- Bounds --------------------------------------------------------------------------

    [ExportGroup("Bounds")]
    [Export] public Vector3I BoundsOrigin { get; set; }

    /// <summary>X and Z hold the dungeon; Y holds the room and corridor layers, from the bottom.</summary>
    [Export] public Vector3I BoundsSize { get; set; } = new(48, 1, 48);

    // ---- Layout --------------------------------------------------------------------------

    [ExportGroup("Rooms")]
    [Export(PropertyHint.Range, "2,200,1,or_greater")] public int RoomCount { get; set; } = 12;

    /// <summary>Smallest room on X and Z, walls included.</summary>
    [Export] public Vector2I MinRoomSize { get; set; } = new(5, 5);

    /// <summary>Largest room on X and Z, walls included.</summary>
    [Export] public Vector2I MaxRoomSize { get; set; } = new(9, 9);

    [Export(PropertyHint.Range, "1,16,1,or_greater")] public int RoomHeight { get; set; } = 1;

    /// <summary>Free cells between rooms, and between rooms and hand-placed cells.</summary>
    [Export(PropertyHint.Range, "1,32,1,or_greater")] public int Gap { get; set; } = 3;

    /// <summary>Free cells between rooms and the edge of the bounds.</summary>
    [Export(PropertyHint.Range, "0,32,1,or_greater")] public int Margin { get; set; } = 1;

    /// <summary>Cells around each room that corridors keep free, except outside the doors. 0 lets corridors run along walls.</summary>
    [ExportGroup("Corridors")]
    [Export(PropertyHint.Range, "0,8,1,or_greater")] public int WallClearance { get; set; } = 1;

    /// <summary>Loops per tree edge of the room graph, 0 to 1.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float ExtraConnections { get; set; } = 0.15f;

    [Export(PropertyHint.Range, "1,4,1")] public int MaxDoorsPerRoom { get; set; } = 4;

    /// <summary>Corridor layers from the bottom of the bounds. 1 keeps corridors on the floor layer.</summary>
    [Export(PropertyHint.Range, "1,16,1,or_greater")] public int CorridorHeight { get; set; } = 1;

    // ---- Limits --------------------------------------------------------------------------

    /// <summary>Whole layouts tried before Generate gives up, each from a seed derived from the Generate seed.</summary>
    [ExportGroup("Limits")]
    [Export(PropertyHint.Range, "1,64,1,or_greater")] public int LayoutAttempts { get; set; } = 4;

    [Export(PropertyHint.Range, "1,1000,1,or_greater")] public int AttemptsPerRoom { get; set; } = 50;
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")] public int MaxBacktracks { get; set; } = 2000;
    [Export(PropertyHint.Range, "0,1000,1,or_greater")] public int MaxRestarts { get; set; } = 20;

    /// <summary>The layout numbers as Core settings. The layout grid is the bounds' X and Z.</summary>
    public LayoutSettings ToLayoutSettings() => new()
    {
        Bounds = new Vector2I(BoundsSize.X, BoundsSize.Z),
        RoomCount = RoomCount,
        MinRoomSize = MinRoomSize,
        MaxRoomSize = MaxRoomSize,
        RoomHeight = RoomHeight,
        WallClearance = WallClearance,
        Gap = Gap,
        Margin = Margin,
        ExtraConnections = ExtraConnections,
        MaxDoorsPerRoom = MaxDoorsPerRoom,
        AttemptsPerRoom = AttemptsPerRoom,
    };

    /// <summary>Errors in the bounds and heights that the layout settings do not cover.</summary>
    public List<string> CheckBounds()
    {
        var errors = new List<string>();
        if (BoundsSize.Y < 1)
            errors.Add("The bounds need at least 1 layer on Y.");
        else
        {
            if (RoomHeight > BoundsSize.Y)
                errors.Add($"Room Height is {RoomHeight}, but the bounds have {BoundsSize.Y} layer(s) on Y.");
            if (CorridorHeight > BoundsSize.Y)
                errors.Add($"Corridor Height is {CorridorHeight}, but the bounds have {BoundsSize.Y} layer(s) on Y.");
        }
        if (CorridorHeight < 1)
            errors.Add($"Corridor Height is {CorridorHeight}. Corridors need at least 1 layer.");
        if (LayoutAttempts < 1)
            errors.Add($"Layout Attempts is {LayoutAttempts}. Use at least 1.");
        return errors;
    }

    /// <summary>Sets a new random seed, different from the current one and from any in <paramref name="avoid"/>.</summary>
    public void PickNewSeed(ICollection<long> avoid = null)
    {
        long previous = Seed;
        long seed;
        do
            seed = GD.Randi();
        while (seed == previous || (avoid != null && avoid.Contains(seed)));
        Seed = seed;
        NotifyPropertyListChanged();
    }

    /// <summary>The settings stored on a GridMap, or null.</summary>
    public static DungeonSettings StoredOn(GridMap map) =>
        map.HasMeta(MetaKey) ? map.GetMeta(MetaKey).As<DungeonSettings>() : null;

    /// <summary>Stores settings on a GridMap. Null removes the entry, so undo leaves nothing behind.</summary>
    public static void StoreOn(GridMap map, DungeonSettings settings)
    {
        if (settings == null)
            map.RemoveMeta(MetaKey);
        else
            map.SetMeta(MetaKey, settings);
    }

    /// <summary>Readable names of a tile set's tiles: the tile's name, its item's name, or its position.</summary>
    public static string[] TileNames(WfcTileSet tileSet)
    {
        if (tileSet?.Tiles == null)
            return System.Array.Empty<string>();
        return tileSet.Tiles.Select((tile, i) =>
            tile == null ? $"Tile {i}"
            : !string.IsNullOrEmpty(tile.ResourceName) ? tile.ResourceName
            : tile.Item == WfcTile.EmptyItem ? $"Tile {i} (empty)"
            : tileSet.Library != null && tileSet.Library.GetItemList().Contains(tile.Item) && !string.IsNullOrEmpty(tileSet.Library.GetItemName(tile.Item))
                ? tileSet.Library.GetItemName(tile.Item)
                : $"Tile {i} (item {tile.Item})").ToArray();
    }

    public override void _ValidateProperty(Dictionary property)
    {
        string name = property["name"].AsString();
        var usage = (PropertyUsageFlags)property["usage"].AsInt64();
        if (name == PropertyName.DoorTile)
        {
            // A drop-down of the room tile set's tiles, stored as the tile's index.
            var names = TileNames(_roomTileSet);
            property["hint"] = (long)PropertyHint.Enum;
            property["hint_string"] = names.Length == 0 ? "Choose a Room Tile Set first:-1"
                : "(none):-1," + string.Join(",", names.Select((n, i) => $"{n.Replace(",", " ").Replace(":", " ")}:{i}"));
        }
        else if (name == PropertyName.Seed && RandomiseSeed)
            property["usage"] = (long)(usage | PropertyUsageFlags.ReadOnly);
    }
}
