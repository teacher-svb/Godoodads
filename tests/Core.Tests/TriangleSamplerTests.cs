using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class TriangleSamplerTests
{
    // Two triangles with areas 1 and 3.
    private static SurfaceData Uneven() => SurfaceData.Build(new[]
    {
        new SurfaceArrays
        {
            Vertices = new[]
            {
                new Vector3(0, 0, 0), new Vector3(0, 0, 2), new Vector3(1, 0, 0),
                new Vector3(5, 0, 0), new Vector3(5, 0, 3), new Vector3(7, 0, 0),
            },
        },
    }, Transform3D.Identity);

    [Fact]
    public void Area_weighted_sampling_is_proportional_to_area()
    {
        var data = Uneven();
        Assert.Equal(1f, data.Areas[0], 4);
        Assert.Equal(3f, data.Areas[1], 4);
        var sampler = new TriangleSampler(data, SamplerMode.AreaWeighted);
        var rng = new Pcg32(1);
        int first = 0;
        const int n = 40_000;
        for (int i = 0; i < n; i++)
            if (sampler.Sample(rng).Triangle == 0)
                first++;
        Assert.InRange(first / (double)n, 0.24, 0.26);
    }

    [Fact]
    public void Uniform_sampling_ignores_area()
    {
        var sampler = new TriangleSampler(Uneven(), SamplerMode.UniformPerTriangle);
        var rng = new Pcg32(2);
        int first = 0;
        const int n = 40_000;
        for (int i = 0; i < n; i++)
            if (sampler.Sample(rng).Triangle == 0)
                first++;
        Assert.InRange(first / (double)n, 0.49, 0.51);
    }

    [Fact]
    public void Points_lie_inside_their_triangle()
    {
        var data = Uneven();
        var sampler = new TriangleSampler(data, SamplerMode.AreaWeighted);
        var rng = new Pcg32(3);
        for (int i = 0; i < 5000; i++)
        {
            var p = sampler.Sample(rng);
            Assert.InRange(p.Barycentric.X, 0f, 1f);
            Assert.InRange(p.Barycentric.Y, 0f, 1f);
            Assert.InRange(p.Barycentric.Z, 0f, 1f);
            Assert.Equal(1f, p.Barycentric.X + p.Barycentric.Y + p.Barycentric.Z, 4);
            Assert.True(p.Position.IsEqualApprox(data.PositionAt(p.Triangle, p.Barycentric)));
        }
    }

    [Fact]
    public void Zero_weight_and_degenerate_triangles_are_never_picked()
    {
        var data = SurfaceData.Build(new[]
        {
            new SurfaceArrays
            {
                Vertices = new[]
                {
                    new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0),
                    new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0), // degenerate
                    new Vector3(5, 0, 0), new Vector3(5, 0, 1), new Vector3(6, 0, 0),
                },
            },
        }, Transform3D.Identity);
        var sampler = new TriangleSampler(data, SamplerMode.UniformPerTriangle, new[] { 0f, 1f, 1f });
        var rng = new Pcg32(4);
        for (int i = 0; i < 2000; i++)
            Assert.Equal(2, sampler.Sample(rng).Triangle);
    }
}
