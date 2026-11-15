using System;
using System.Collections.Generic;
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
    /// <summary>The guided paths to lay out before solving, for the corridor solve; null for rooms.</summary>
    public PathRequest Paths { get; init; }

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
    /// The corridor solve: the layout grid × <paramref name="corridorHeight"/> layers from the bottom.
    /// Room cells and hand-placed cells are excluded. The Wall Clearance ring around each room is
    /// fixed to the empty variant, so routes never touch a room wall. Outside each door, a lane of
    /// max(clearance, 1) cells is fixed to the straight variant that runs through the door; its face
    /// towards the excluded door counts as connected, so guided paths routes from its outer end.
    /// Every other cell is free; guided paths keeps path variants off them unless a route passes.
    /// </summary>
    /// <param name="bounds">The layout grid size (the bounds' X and Z).</param>
    /// <param name="handPlaced">GridMap cells that hold hand-placed items; null means none.</param>
    public static SolveInput Corridors(DungeonLayout layout, Vector2I bounds, int corridorHeight, int wallClearance,
        CompiledTileSet tiles, string pathSocket, float wander, Func<Vector3I, bool> handPlaced = null)
    {
        var origin = layout.Origin;
        var grid = GridTopology.Create3D(new Vector3I(bounds.X, Math.Max(corridorHeight, 1), bounds.Y));
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);
        string path = pathSocket?.Trim() ?? "";
        var paths = new PathRequest(path, wander, origin);

        int empty = DungeonTileRules.EmptyVariant(tiles, path);
        int alongX = DungeonTileRules.StraightVariant(tiles, path, alongX: true);
        int alongZ = DungeonTileRules.StraightVariant(tiles, path, alongX: false);
        if (empty < 0 || alongX < 0 || alongZ < 0)
            return new SolveInput(origin, grid, initial, null, "The corridor tile set needs straight pieces along X and Z and an empty tile with a weight above 0.") { Paths = paths };

        bool Inside(Vector2I cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < bounds.X && cell.Y < bounds.Y;
        void SetColumn(Vector2I cell, int value, int layers)
        {
            if (!Inside(cell))
                return;
            for (int y = 0; y < layers; y++)
                initial[grid.Index(new Vector3I(cell.X, y, cell.Y))] = value;
        }

        foreach (var room in layout.Rooms)
            foreach (var cell in room.Ring(wallClearance))
                SetColumn(cell, empty, grid.Size.Y);
        foreach (var door in layout.Doors)
        {
            int straight = door.Side is DoorSide.PosX or DoorSide.NegX ? alongX : alongZ;
            int lane = Math.Max(wallClearance, 1);
            for (int step = 0; step < lane; step++)
                if (Inside(door.Outside + door.Side.Offset() * step))
                    initial[grid.Index(new Vector3I(door.Outside.X + door.Side.Offset().X * step, 0, door.Outside.Y + door.Side.Offset().Y * step))] = straight;
        }
        // Rooms and hand-placed cells last, so nothing is ever fixed on top of them.
        foreach (var room in layout.Rooms)
            foreach (var cell in room.Cells())
                SetColumn(cell, WfcSolver.Excluded, grid.Size.Y);
        if (handPlaced != null)
            for (int cell = 0; cell < grid.CellCount; cell++)
                if (handPlaced(origin + grid.Coordinates(cell)))
                    initial[cell] = WfcSolver.Excluded;

        return new SolveInput(origin, grid, initial, null) { Paths = paths };
    }
}
