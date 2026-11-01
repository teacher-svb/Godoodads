using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class ScatterPipelineTests
{
    private sealed class RejectHalfX : ICandidateFilter
    {
        public string Name => "half";
        public bool Accept(SurfaceData surface, in SurfacePoint point, Pcg32 rng) => point.Position.X > 0;
    }

    [Fact]
    public void Same_seed_and_settings_give_identical_transforms()
    {
        var surface = TestMeshes.Plane();
        var o = new ScatterOptions { Amount = 300, Seed = 4821, MinDistance = 0.2f, RandomTiltDegrees = 15 };
        var a = ScatterPipeline.Run(surface, o).Transforms;
        var b = ScatterPipeline.Run(surface, o).Transforms;
        Assert.Equal(a, b);
        var c = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 300, Seed = 4822, MinDistance = 0.2f }).Transforms;
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void Seed_salt_changes_positions_but_stays_deterministic()
    {
        var surface = TestMeshes.Plane();
        var plain = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 50, Seed = 7 }).Transforms;
        var zeroSalt = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 50, Seed = 7, SeedSalt = 0 }).Transforms;
        var salted = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 50, Seed = 7, SeedSalt = 50 }).Transforms;
        var saltedAgain = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 50, Seed = 7, SeedSalt = 50 }).Transforms;
        Assert.Equal(plain, zeroSalt);
        Assert.Equal(salted, saltedAgain);
        Assert.Empty(plain.Select(t => t.Origin).Intersect(salted.Select(t => t.Origin)));
    }

    [Fact]
    public void Places_the_requested_amount_on_the_surface()
    {
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 500 });
        Assert.Equal(500, result.Placed);
        Assert.Equal(500, result.Attempts);
        Assert.All(result.Transforms, t =>
        {
            Assert.Equal(0f, t.Origin.Y, 5);
            Assert.InRange(t.Origin.X, -5f, 5f);
            Assert.InRange(t.Origin.Z, -5f, 5f);
        });
    }

    [Fact]
    public void Spacing_keeps_min_distance_between_instances()
    {
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 200, MinDistance = 0.4f });
        Assert.Equal(200, result.Placed);
        var p = result.Transforms.Select(t => t.Origin).ToArray();
        for (int i = 0; i < p.Length; i++)
            for (int j = i + 1; j < p.Length; j++)
                Assert.True(p[i].DistanceTo(p[j]) >= 0.4f);
    }

    [Fact]
    public void Filters_are_counted_and_retried()
    {
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 400 }, new[] { new RejectHalfX() });
        Assert.Equal(400, result.Placed);
        Assert.True(result.Rejected["half"] > 300);
        Assert.Equal(result.Attempts, result.Placed + result.Rejected["half"]);
        Assert.All(result.Transforms, t => Assert.True(t.Origin.X > 0));
    }

    [Fact]
    public void Without_randomisation_instances_are_upright_and_scaled()
    {
        var o = new ScatterOptions { Amount = 20, BaseScale = 2, RandomScale = 0, RandomYawDegrees = 0, RandomTiltDegrees = 0 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms)
            Assert.True(t.Basis.IsEqualApprox(Basis.FromScale(new Vector3(2, 2, 2))), t.Basis.ToString());
    }

    [Fact]
    public void Up_axis_follows_target_direction_including_opposite()
    {
        foreach (var dir in new[] { Vector3.Down, new Vector3(1, 1, 0).Normalized(), Vector3.Back })
        {
            var o = new ScatterOptions { Amount = 50, TargetDirection = dir, RandomTiltDegrees = 0, RandomScale = 0 };
            foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms)
            {
                var up = t.Basis.Y.Normalized();
                Assert.True(up.Dot(dir) > 0.9998f, $"dir {dir}: up {up}"); // within about 1 degree
                Assert.True(t.Basis.Orthonormalized().IsEqualApprox(t.Basis), "basis must stay orthonormal");
            }
        }
    }

    [Fact]
    public void Tilt_stays_within_its_limit()
    {
        var o = new ScatterOptions { Amount = 500, RandomTiltDegrees = 20, RandomScale = 0 };
        foreach (var t in ScatterPipeline.Run(TestMeshes.Plane(), o).Transforms)
            Assert.True(Mathf.RadToDeg(t.Basis.Y.AngleTo(Vector3.Up)) <= 20.01f);
    }
}
