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
}
