namespace Godoodads.Tests.WfcGenerator;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class WfcDemoAssetTests
{
    private const string TileSetPath = "res://demo/wfc_generator/assets/ruin_tileset.tres";
    private const string PathTileSetPath = "res://demo/wfc_generator/assets/ruin_paths_tileset.tres";

    [TestCase]
    [RequireGodotRuntime]
    public void Ruin_tile_set_is_valid_and_solves_on_a_ground_layer()
    {
        var tileSet = ResourceLoader.Load<WfcTileSet>(TileSetPath);
        var grid = GridTopology.Create3D(new Vector3I(16, 8, 16));
        var binding = TileSetBinding.Create(tileSet, grid.ActiveDirections);
        AssertThat(binding.IsValid).IsTrue();

        int rock = binding.Compiled.FindVariant(binding.TileForItem(tileSet.Library.FindItemByName("Rock")), 0);
        var initial = Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray();
        for (int z = 0; z < grid.Size.Z; z++)
            for (int x = 0; x < grid.Size.X; x++)
                initial[grid.Index(new Vector3I(x, 0, z))] = rock;

        var result = new WfcSolver(grid, binding.Compiled, initial).Solve(new SolveOptions { Seed = 1 });
        AssertThat((int)result.Status).IsEqual((int)SolveStatus.Solved);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ruin_tile_sets_have_no_check_warnings()
    {
        foreach (string path in new[] { TileSetPath, PathTileSetPath })
        {
            var binding = TileSetBinding.Create(ResourceLoader.Load<WfcTileSet>(path), Directions.All3D);
            AssertThat(binding.IsValid).IsTrue();
            AssertThat(string.Join("; ", binding.Warnings())).IsEqual("");
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Demo_scenes_store_their_settings()
    {
        foreach (string name in new[] { "hillside_free", "hillside_ground" })
        {
            var root = ResourceLoader.Load<PackedScene>($"res://demo/wfc_generator/{name}.tscn").Instantiate();
            var map = root.GetNode<GridMap>("GridMap");
            var settings = map.GetMeta(WfcSettings.MetaKey).As<WfcSettings>();
            AssertThat(settings.TileSet).IsNotNull();
            AssertThat(map.MeshLibrary == settings.TileSet.Library).IsTrue();
            AssertThat(map.GetUsedCells().Count).IsEqual(0);
            AssertThat(settings.BoundaryNegY).IsEqual(name == "hillside_ground" ? "rock" : "");
            root.Free();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ruin_tile_set_solves_on_a_rock_boundary_and_places_tower_lights()
    {
        var tileSet = ResourceLoader.Load<WfcTileSet>(TileSetPath);
        var grid = GridTopology.Create3D(new Vector3I(24, 10, 24));
        var binding = TileSetBinding.Create(tileSet, grid.ActiveDirections);
        var boundary = new Socket?[Directions.Count];
        Socket.TryParse("rock", true, out var rock, out _);
        boundary[Directions.NegY] = rock;

        var cells = GridMapCells.AllFree(grid);
        var result = new WfcSolver(grid, binding.Compiled, cells.Initial).Solve(new SolveOptions { Seed = 2, Boundary = boundary });
        AssertThat((int)result.Status).IsEqual((int)SolveStatus.Solved);

        int towerTop = binding.TileForItem(tileSet.Library.FindItemByName("Tower Top"));
        int towers = result.Cells.Count(v => binding.Compiled.Variants[v].TileIndex == towerTop);
        var map = AutoFree(new GridMap { MeshLibrary = tileSet.Library });
        var plan = GridMapMarkers.Plan(map, Vector3I.Zero, grid, binding, cells.Initial, result.Cells);
        AssertThat(plan.Count).IsEqual(towers);
        AssertThat(plan.All(p => p.Scene.ResourcePath == "res://demo/wfc_generator/assets/tower_light.tscn")).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Paths_scene_joins_its_two_ends_for_every_seed_and_sometimes_climbs()
    {
        var root = ResourceLoader.Load<PackedScene>("res://demo/wfc_generator/hillside_paths.tscn").Instantiate();
        var map = root.GetNode<GridMap>("GridMap");
        var settings = map.GetMeta(WfcSettings.MetaKey).As<WfcSettings>();
        AssertThat(settings.TileSet.ResourcePath).IsEqual(PathTileSetPath);
        AssertThat(settings.PathSocket).IsEqual("path");
        AssertThat(map.GetUsedCells().Count).IsEqual(2);

        var grid = GridTopology.Create3D(settings.BoundsSize);
        var binding = TileSetBinding.Create(settings.TileSet, grid.ActiveDirections);
        var cells = GridMapCells.ReadFixed(map, settings.BoundsOrigin, grid, binding);
        var errors = new System.Collections.Generic.List<string>();
        var boundary = settings.ParseBoundary(errors);
        int stairs = binding.TileForItem(settings.TileSet.Library.FindItemByName("Path Stairs"));
        int solved = 0, climbing = 0;
        for (long seed = 1; seed <= 20; seed++)
        {
            var job = new SolveJob(grid, binding.Compiled, cells.Initial, new SolveOptions { Seed = seed, Boundary = boundary },
                paths: new PathRequest(settings.PathSocket, settings.PathWander));
            job.Start();
            AssertThat(job.Wait(20000)).IsTrue();
            AssertThat(job.Plan.IsValid).IsTrue();
            if (job.Result.Status != SolveStatus.Solved)
                continue;
            solved++;
            if (job.Result.Cells.Any(v => binding.Compiled.Variants[v].TileIndex == stairs))
                climbing++;
        }
        root.Free();
        AssertThat(solved).IsEqual(20);
        AssertThat(climbing).IsEqual(20);   // the ends are two layers apart
    }
}
