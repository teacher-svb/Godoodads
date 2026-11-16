using System.Linq;
using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Dungeon.Adapters;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon.Demo;

/// <summary>
/// The M2 review scene (card dgn-m2-review): the whole godoodungeon pipeline without the dialog, on
/// the M0 prototype tile sets. Two blocks of hand-placed cells show that rooms and corridors avoid
/// them. Run it headless, then open prototype_m2_dungeon.tscn in the editor:
/// godot --headless --path . res://demo/godoodungeon/prototype/build_m2_dungeon.tscn
/// </summary>
public partial class BuildM2Dungeon : Node
{
    private const string Directory = "res://demo/godoodungeon/prototype";

    public override void _Ready()
    {
        var room = GD.Load<WfcTileSet>($"{Directory}/prototype_room_tileset.tres");
        var corridor = GD.Load<WfcTileSet>($"{Directory}/prototype_corridor_tileset.tres");
        var settings = new DungeonSettings
        {
            RoomTileSet = room,
            CorridorTileSet = corridor,
            Door = room.Tiles.Single(t => t.ResourceName == "Door").Item,
            BoundsSize = new Vector3I(48, 1, 48),
            RoomCount = 12,
            Seed = 2026,
        };

        var map = new GridMap { Name = "GridMap", MeshLibrary = room.Library };
        int crate = room.Tiles.Single(t => t.ResourceName == "Crate").Item;
        foreach (var block in new[] { new Rect2I(20, 20, 6, 3), new Rect2I(6, 36, 2, 6) })
            for (int x = block.Position.X; x < block.End.X; x++)
                for (int z = block.Position.Y; z < block.End.Y; z++)
                    map.SetCellItem(new Vector3I(x, 0, z), crate);

        var tileSets = DungeonTileSets.Check(settings, map);
        foreach (string error in tileSets.Errors)
            GD.PushError($"M2 dungeon: {error}");
        if (!tileSets.IsValid)
        {
            GetTree().Quit(1);
            return;
        }

        var record = DungeonGridMap.ReadRecord(map);
        var handPlaced = DungeonGridMap.ReadHandPlaced(map, settings.BoundsOrigin, settings.BoundsSize, record);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var result = new DungeonJob(DungeonGridMap.Request(settings, tileSets, handPlaced)).RunNow();
        GD.Print($"M2 dungeon: {result.Status} in {clock.ElapsedMilliseconds} ms, layout attempt {result.LayoutAttempt + 1}, "
            + $"{result.Layout?.Rooms.Count} rooms, {result.Layout?.Doors.Count} doors, {result.RouteCount} corridors over {result.RouteCells} cells, "
            + $"{result.Backtracks} backtracks, {result.Restarts} restarts.");
        foreach (var failure in result.Attempts)
            GD.Print($"  attempt {failure.Attempt + 1} failed: {failure.Message}");
        foreach (string error in result.Errors)
            GD.PushError($"M2 dungeon: {error}");
        if (result.Status != DungeonStatus.Solved)
        {
            GetTree().Quit(1);
            return;
        }

        var (changes, newRecord) = DungeonGridMap.Write(map, record, DungeonGridMap.Cells(result, tileSets.Room, tileSets.Corridor));
        GridMapCells.Apply(map, changes.Do);
        DungeonGridMap.WriteRecord(map, newRecord.Encode());
        DungeonSettings.StoreOn(map, settings);
        GD.Print($"M2 dungeon: wrote {changes.Do.Length / CellChanges.Stride} cells; {handPlaced.Count} hand-placed cells kept.");
        GD.Print(LayoutText.Render(result.Layout, new Vector2I(settings.BoundsSize.X, settings.BoundsSize.Z),
            Occupancy(handPlaced, settings), result.CorridorFaces));

        var root = new Node3D { Name = "PrototypeM2Dungeon" };
        root.AddChild(map);
        map.Owner = root;
        var sun = new DirectionalLight3D { Name = "Sun", ShadowEnabled = true, RotationDegrees = new Vector3(-60, -30, 0) };
        root.AddChild(sun);
        sun.Owner = root;
        var environment = new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                TonemapMode = Godot.Environment.ToneMapper.Filmic,
            },
        };
        root.AddChild(environment);
        environment.Owner = root;
        var packed = new PackedScene();
        packed.Pack(root);
        var saved = ResourceSaver.Save(packed, $"{Directory}/prototype_m2_dungeon.tscn");
        GD.Print($"M2 dungeon: saved ({saved}).");
        root.Free();
        GetTree().Quit();
    }

    private static OccupancyGrid Occupancy(System.Collections.Generic.IEnumerable<Vector3I> cells, DungeonSettings settings)
    {
        var grid = new OccupancyGrid(new Vector2I(settings.BoundsSize.X, settings.BoundsSize.Z));
        foreach (var cell in cells)
            grid.Take(new Vector2I(cell.X - settings.BoundsOrigin.X, cell.Z - settings.BoundsOrigin.Z));
        return grid;
    }
}
