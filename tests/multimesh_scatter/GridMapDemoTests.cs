namespace Godoodads.Tests.MultiMeshScatter;

using System.Linq;
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

// Brief §10.3: the GridMap demo scatters grass on open tops and ramps only.
[TestSuite]
public class GridMapDemoTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Grass_grows_on_open_tops_and_ramps_but_not_inside_blocks_or_on_paving()
    {
        var root = AutoFree(GD.Load<PackedScene>("res://demo/multimesh_scatter/demo_gridmap.tscn").Instantiate<Node3D>());
        var map = root.GetNode<GridMap>("Tiles");
        var scatter = root.GetNode<MultiMeshInstance3D>("Scatter");
        var library = map.MeshLibrary;
        int ramp = library.FindItemByName("Ramp"), paving = library.FindItemByName("Paving");

        // Headless runs have no rendering server data, so read the buffer (row-major 3 x 4 per instance).
        var buffer = scatter.Multimesh.Buffer;
        int count = scatter.Multimesh.InstanceCount;
        AssertThat(count).IsEqual(6000);
        var positions = Enumerable.Range(0, count)
            .Select(i => new Vector3(buffer[i * 12 + 3], buffer[i * 12 + 7], buffer[i * 12 + 11]))
            .Select(p => map.Transform.AffineInverse() * (scatter.Transform * p)).ToArray();

        int onRamps = 0;
        foreach (var p in positions)
        {
            int above = map.GetCellItem(map.LocalToMap(p + Vector3.Up * 0.01f));
            int below = map.GetCellItem(map.LocalToMap(p - Vector3.Up * 0.01f));
            AssertThat(above == GridMap.InvalidCellItem || above == ramp).OverrideFailureMessage($"Instance inside a block at {p}").IsTrue();
            AssertThat(below).OverrideFailureMessage($"Instance on paving at {p}").IsNotEqual(paving);
            if (below == ramp)
                onRamps++;
        }
        AssertThat(onRamps).IsGreater(0);
    }
}
