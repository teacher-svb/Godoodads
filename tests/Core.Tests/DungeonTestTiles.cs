using System.Linq;
using Godoodads.Common.Wfc.Core;

namespace Godoodads.Tests;

/// <summary>The M0 prototype's rules as a small synthetic tile set pair for godoodungeon Core tests.</summary>
public static class DungeonTestTiles
{
    private static string[] S(params string[] sockets) => sockets;

    public const string Edge = "outside";
    public const string Path = "path";

    // Socket order: +X, -X, +Y, -Y, +Z, -Z.
    public const int Floor = 0, Wall = 1, Corner = 2, Door = 3, Crate = 4;

    public static readonly TileDefinition[] RoomTiles =
    {
        new(S("in", "in", "v", "v", "in", "in"), TileRotation.None, 12),
        new(S("outside+", "in", "v", "v", "edge", "edge"), TileRotation.FourWay),
        new(S("outside+", "edge", "v", "v", "outside+", "edge"), TileRotation.FourWay),
        new(S("outside+", "in", "v", "v", "edge", "edge"), TileRotation.FourWay, 0),
        new(S("in", "in", "v", "v", "in", "in"), TileRotation.None, 1.5),
    };

    public const int End = 0, Straight = 1, Bend = 2, Tee = 3, Cross = 4, Empty = 5;

    public static readonly TileDefinition[] CorridorTiles =
    {
        new(S("path", "x", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("path", "path", "v", "v", "x", "x"), TileRotation.FourWay),
        new(S("path", "x", "v", "v", "path", "x"), TileRotation.FourWay),
        new(S("path", "path", "v", "v", "path", "x"), TileRotation.FourWay),
        new(S("path", "path", "v", "v", "path", "path")),
        new(S("x", "x", "v", "v", "x", "x"), TileRotation.None, 20),
    };

    public static CompiledTileSet Room() => CompiledTileSet.Compile(RoomTiles.ToList(), Directions.All3D);

    public static CompiledTileSet Corridor() => CompiledTileSet.Compile(CorridorTiles.ToList(), Directions.All3D);
}
