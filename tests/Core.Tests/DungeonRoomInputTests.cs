using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonRoomInputTests
{
    private static readonly RoomRect Rect = new(new Vector2I(4, 6), new Vector2I(7, 5));

    private static DungeonLayout OneRoom(params Door[] doors)
    {
        var layout = new DungeonLayout { Origin = new Vector3I(100, 3, -20) };
        layout.Rooms.Add(Rect);
        layout.Doors.AddRange(doors);
        return layout;
    }

    private static readonly Door East = new(0, new Vector2I(10, 8), DoorSide.PosX);
    private static readonly Door West = new(0, new Vector2I(4, 9), DoorSide.NegX);
    private static readonly Door South = new(0, new Vector2I(6, 10), DoorSide.PosY);
    private static readonly Door North = new(0, new Vector2I(8, 6), DoorSide.NegY);

    private static (SolveInput Input, SolveResult Result, CompiledTileSet Tiles) Solve(DungeonLayout layout, int height = 1, long seed = 1)
    {
        var tiles = DungeonTestTiles.Room();
        var input = SolveInputs.Room(layout, 0, height, tiles, DungeonTestTiles.Door, DungeonTestTiles.Edge);
        Assert.True(input.IsValid, input.Error);
        var result = new WfcSolver(input.Grid, tiles, input.Initial).Solve(new SolveOptions { Seed = seed, Boundary = input.Boundary });
        return (input, result, tiles);
    }

    /// <summary>Every face towards the outside of the room is outside+, and no outside face points inwards.</summary>
    private static void AssertClosed(SolveInput input, SolveResult result, CompiledTileSet tiles)
    {
        Assert.Equal(SolveStatus.Solved, result.Status);
        for (int cell = 0; cell < input.Grid.CellCount; cell++)
        {
            var sockets = tiles.Variants[result.Cells[cell]].Sockets;
            foreach (int direction in DungeonTileRules.HorizontalDirections)
            {
                bool outward = input.Grid.Neighbour(cell, direction) < 0;
                bool edgeOut = sockets[direction].Name == DungeonTestTiles.Edge && sockets[direction].Kind == SocketKind.HorizontalPlus;
                Assert.True(outward == edgeOut, $"cell {input.Grid.Coordinates(cell)} faces {Directions.Name(direction)} with '{sockets[direction]}'");
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void A_room_solves_closed_with_a_door_on_any_side(int side)
    {
        var door = new[] { East, West, South, North }[side];
        var (input, result, tiles) = Solve(OneRoom(door), seed: side + 1);
        AssertClosed(input, result, tiles);

        var local = door.Cell - Rect.Position;
        int cell = input.Grid.Index(new Vector3I(local.X, 0, local.Y));
        var variant = tiles.Variants[result.Cells[cell]];
        Assert.Equal(DungeonTestTiles.Door, variant.TileIndex);
        Assert.Equal(new Socket(DungeonTestTiles.Edge, SocketKind.HorizontalPlus), variant.Sockets[SolveInputs.Direction(door.Side)]);
    }

    [Fact]
    public void A_room_with_doors_on_all_sides_and_two_layers_solves_closed()
    {
        var (input, result, tiles) = Solve(OneRoom(East, West, South, North), height: 2, seed: 7);
        Assert.Equal(new Vector3I(7, 2, 5), input.Grid.Size);
        AssertClosed(input, result, tiles);
        int doors = result.Cells.Count(v => tiles.Variants[v].TileIndex == DungeonTestTiles.Door);
        Assert.Equal(4, doors); // weight 0: only the fixed doors
    }

    [Fact]
    public void Rooms_solve_closed_over_many_seeds()
    {
        for (long seed = 0; seed < 30; seed++)
        {
            var (input, result, tiles) = Solve(OneRoom(East, North), seed: seed);
            AssertClosed(input, result, tiles);
        }
    }

    [Fact]
    public void The_input_sits_at_the_room_in_gridmap_cells()
    {
        var input = SolveInputs.Room(OneRoom(East), 0, 1, DungeonTestTiles.Room(), DungeonTestTiles.Door, DungeonTestTiles.Edge);
        Assert.Equal(new Vector3I(104, 3, -14), input.Origin);
        Assert.Equal(new Vector3I(110, 3, -12), input.GridMapCell(input.Grid.Index(new Vector3I(6, 0, 2))));
        Assert.Equal(1, input.Initial.Count(v => v != WfcSolver.Free));
        Assert.All(DungeonTileRules.HorizontalDirections, d => Assert.Equal(new Socket("outside", SocketKind.HorizontalMinus), input.Boundary[d]));
        Assert.Null(input.Boundary[Directions.PosY]);
        Assert.Null(input.Boundary[Directions.NegY]);
    }

    [Fact]
    public void A_door_tile_with_two_edge_faces_is_rejected()
    {
        var input = SolveInputs.Room(OneRoom(East), 0, 1, DungeonTestTiles.Room(), DungeonTestTiles.Corner, DungeonTestTiles.Edge);
        Assert.False(input.IsValid);
        Assert.Equal("The Door Tile has no rotation whose 'outside+' face points +X, so the door at (110, 3, -12) cannot be placed.", input.Error);
        Assert.Equal(-1, DungeonTileRules.DoorVariant(DungeonTestTiles.Room(), DungeonTestTiles.Corner, DungeonTestTiles.Edge, Directions.PosX));
    }

    [Fact]
    public void An_unusable_edge_socket_is_an_error()
    {
        var input = SolveInputs.Room(OneRoom(East), 0, 1, DungeonTestTiles.Room(), DungeonTestTiles.Door, "");
        Assert.False(input.IsValid);
        Assert.StartsWith("Room Edge Socket ''", input.Error);
    }
}
