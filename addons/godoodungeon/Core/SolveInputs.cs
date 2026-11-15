using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// One WFC solve of a dungeon: where its grid sits in the GridMap, the grid, the starting state per
/// cell (a fixed variant, <see cref="WfcSolver.Free"/> or <see cref="WfcSolver.Excluded"/>) and the
/// boundary sockets. <see cref="Error"/> is set instead when the input cannot be built.
/// </summary>
public sealed record SolveInput(Vector3I Origin, GridTopology Grid, int[] Initial, Socket?[] Boundary, string Error = null)
{
    /// <summary>The variants each cell may use, for the corridor solve; null for rooms.</summary>
    public CellRestrictions Restrictions { get; init; }

    public bool IsValid => Error == null;

    /// <summary>The GridMap cell of a grid cell.</summary>
    public Vector3I GridMapCell(int cell) => Origin + Grid.Coordinates(cell);
}

/// <summary>Builds the WFC inputs of a dungeon from its layout (brief godoodungeon §4.4).</summary>
public static class SolveInputs
{
    /// <summary>The WFC direction a door side points to: layout Y is GridMap Z.</summary>
    public static int Direction(DoorSide side) => side switch
    {
        DoorSide.PosX => Directions.PosX,
        DoorSide.NegX => Directions.NegX,
        DoorSide.PosY => Directions.PosZ,
        _ => Directions.NegZ,
    };

    /// <summary>
    /// The solve of one room: its footprint × <paramref name="roomHeight"/> layers from the layout's
    /// bottom layer, with <paramref name="edgeSocket"/>- as the boundary on the four side faces and every
    /// door cell fixed to the door variant whose outside faces out of the room. Other cells are free.
    /// </summary>
    public static SolveInput Room(DungeonLayout layout, int room, int roomHeight, CompiledTileSet tiles, int doorTile, string edgeSocket)
    {
        var rect = layout.Rooms[room];
        var origin = layout.GridCell(rect.Position);
        var grid = GridTopology.Create3D(new Vector3I(rect.Size.X, Math.Max(roomHeight, 1), rect.Size.Y));
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);

        string edge = edgeSocket?.Trim() ?? "";
        if (!Socket.TryParse(edge + "-", false, out var boundarySocket, out string socketError))
            return new SolveInput(origin, grid, initial, null, $"Room Edge Socket '{edge}': {socketError}");
        var boundary = new Socket?[Directions.Count];
        foreach (int direction in DungeonTileRules.HorizontalDirections)
            boundary[direction] = boundarySocket;

        foreach (var door in layout.Doors)
        {
            if (door.Room != room)
                continue;
            int variant = DungeonTileRules.DoorVariant(tiles, doorTile, edge, Direction(door.Side));
            if (variant < 0)
                return new SolveInput(origin, grid, initial, boundary,
                    $"The Door Tile has no rotation whose '{edge}+' face points {Directions.Name(Direction(door.Side))}, so the door at {layout.GridCell(door.Cell)} cannot be placed.");
            var local = door.Cell - rect.Position;
            initial[grid.Index(new Vector3I(local.X, 0, local.Y))] = variant;
        }
        return new SolveInput(origin, grid, initial, boundary);
    }

    /// <summary>
    /// The corridor solve on the bottom layer of the layout grid, from a <see cref="CorridorPlan"/>.
    /// Room cells and hand-placed cells are excluded. Every routed cell may only use corridor variants
    /// with exactly its path faces, and every other cell only variants without path faces, so the
    /// solve draws the planned corridors and nothing else, while weights still choose among the
    /// variants that fit.
    /// </summary>
    /// <param name="handPlaced">GridMap cells that hold hand-placed items; null means none.</param>
    public static SolveInput Corridors(DungeonLayout layout, CorridorPlan plan, CompiledTileSet tiles, string pathSocket,
        Func<Vector3I, bool> handPlaced = null)
    {
        var origin = layout.Origin;
        var grid = GridTopology.Create3D(new Vector3I(plan.Size.X, 1, plan.Size.Y));
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);
        string path = pathSocket?.Trim() ?? "";

        // One class of variants per path face mask that occurs, mask 0 (no path faces) first.
        var classOfMask = new Dictionary<int, int>();
        var classes = new List<IReadOnlyList<int>>();
        int ClassOf(int mask)
        {
            if (classOfMask.TryGetValue(mask, out int index))
                return index;
            var variants = Enumerable.Range(0, tiles.Variants.Count).Where(v => DungeonTileRules.PathFaces(tiles.Variants[v], path) == mask).ToList();
            classOfMask[mask] = classes.Count;
            classes.Add(variants);
            return classes.Count - 1;
        }
        ClassOf(0);
        var classOfCell = new int[grid.CellCount];
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            var at = grid.Coordinates(cell);
            classOfCell[cell] = ClassOf(plan.Faces[at.Z * plan.Size.X + at.X]);
        }
        foreach (var (mask, index) in classOfMask)
            if (classes[index].Count == 0)
                return new SolveInput(origin, grid, initial, null,
                    $"The corridor tile set has no piece with '{path}' faces on {DescribeMask(mask)}, which the corridors need.");

        foreach (var room in layout.Rooms)
            foreach (var cell in room.Cells())
                if (cell.X >= 0 && cell.Y >= 0 && cell.X < plan.Size.X && cell.Y < plan.Size.Y)
                    initial[grid.Index(new Vector3I(cell.X, 0, cell.Y))] = WfcSolver.Excluded;
        if (handPlaced != null)
            for (int cell = 0; cell < grid.CellCount; cell++)
                if (handPlaced(origin + grid.Coordinates(cell)))
                    initial[cell] = WfcSolver.Excluded;
        for (int cell = 0; cell < grid.CellCount; cell++)
            if (initial[cell] == WfcSolver.Excluded)
                classOfCell[cell] = CellRestrictions.None;

        return new SolveInput(origin, grid, initial, null) { Restrictions = new CellRestrictions(classOfCell, classes) };
    }

    /// <summary>"+X and -Z" for a mask of WFC directions.</summary>
    public static string DescribeMask(int mask) => mask == 0 ? "no side"
        : string.Join(" and ", DungeonTileRules.HorizontalDirections.Where(d => (mask & (1 << d)) != 0).Select(Directions.Name));
}
