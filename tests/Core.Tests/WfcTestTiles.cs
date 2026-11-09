using Godoodads.WfcGenerator.Core;
using Xunit;

namespace Godoodads.Tests;

internal static class WfcTestTiles
{
    public static TileDefinition Tile3D(string px, string nx, string py, string ny, string pz, string nz,
        TileRotation rotation = TileRotation.None) => new(new[] { px, nx, py, ny, pz, nz }, rotation);

    public static TileDefinition Tile2D(string px, string nx, string pz, string nz,
        TileRotation rotation = TileRotation.None) => new(new[] { px, nx, null, null, pz, nz }, rotation);

    public static TileDefinition Uniform3D(string socket) => Tile3D(socket, socket, socket, socket, socket, socket);

    /// <summary>Asserts that every pair of solved neighbouring cells is allowed by the adjacency table.</summary>
    public static void AssertValid(GridTopology grid, CompiledTileSet tiles, int[] cells)
    {
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (cells[cell] < 0)
                continue;
            foreach (int direction in grid.ActiveDirections)
            {
                int neighbour = grid.Neighbour(cell, direction);
                if (neighbour >= 0 && cells[neighbour] >= 0)
                    Assert.True(tiles.Allows(cells[cell], direction, cells[neighbour]),
                        $"Cells {grid.Coordinates(cell)} and {grid.Coordinates(neighbour)} do not match.");
            }
        }
    }
}
