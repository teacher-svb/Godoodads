using System;
using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class OrientationTests
{
    private const float OneDegreeCos = 0.99985f;

    private static bool IsValid(Transform3D t) =>
        !float.IsNaN(t.Basis.X.X + t.Basis.X.Y + t.Basis.X.Z + t.Basis.Y.X + t.Basis.Y.Y + t.Basis.Y.Z + t.Basis.Z.X + t.Basis.Z.Y + t.Basis.Z.Z)
        && t.Basis.Orthonormalized().IsEqualApprox(t.Basis);

    [Fact]
    public void Slerp_direction_hits_both_ends_and_survives_opposites()
    {
        var a = Vector3.Up;
        var b = new Vector3(1, 0, 1).Normalized();
        Assert.True(OrientationMath.SlerpDirection(a, b, 0).IsEqualApprox(a));
        Assert.True(OrientationMath.SlerpDirection(a, b, 1).IsEqualApprox(b));
        Assert.Equal(45f, Mathf.RadToDeg(OrientationMath.SlerpDirection(a, b, 0.5f).AngleTo(a)), 2);

        foreach (var from in new[] { Vector3.Up, Vector3.Right, new Vector3(1, 2, 3).Normalized() })
        {
            var mid = OrientationMath.SlerpDirection(from, -from, 0.5f);
            Assert.False(float.IsNaN(mid.X) || float.IsNaN(mid.Y) || float.IsNaN(mid.Z));
            Assert.Equal(1f, mid.Length(), 4);
            Assert.Equal(0f, mid.Dot(from), 3);
        }
    }

    // Brief §8: alignment 0 on a sphere puts every up axis within 1 degree of the radial direction.
    [Fact]
    public void Alignment_zero_on_a_sphere_follows_the_radial_direction()
    {
        var o = new ScatterOptions { Amount = 2000, Alignment = 0, NormalSource = NormalSource.Smooth, RandomScale = 0 };
        var result = ScatterPipeline.Run(TestMeshes.Sphere(), o);
        Assert.Equal(2000, result.Placed);
        foreach (var t in result.Transforms)
        {
            Assert.True(IsValid(t));
            Assert.True(t.Basis.Y.Normalized().Dot(t.Origin.Normalized()) > OneDegreeCos, $"up {t.Basis.Y} at {t.Origin}");
        }
    }

    // Brief §8: alignment 1 puts every up axis within 1 degree of V, for several V including -Y.
    [Fact]
    public void Alignment_one_follows_the_target_on_a_sphere()
    {
        foreach (var v in new[] { Vector3.Up, Vector3.Down, Vector3.Right, new Vector3(-1, 2, 0.5f).Normalized() })
        {
            var o = new ScatterOptions { Amount = 500, Alignment = 1, TargetDirection = v, NormalSource = NormalSource.Smooth, RandomScale = 0 };
            foreach (var t in ScatterPipeline.Run(TestMeshes.Sphere(), o).Transforms)
                Assert.True(t.Basis.Y.Normalized().Dot(v) > OneDegreeCos, $"V {v}: up {t.Basis.Y}");
        }
    }

    // Brief §8: N = -V with alignment 0.5 must give valid, orthonormal transforms.
    [Fact]
    public void Opposite_normal_and_target_give_valid_transforms()
    {
        var o = new ScatterOptions { Amount = 300, Alignment = 0.5f, TargetDirection = Vector3.Down, RandomScale = 0, RandomTiltDegrees = 10 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms)
        {
            Assert.True(IsValid(t), t.Basis.ToString());
            Assert.Equal(90f, Mathf.RadToDeg(t.Basis.Y.AngleTo(Vector3.Up)), 0.1f + 10f); // halfway, up to 10° tilt
        }
        var flat = new ScatterOptions { Amount = 50, Alignment = 0.5f, TargetDirection = Vector3.Down, RandomScale = 0, RandomTiltDegrees = 0 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), flat).Transforms)
            Assert.Equal(0f, t.Basis.Y.Dot(Vector3.Up), 4);
    }

    [Fact]
    public void Face_normals_on_a_cube_point_along_the_axes()
    {
        var o = new ScatterOptions { Amount = 600, Alignment = 0, NormalSource = NormalSource.Face, RandomScale = 0 };
        var axes = new[] { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Cube(), o).Transforms)
        {
            var up = t.Basis.Y.Normalized();
            Assert.Contains(axes, a => up.Dot(a) > OneDegreeCos);
            // The axis matches the face the instance sits on.
            var face = axes.OrderByDescending(a => t.Origin.Dot(a)).First();
            Assert.True(up.Dot(face) > OneDegreeCos, $"origin {t.Origin}, up {up}");
        }
    }

    [Fact]
    public void Smooth_falls_back_to_face_normals_without_vertex_normals()
    {
        var o = new ScatterOptions { Amount = 100, Alignment = 0, NormalSource = NormalSource.Smooth, RandomScale = 0 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Cube(), o).Transforms)
            Assert.True(IsValid(t));
    }

    [Theory]
    [InlineData(MeshAxis.PositiveX)]
    [InlineData(MeshAxis.NegativeX)]
    [InlineData(MeshAxis.PositiveY)]
    [InlineData(MeshAxis.NegativeY)]
    [InlineData(MeshAxis.PositiveZ)]
    [InlineData(MeshAxis.NegativeZ)]
    public void Mesh_up_axis_is_mapped_onto_the_instance_up(MeshAxis axis)
    {
        var v = new Vector3(1, 1, -1).Normalized();
        var o = new ScatterOptions { Amount = 50, Alignment = 1, TargetDirection = v, MeshUpAxis = axis, RandomScale = 0 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms)
        {
            Assert.True(IsValid(t));
            Assert.True((t.Basis * axis.ToVector()).Dot(v) > OneDegreeCos, $"{axis}: {t.Basis * axis.ToVector()}");
        }
    }

    [Fact]
    public void Alignment_randomness_spreads_within_its_range()
    {
        // N = +Y, V = +X: the angle from N is 90° times the per-instance alignment.
        var o = new ScatterOptions { Amount = 2000, Alignment = 0.5f, AlignmentRandomness = 0.25f, TargetDirection = Vector3.Right, RandomScale = 0 };
        var angles = ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms.Select(t => Mathf.RadToDeg(t.Basis.Y.AngleTo(Vector3.Up))).ToArray();
        Assert.True(angles.Min() >= 22.5f - 0.1f && angles.Min() < 25f, $"min {angles.Min()}");
        Assert.True(angles.Max() <= 67.5f + 0.1f && angles.Max() > 65f, $"max {angles.Max()}");
    }

    [Fact]
    public void Slope_filter_keeps_only_the_top_of_a_cube()
    {
        var o = new ScatterOptions { Amount = 500, SlopeMinDegrees = 0, SlopeMaxDegrees = 45 };
        var result = ScatterPipeline.Run(TestMeshes.Cube(), o);
        Assert.Equal(500, result.Placed);
        Assert.Equal(1f / 6, result.SlopeAreaFraction, 4);
        Assert.All(result.Transforms, t => Assert.Equal(1f, t.Origin.Y, 4));
    }

    [Fact]
    public void Slope_filter_uses_its_reference_and_full_range_is_a_no_op()
    {
        var down = new ScatterOptions { Amount = 100, SlopeMaxDegrees = 10, SlopeReference = Vector3.Down };
        Assert.All(ScatterPipeline.Run(TestMeshes.Cube(), down).Transforms, t => Assert.Equal(-1f, t.Origin.Y, 4));

        var tilted = SurfaceData.Build(new[] { TestMeshes.Quad(2) },
            new Transform3D(Basis.Identity.Rotated(Vector3.Right, Mathf.DegToRad(30)), Vector3.Zero));
        Assert.Equal(0, ScatterPipeline.Run(tilted, new ScatterOptions { Amount = 10, SlopeMaxDegrees = 20 }).Placed);
        Assert.Equal(10, ScatterPipeline.Run(tilted, new ScatterOptions { Amount = 10, SlopeMaxDegrees = 40 }).Placed);
        Assert.Equal(10, ScatterPipeline.Run(tilted, new ScatterOptions { Amount = 10, SlopeMinDegrees = 25, SlopeMaxDegrees = 35 }).Placed);

        Assert.Null(OrientationMath.SlopeWeights(TestMeshes.Cube(), Vector3.Up, 0, 180));
        Assert.Equal(1f, ScatterPipeline.Run(TestMeshes.Cube(), new ScatterOptions { Amount = 10 }).SlopeAreaFraction);
    }
}
