using System.Collections.Generic;
using System.Linq;
using Godoodads.Common.Wfc.Core;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonTileRulesTests
{
    private static string[] S(params string[] sockets) => sockets;

    // Socket order: +X, -X, +Y, -Y, +Z, -Z.
    private static readonly (string Name, TileDefinition Tile)[] RoomTiles =
    {
        ("Floor", new(S("in", "in", "v", "v", "in", "in"))),
        ("Wall", new(S("outside+", "in", "v", "v", "edge", "edge"), TileRotation.FourWay)),
        ("Corner", new(S("outside+", "edge", "v", "v", "outside+", "edge"), TileRotation.FourWay)),
        ("Door", new(S("outside+", "in", "v", "v", "edge", "edge"), TileRotation.FourWay, 0)),
    };

    private static readonly TileDefinition[] CorridorTiles =
    {
        new(S("path", "x", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("path", "path", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("x", "x", "v", "v", "x", "x"), TileRotation.None, 20),
        new(S("path", "path", "v", "v", "path", "x"), TileRotation.FourWay),
    };

    private const int Door = 3;

    private static CompiledTileSet Compile(IEnumerable<TileDefinition> tiles) => CompiledTileSet.Compile(tiles.ToList(), Directions.All3D);

    private static (List<string> Errors, List<string> Warnings) Check(IEnumerable<TileDefinition> room = null, int door = Door, string edge = "outside",
        IEnumerable<TileDefinition> corridor = null, string path = "path")
    {
        var roomTiles = (room ?? RoomTiles.Select(t => t.Tile)).ToList();
        var names = RoomTiles.Select(t => t.Name).Concat(Enumerable.Range(RoomTiles.Length, 10).Select(i => $"Tile {i}")).Take(roomTiles.Count).ToList();
        return DungeonTileRules.Check(Compile(roomTiles), names, door, edge, Compile(corridor ?? CorridorTiles), path);
    }

    private static IEnumerable<TileDefinition> RoomWith(int index, TileDefinition tile) =>
        RoomTiles.Select((t, i) => i == index ? tile : t.Tile);

    [Fact]
    public void The_m0_prototype_rules_pass()
    {
        var (errors, warnings) = Check();
        Assert.Empty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    public void A_symmetric_edge_socket_is_an_error()
    {
        var room = RoomWith(1, new TileDefinition(S("outside", "in", "v", "v", "edge", "edge"), TileRotation.FourWay));
        var error = Assert.Single(Check(room).Errors);
        Assert.StartsWith("Room Edge Socket 'outside' must be one-sided", error);
        Assert.EndsWith("Used plain by: Wall.", error);
    }

    [Fact]
    public void Tiles_must_not_use_the_boundary_side_of_the_edge_socket()
    {
        var room = RoomWith(2, new TileDefinition(S("outside-", "edge", "v", "v", "outside+", "edge"), TileRotation.FourWay));
        Assert.StartsWith("Tiles use 'outside-'", Assert.Single(Check(room).Errors));
    }

    [Fact]
    public void Without_any_outer_face_nothing_stands_at_the_edge()
    {
        var errors = Check(edge: "rim").Errors;
        Assert.Contains(errors, e => e.StartsWith("No room tile has a face 'rim+'"));
        Assert.Contains(errors, e => e.StartsWith("The Door Tile 'Door' needs exactly one horizontal face with 'rim+'"));
    }

    [Fact]
    public void The_door_tile_must_be_chosen_and_have_one_outer_face()
    {
        Assert.Equal("Choose the Door Tile from the room tile set.", Assert.Single(Check(door: -1).Errors));
        Assert.Equal("Choose the Door Tile from the room tile set.", Assert.Single(Check(door: 9).Errors));
        Assert.Equal("The Door Tile 'Corner' needs exactly one horizontal face with 'outside+' (its outside), but has 2.", Assert.Single(Check(door: 2).Errors));
        Assert.StartsWith("The Door Tile 'Floor' needs exactly one", Assert.Single(Check(door: 0).Errors));
    }

    [Fact]
    public void A_door_tile_with_weight_is_a_warning()
    {
        var (errors, warnings) = Check(door: 1);
        Assert.Empty(errors);
        Assert.StartsWith("The Door Tile 'Wall' has a weight above 0", Assert.Single(warnings));
    }

    [Fact]
    public void Empty_socket_names_are_errors()
    {
        Assert.Contains(Check(edge: " ").Errors, e => e.StartsWith("Room Edge Socket is empty."));
        Assert.Contains(Check(path: "").Errors, e => e.StartsWith("Path Socket is empty."));
    }

    [Fact]
    public void The_path_socket_must_appear_in_the_corridor_tile_set()
    {
        Assert.StartsWith("No corridor tile has a face 'road'", Check(path: "road").Errors.First());
    }

    [Fact]
    public void Straights_are_needed_along_both_axes()
    {
        var onlyX = new[] { new TileDefinition(S("path", "path", "v", "v", "x", "x")), CorridorTiles[2], CorridorTiles[3] };
        Assert.StartsWith("The corridor tile set has no straight piece along Z", Assert.Single(Check(corridor: onlyX).Errors));

        var noStraight = new[] { CorridorTiles[0], CorridorTiles[2], CorridorTiles[3] };
        Assert.Equal(2, Check(corridor: noStraight).Errors.Count(e => e.Contains("no straight piece")));
    }

    [Fact]
    public void T_pieces_are_needed_in_all_four_ways()
    {
        var withoutT = CorridorTiles.Take(3);
        Assert.StartsWith("The corridor tile set has no T piece", Assert.Single(Check(corridor: withoutT).Errors));

        var oneWayT = CorridorTiles.Take(3).Append(new TileDefinition(S("path", "path", "v", "v", "path", "x")));
        Assert.StartsWith("The corridor tile set has no T piece", Assert.Single(Check(corridor: oneWayT).Errors));
    }

    [Fact]
    public void An_empty_tile_with_weight_is_needed()
    {
        var withoutEmpty = new[] { CorridorTiles[0], CorridorTiles[1], CorridorTiles[3] };
        Assert.StartsWith("The corridor tile set has no tile without 'path' faces", Assert.Single(Check(corridor: withoutEmpty).Errors));

        var weightlessEmpty = new[] { CorridorTiles[0], CorridorTiles[1], CorridorTiles[3], new TileDefinition(S("x", "x", "v", "v", "x", "x"), TileRotation.None, 0) };
        Assert.Single(Check(corridor: weightlessEmpty).Errors);
    }

    [Fact]
    public void Lookups_find_the_door_for_every_side_the_straights_and_the_empty_tile()
    {
        var room = Compile(RoomTiles.Select(t => t.Tile));
        foreach (int outward in DungeonTileRules.HorizontalDirections)
        {
            int variant = DungeonTileRules.DoorVariant(room, Door, "outside", outward);
            Assert.True(variant >= 0);
            Assert.Equal(Door, room.Variants[variant].TileIndex);
            Assert.Equal(new Socket("outside", SocketKind.HorizontalPlus), room.Variants[variant].Sockets[outward]);
            Assert.Equal("in", room.Variants[variant].Sockets[Directions.Opposite(outward)].Name);
        }

        var corridor = Compile(CorridorTiles);
        int alongX = DungeonTileRules.StraightVariant(corridor, "path", alongX: true);
        int alongZ = DungeonTileRules.StraightVariant(corridor, "path", alongX: false);
        Assert.Equal((1 << Directions.PosX) | (1 << Directions.NegX), DungeonTileRules.PathFaces(corridor.Variants[alongX], "path"));
        Assert.Equal((1 << Directions.PosZ) | (1 << Directions.NegZ), DungeonTileRules.PathFaces(corridor.Variants[alongZ], "path"));
        int empty = DungeonTileRules.EmptyVariant(corridor, "path");
        Assert.Equal(2, corridor.Variants[empty].TileIndex);
    }
}
