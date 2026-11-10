namespace Godoodads.Tests.WfcGenerator;

using System.Linq;
using Godot;
using Godot.Collections;
using GdUnit4;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;
using static GdUnit4.Assertions;

// Brief §13.3: guided paths on a GridMap, with regenerate and pinning.
[TestSuite]
public class WfcPathTests
{
    private const string P = "path";

    private static WfcTile Tile(int item, string px, string nx, string pz, string nz, TileRotation rotation = TileRotation.FourWay) => new()
    {
        Item = item, Rotation = rotation, SocketPosX = px, SocketNegX = nx, SocketPosY = "v", SocketNegY = "v", SocketPosZ = pz, SocketNegZ = nz,
    };

    private static TileSetBinding Binding()
    {
        var library = new MeshLibrary();
        for (int i = 0; i < 5; i++)
        {
            library.CreateItem(i);
            library.SetItemMesh(i, new BoxMesh());
        }
        var tiles = new Array<WfcTile>(new[]
        {
            Tile(0, "g", "g", "g", "g", TileRotation.None),
            Tile(1, P, P, "g", "g"),
            Tile(2, P, "g", P, "g"),
            Tile(3, P, P, P, "g"),
            Tile(4, P, "g", "g", "g"),
        });
        return TileSetBinding.Create(new WfcTileSet { Library = library, Tiles = tiles }, Directions.All3D);
    }

    private static void Place(GridMap map, TileSetBinding binding, Vector3I position, int direction)
    {
        var faces = PathRouter.PathFaces(binding.Compiled, P);
        var variant = binding.Compiled.Variants[System.Array.FindIndex(faces, f => f == 1 << direction)];
        map.SetCellItem(position, binding.Items[variant.TileIndex], GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
    }

    /// <summary>One Generate as the plugin runs it: read, lay out paths, solve, write cells and record.</summary>
    private static PathPlan Generate(GridMap map, TileSetBinding binding, Vector3I origin, GridTopology grid, long seed, double wander)
    {
        var record = GridMapCells.ReadRecord(map);
        var cells = GridMapCells.ReadFixed(map, origin, grid, binding, record);
        var job = new SolveJob(grid, binding.Compiled, cells.Initial, new SolveOptions { Seed = seed }, paths: new PathRequest(P, wander, origin));
        job.Start();
        AssertThat(job.Wait(10000)).IsTrue();
        if (!job.Plan.IsValid)
            return job.Plan;
        AssertThat(job.Result.Status).IsEqual(SolveStatus.Solved);
        GridMapCells.Apply(map, GridMapCells.Diff(map, origin, grid, binding, cells.Initial, job.Result.Cells).Do);
        GridMapCells.WriteRecord(map, GridMapCells.RecordAfter(record, origin, grid, binding, cells.Initial, job.Result.Cells).Encode());
        return job.Plan;
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Path_settings_round_trip()
    {
        const string path = "user://wfc_path_settings.tres";
        var settings = new WfcSettings { PathSocket = "road", PathWander = 0.75f };
        AssertThat(ResourceSaver.Save(settings, path)).IsEqual(Error.Ok);
        var loaded = ResourceLoader.Load<WfcSettings>(path, null, ResourceLoader.CacheMode.Ignore);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        AssertThat(loaded.PathSocket).IsEqual("road");
        AssertThat(loaded.PathWander).IsEqual(0.75f);
        AssertThat(loaded.HasPaths).IsTrue();
        AssertThat(new WfcSettings().HasPaths).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Regenerate_routes_again_and_pinned_path_cells_are_kept()
    {
        var binding = Binding();
        var map = AutoFree(new GridMap());
        var origin = new Vector3I(10, 0, -4);
        var grid = GridTopology.Create3D(new Vector3I(10, 1, 10));
        Place(map, binding, origin + new Vector3I(0, 0, 2), Directions.PosX);
        Place(map, binding, origin + new Vector3I(9, 0, 7), Directions.NegX);

        var first = Generate(map, binding, origin, grid, 1, 1);
        AssertThat(first.RouteCount).IsEqual(1);
        AssertThat(first.PlacedPathCells).IsEqual(2);

        // The route cells were generated, so a regenerate frees them and routes again.
        var second = Generate(map, binding, origin, grid, 2, 1);
        AssertThat(second.RouteCount).IsEqual(1);
        AssertThat(second.PlacedPathCells).IsEqual(2);
        AssertThat(string.Join(",", second.RouteFaces)).IsNotEqual(string.Join(",", first.RouteFaces));

        // Pinned, the whole path is placed: one closed group with nothing left to route.
        var (pinned, changed) = GridMapCells.Pin(map, origin, grid.Size, true);
        AssertThat(changed).IsGreater(0);
        GridMapCells.WriteRecord(map, pinned.Encode());
        var third = Generate(map, binding, origin, grid, 3, 1);
        AssertThat(third.IsValid).IsTrue();
        AssertThat(third.RouteCount).IsEqual(0);
        AssertThat(third.PlacedPathCells).IsEqual(second.RouteCellCount + 2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Errors_name_GridMap_coordinates()
    {
        var binding = Binding();
        var map = AutoFree(new GridMap());
        var origin = new Vector3I(10, 0, -4);
        var grid = GridTopology.Create3D(new Vector3I(6, 1, 6));
        Place(map, binding, origin + new Vector3I(2, 0, 2), Directions.PosX);
        var plan = Generate(map, binding, origin, grid, 1, 0.3);
        AssertThat(plan.IsValid).IsFalse();
        AssertThat(plan.Errors[0].Message).Contains("(12, 0, -2)");
        AssertThat(plan.Errors[0].Cells[0]).IsEqual(new Vector3I(12, 0, -2));
    }
}
