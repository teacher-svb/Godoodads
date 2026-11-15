namespace Godoodads.Tests.Wave;

using System.Collections.Generic;
using Godot;
using GdUnit4;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using static GdUnit4.Assertions;

/// <summary>
/// WFC Generator 1.3.3 moved its resources to editor_tools_common/Wfc and left empty legacy
/// subclasses at the old paths (brief godoodungeon §4.1). The fixtures are the 1.3.2 demo files,
/// which name the scripts by their old paths only, as every file ResourceSaver writes outside the
/// editor does.
/// </summary>
[TestSuite]
public class WfcSharedSolverMoveTests
{
    private const string Fixtures = "res://tests/godoowave/fixtures/wfc_1_3_2";

    // FNV-1a over (item, quarter turns) of every cell, computed with the WFC Generator 1.3.2 code
    // for the hillside_ground settings (32×12×32, seed 2, rock bottom boundary).
    private const ulong HillsideGroundHash132 = 17592921742900000595UL;

    [TestCase]
    [RequireGodotRuntime]
    public void Tile_sets_saved_with_old_script_paths_load_through_the_legacy_classes()
    {
        var tileSet = ResourceLoader.Load<WfcTileSet>($"{Fixtures}/ruin_tileset.tres", null, ResourceLoader.CacheMode.Ignore);
        AssertThat(tileSet).IsNotNull();
        AssertThat(tileSet.GetType().FullName).IsEqual("Godoodads.Wave.Legacy.WfcTileSet");
        AssertThat(tileSet.Tiles.Count).IsEqual(24);
        AssertThat(tileSet.Tiles[0].GetType().FullName).IsEqual("Godoodads.Wave.Legacy.WfcTile");

        var binding = TileSetBinding.Create(tileSet, Directions.All3D);
        AssertThat(binding.IsValid).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_scene_saved_with_1_3_2_generates_the_same_cells()
    {
        var root = ResourceLoader.Load<PackedScene>($"{Fixtures}/hillside_ground.tscn", null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var settings = WfcSettings.StoredOn(root.GetNode<GridMap>("GridMap"));
        AssertThat(settings).IsNotNull();
        AssertThat(settings.GetType().FullName).IsEqual("Godoodads.Wave.Legacy.WfcSettings");

        var grid = GridTopology.Create3D(settings.BoundsSize);
        var binding = TileSetBinding.Create(settings.TileSet, grid.ActiveDirections);
        var errors = new List<string>();
        var boundary = settings.ParseBoundary(errors);
        AssertThat(errors.Count).IsEqual(0);
        var result = new WfcSolver(grid, binding.Compiled, GridMapCells.AllFree(grid).Initial).Solve(new SolveOptions
        {
            Seed = settings.Seed, MaxBacktracks = settings.MaxBacktracks, MaxRestarts = settings.MaxRestarts, Boundary = boundary,
        });
        root.Free();

        AssertThat((int)result.Status).IsEqual((int)SolveStatus.Solved);
        AssertThat(Hash(binding, result.Cells)).IsEqual(HillsideGroundHash132);
    }

    private static ulong Hash(TileSetBinding binding, int[] cells)
    {
        ulong hash = 14695981039346656037UL;
        foreach (int cell in cells)
        {
            var variant = binding.Compiled.Variants[cell];
            foreach (int value in new[] { binding.Items[variant.TileIndex], variant.QuarterTurns })
            {
                hash ^= (uint)value;
                hash *= 1099511628211UL;
            }
        }
        return hash;
    }
}
