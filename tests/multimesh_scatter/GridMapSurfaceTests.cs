namespace Godoodads.Tests.MultiMeshScatter;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;
using static GdUnit4.Assertions;

// Brief §10.1: a GridMap as the surface.
[TestSuite]
public class GridMapSurfaceTests
{
    private static readonly Vector3 CellSize = new(2, 2, 2);

    /// <summary>Item 0 is a block that fills its cell, item 1 a smaller "post" named Post.</summary>
    private static MeshLibrary Library()
    {
        var library = new MeshLibrary();
        library.CreateItem(0);
        library.SetItemName(0, "Block");
        library.SetItemMesh(0, new BoxMesh { Size = CellSize });
        library.CreateItem(1);
        library.SetItemName(1, "Post");
        library.SetItemMesh(1, new BoxMesh { Size = new Vector3(0.5f, 2, 0.5f) });
        return library;
    }

    private static GridMap Map(params (Vector3I Cell, int Item)[] cells)
    {
        var map = AutoFree(new GridMap { MeshLibrary = Library(), CellSize = CellSize });
        foreach (var (cell, item) in cells)
            map.SetCellItem(cell, item);
        return map;
    }

    private static SurfaceData Read(GridMap map, GridMapSelection selection, out GridMapSurfaceStats stats)
    {
        var data = SurfaceSource.FromGridMap(map, selection, false, out string error, out _, out stats);
        AssertThat(error).IsNull();
        return data;
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Cells_are_placed_as_GridMap_renders_them()
    {
        var library = Library();
        library.SetItemMeshTransform(0, new Transform3D(Basis.FromEuler(new Vector3(0.2f, 0.4f, 0)), new Vector3(0.1f, -0.2f, 0.3f)));
        foreach (var (centre, scale) in new[] { (true, 1f), (false, 1.5f) })
        {
            var map = AutoFree(new GridMap { MeshLibrary = library, CellSize = new Vector3(2, 1.5f, 3), CellCenterX = centre, CellCenterZ = centre, CellScale = scale });
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(map);   // GlobalTransform needs the tree
            map.Transform = new Transform3D(Basis.FromEuler(new Vector3(0, 0.7f, 0)), new Vector3(5, 1, -2));
            // Far apart, so no face is covered.
            var cells = new[] { (new Vector3I(0, 0, 0), 0), (new Vector3I(5, 3, -6), 16), (new Vector3I(-7, -2, 4), 7) };
            foreach (var (cell, orientation) in cells)
                map.SetCellItem(cell, 0, orientation);

            var data = Read(map, null, out var stats);
            AssertThat(stats.Cells).IsEqual(3);
            AssertThat(stats.CoveredTriangles).IsEqual(0);

            var vertices = library.GetItemMesh(0).SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var meshes = map.GetMeshes();
            var expected = Enumerable.Range(0, meshes.Count / 2)
                .SelectMany(i => vertices.Select(v => map.Transform * (meshes[i * 2].AsTransform3D() * v))).ToArray();
            AssertThat(data.Positions.Length).IsEqual(expected.Length);
            float worst = data.Positions.Max(p => expected.Min(e => e.DistanceTo(p)));
            AssertThat(worst).IsLess(1e-3f);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Faces_between_occupied_cells_are_left_out()
    {
        // Two blocks side by side and one on top of the first: three shared faces, six triangles.
        var map = Map((new Vector3I(0, 0, 0), 0), (new Vector3I(1, 0, 0), 0), (new Vector3I(0, 1, 0), 0));
        var data = Read(map, null, out var stats);
        AssertThat(stats.CoveredTriangles).IsEqual(8);   // block 0: +X and +Y; block 1: -X; top block: -Y
        AssertThat(data.TriangleCount).IsEqual(3 * 12 - 8);
        AssertThat(data.TotalArea).IsEqualApprox(3 * 24f - 4 * 4f, 1e-3f);

        // Any item in the neighbouring cell covers the face: a post on a block hides the block's top,
        // and the post's bottom; its sides stay because they are inside its cell.
        var posts = Map((new Vector3I(0, 0, 0), 0), (new Vector3I(0, 1, 0), 1));
        Read(posts, null, out var postStats);
        AssertThat(postStats.CoveredTriangles).IsEqual(4);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_box_only_reads_its_cells_but_neighbours_outside_still_cover()
    {
        var map = Map((new Vector3I(0, 0, 0), 0), (new Vector3I(1, 0, 0), 0), (new Vector3I(5, 0, 0), 0));
        var data = Read(map, new GridMapSelection(BoxOrigin: new Vector3I(0, 0, 0), BoxSize: new Vector3I(1, 1, 1)), out var stats);
        AssertThat(stats.Cells).IsEqual(1);
        AssertThat(data.TriangleCount).IsEqual(10);
        AssertThat(data.Positions.All(p => p.X <= 2.001f)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Items_can_be_included_or_excluded_by_name()
    {
        var map = Map((new Vector3I(0, 0, 0), 0), (new Vector3I(3, 0, 0), 1), (new Vector3I(6, 0, 0), 1));
        Read(map, new GridMapSelection(new[] { "Post" }), out var included);
        AssertThat(included.Cells).IsEqual(2);
        Read(map, new GridMapSelection(new[] { "Post" }, Exclude: true), out var excluded);
        AssertThat(excluded.Cells).IsEqual(1);

        SurfaceSource.FromGridMap(map, new GridMapSelection(new[] { "Missing" }), false, out string error, out _, out _);
        AssertThat(error).Contains("No cells");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Each_cell_has_its_own_normal_basis_and_the_library_is_unchanged()
    {
        var map = Map();
        map.SetCellItem(new Vector3I(0, 0, 0), 0, 0);
        map.SetCellItem(new Vector3I(4, 0, 0), 0, 16);
        var before = map.MeshLibrary.GetItemMesh(0).SurfaceGetArrays(0);

        var data = SurfaceSource.FromGridMap(map, null, true, out _, out bool generated, out _);
        AssertThat(generated).IsFalse();   // BoxMesh has real tangents
        AssertThat(data.NormalBasisOf(0).IsEqualApprox(data.NormalBasisOf(data.TriangleCount - 1))).IsFalse();

        var after = map.MeshLibrary.GetItemMesh(0).SurfaceGetArrays(0);
        AssertThat(after[(int)Mesh.ArrayType.Vertex].AsVector3Array()).IsEqual(before[(int)Mesh.ArrayType.Vertex].AsVector3Array());
        AssertThat(map.MeshLibrary.GetItemMesh(0) is BoxMesh).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_GridMap_is_a_supported_surface()
    {
        var map = Map((new Vector3I(0, 0, 0), 0));
        AssertThat(SurfaceSource.IsSupported(map)).IsTrue();
        var data = SurfaceSource.FromNode(map, false, out string error, out _);
        AssertThat(error).IsNull();
        AssertThat(data.TriangleCount).IsEqual(12);
    }
}
