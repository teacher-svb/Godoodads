namespace Godoodads.Tests.MultiMeshScatter;

using System.Diagnostics;
using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;
using static GdUnit4.Assertions;

/// <summary>
/// Brief §8: 50,000 instances on a 100,000-triangle mesh in under 3 s without minimum distance and
/// under 6 s with it. Measures the full editor path: surface extraction, pipeline, local transforms
/// and the MultiMesh buffer write.
/// </summary>
[TestSuite]
public class PerformanceTests
{
    [TestCase(0f, 3000, TestName = "Without_min_distance")]
    [TestCase(0.05f, 6000, TestName = "With_min_distance")]
    [RequireGodotRuntime]
    public void Fifty_thousand_instances_on_a_hundred_thousand_triangles(float minDistance, long budgetMs)
    {
        var mesh = new PlaneMesh { Size = new Vector2(50, 50), SubdivideWidth = 223, SubdivideDepth = 223 };
        var target = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = new BoxMesh() };

        var clock = Stopwatch.StartNew();
        var surface = SurfaceSource.FromMesh(mesh, Transform3D.Identity);
        long extracted = clock.ElapsedMilliseconds;
        var result = ScatterPipeline.Run(surface, new ScatterOptions
        {
            Amount = 50_000, MinDistance = minDistance, NormalSource = NormalSource.Smooth, Alignment = 0.5f, RandomTiltDegrees = 10,
        });
        long scattered = clock.ElapsedMilliseconds;
        var toLocal = Transform3D.Identity;
        var written = MultiMeshWriter.Build(target, result.Transforms.Select(t => toLocal * t).ToArray(), append: false);
        clock.Stop();

        GD.Print($"PERF min distance {minDistance}: {surface.TriangleCount} triangles, {result.Placed} placed from {result.Candidates} candidates; "
            + $"extract {extracted} ms, pipeline {scattered - extracted} ms, write {clock.ElapsedMilliseconds - scattered} ms, total {clock.ElapsedMilliseconds} ms");
        AssertThat(surface.TriangleCount).IsGreaterEqual(100_000);
        AssertThat(written.InstanceCount).IsEqual(50_000);
        AssertThat(clock.ElapsedMilliseconds).IsLess(budgetMs);
    }
}
