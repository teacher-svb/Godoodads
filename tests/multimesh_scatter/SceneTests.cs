namespace Godoodads.Tests.MultiMeshScatter;

using System.Linq;
using System.Threading.Tasks;
using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;
using static GdUnit4.Assertions;

/// <summary>Tests that need nodes inside a running scene tree.</summary>
[TestSuite]
public class SceneTests
{
    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [TestCase]
    [RequireGodotRuntime]
    public async Task Csg_root_shapes_are_baked_and_children_rejected()
    {
        var root = AutoFree(new CsgCombiner3D());
        var box = new CsgBox3D { Size = new Vector3(2, 2, 2) };
        root.AddChild(box);
        Tree.Root.AddChild(root);
        // CSG meshes update in a deferred call after entering the tree; give it a few frames.
        SurfaceData data = null;
        string error = null;
        for (int frame = 0; frame < 5 && data == null; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
            data = SurfaceSource.FromNode(root, out error);
        }
        AssertThat(data).OverrideFailureMessage($"CSG bake failed: {error}").IsNotNull();
        AssertThat(data.TotalArea).IsEqualApprox(24f, 1e-2f);

        AssertThat(SurfaceSource.FromNode(box, out error)).IsNull();
        AssertThat(error).Contains("root CSG");
        Tree.Root.RemoveChild(root);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task Demo_scene_terrain_end_to_end()
    {
        var scene = AutoFree(GD.Load<PackedScene>("res://demo/multimesh_scatter/demo.tscn").Instantiate<Node3D>());
        Tree.Root.AddChild(scene);
        await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

        var terrain = scene.GetNode<MeshInstance3D>("Terrain");
        var target = scene.GetNode<MultiMeshInstance3D>("Scatter");
        var surface = SurfaceSource.FromNode(terrain, out _);
        var result = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 1000, Seed = 4821, MinDistance = 0.2f });
        var toLocal = target.GlobalTransform.AffineInverse();
        var mm = MultiMeshWriter.Build(target.Multimesh, result.Transforms.Select(t => toLocal * t).ToArray(), append: false);

        AssertThat(result.Placed).IsEqual(1000);
        AssertThat(mm.InstanceCount).IsEqual(1000);
        AssertThat(mm.Mesh).IsSame(target.Multimesh.Mesh);
        target.Multimesh = mm;
        var positions = MultiMeshWriter.WorldPositions(target);
        AssertThat(positions.Count).IsEqual(1000);
        AssertThat(positions.All(p => Mathf.Abs(p.Y) < 1e-4f && Mathf.Abs(p.X) <= 10f && Mathf.Abs(p.Z) <= 10f)).IsTrue();
        Tree.Root.RemoveChild(scene);
    }
}
