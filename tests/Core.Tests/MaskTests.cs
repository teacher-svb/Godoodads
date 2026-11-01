using System;
using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class MaskTests
{
    /// <summary>Checkerboard of <paramref name="cells"/>×<paramref name="cells"/> squares, white at (0, 0).</summary>
    private static MaskImage Checker(int size = 64, int cells = 8)
    {
        var values = new float[size * size];
        int cell = size / cells;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                values[y * size + x] = ((x / cell + y / cell) % 2 == 0) ? 1f : 0f;
        return new MaskImage(size, size, values);
    }

    private static MaskImage Flat(float value, int size = 4) =>
        new(size, size, Enumerable.Repeat(value, size * size).ToArray());

    // UV of a world position on TestMeshes.Quad(size): u = x / size + 0.5, v = z / size + 0.5.
    private static Vector2 UvOf(Vector3 p, float size) => new(p.X / size + 0.5f, p.Z / size + 0.5f);

    private static bool OnWhite(Vector2 uv, int cells = 8) =>
        ((int)MathF.Floor(uv.X * cells) + (int)MathF.Floor(uv.Y * cells)) % 2 == 0;

    [Fact]
    public void Bilinear_sampling_hits_pixel_centres_and_wraps()
    {
        var mask = new MaskImage(2, 1, new[] { 0f, 1f });
        Assert.Equal(0f, mask.SampleBilinear(new Vector2(0.25f, 0.5f)), 5);
        Assert.Equal(1f, mask.SampleBilinear(new Vector2(0.75f, 0.5f)), 5);
        Assert.Equal(0.5f, mask.SampleBilinear(new Vector2(0.5f, 0.5f)), 5);
        // u = 0 lies between the last pixel (wrapped) and the first.
        Assert.Equal(0.5f, mask.SampleBilinear(new Vector2(0f, 0.5f)), 5);
        Assert.Equal(mask.SampleBilinear(new Vector2(0.3f, 0.2f)), mask.SampleBilinear(new Vector2(1.3f, -0.8f)), 5);
    }

    // Brief §8: a checkerboard mask on a plane places 0 instances on black squares.
    [Fact]
    public void Threshold_checkerboard_places_nothing_on_black()
    {
        var filter = new MaskFilter(Checker(), MaskMode.Threshold, 0.5f);
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 2000 }, new[] { filter });
        Assert.Equal(2000, result.Placed);
        Assert.InRange(result.Rejected["mask"], 1500, 2600); // about half the attempts land on black
        Assert.All(result.Transforms, t => Assert.True(OnWhite(UvOf(t.Origin, 10)), $"on black at {t.Origin}"));
    }

    [Fact]
    public void Invert_swaps_the_checkerboard()
    {
        var filter = new MaskFilter(Checker(), MaskMode.Threshold, 0.5f, invert: true);
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 500 }, new[] { filter });
        Assert.All(result.Transforms, t => Assert.False(OnWhite(UvOf(t.Origin, 10))));
    }

    // Brief §8: a 50% grey mask in Density mode accepts 50% ± 3% of attempts over 10,000 samples.
    [Fact]
    public void Density_with_half_grey_accepts_half_the_attempts()
    {
        var filter = new MaskFilter(Flat(0.5f), MaskMode.Density);
        var surface = TestMeshes.Plane(10);
        var sampler = new TriangleSampler(surface, SamplerMode.AreaWeighted);
        var rng = new Pcg32(1);
        int accepted = 0;
        const int attempts = 10_000;
        for (int i = 0; i < attempts; i++)
            if (filter.Accept(surface, sampler.Sample(rng), rng))
                accepted++;
        Assert.InRange(accepted / (double)attempts, 0.47, 0.53);
    }

    [Fact]
    public void Black_mask_hits_the_attempt_limit_and_reports_the_shortfall()
    {
        var filter = new MaskFilter(Flat(0f), MaskMode.Threshold, 0.5f);
        var result = ScatterPipeline.Run(TestMeshes.Plane(10), new ScatterOptions { Amount = 100 }, new[] { filter });
        Assert.Equal(0, result.Placed);
        Assert.Equal(100 * 20, result.Attempts);
        Assert.Equal(result.Attempts, result.Rejected["mask"]);
    }

    [Fact]
    public void Masked_scatter_is_deterministic()
    {
        var filter = new MaskFilter(Checker(), MaskMode.Density);
        var o = new ScatterOptions { Amount = 300, Seed = 9, MinDistance = 0.2f };
        Assert.Equal(ScatterPipeline.Run(TestMeshes.Plane(10), o, new[] { filter }).Transforms,
            ScatterPipeline.Run(TestMeshes.Plane(10), o, new[] { filter }).Transforms);
    }

    [Fact]
    public void Second_uv_set_is_used_when_asked()
    {
        var quad = TestMeshes.Quad(10);
        quad.Uv2 = quad.Uv.Select(uv => new Vector2(1 - uv.X, uv.Y)).ToArray(); // mirrored in U
        var surface = SurfaceData.Build(new[] { quad }, Transform3D.Identity);
        var left = new MaskImage(2, 1, new[] { 1f, 0f }); // white on the left half only (sharp enough at threshold 0.9)
        var uv1 = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 200 }, new[] { new MaskFilter(left, MaskMode.Threshold, 0.9f) });
        var uv2 = ScatterPipeline.Run(surface, new ScatterOptions { Amount = 200 }, new[] { new MaskFilter(left, MaskMode.Threshold, 0.9f, secondUvSet: true) });
        Assert.All(uv1.Transforms, t => Assert.True(t.Origin.X < 0));
        Assert.All(uv2.Transforms, t => Assert.True(t.Origin.X > 0));
    }
}
