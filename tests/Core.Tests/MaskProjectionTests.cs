using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §10.2: a mask projected from above.
public class MaskProjectionTests
{
    private static readonly Rect2 Area = new(-10, 4, 20, 8);   // X from -10 to 10, Z from 4 to 12

    [Fact]
    public void CornersAndCentreMapAcrossTheImage()
    {
        Assert.Equal(new Vector2(0, 0), MaskFilter.TopDownUv(new Vector3(-10, 5, 4), Area));
        Assert.Equal(new Vector2(1, 1), MaskFilter.TopDownUv(new Vector3(10, -3, 12), Area));
        Assert.Equal(new Vector2(0.5f, 0.5f), MaskFilter.TopDownUv(new Vector3(0, 0, 8), Area));
        // +X is to the right of the image and +Z down; height does not matter.
        Assert.Equal(new Vector2(0.75f, 0.25f), MaskFilter.TopDownUv(new Vector3(5, 100, 6), Area));
    }

    [Fact]
    public void TheAreaIsTheExtentSeenFromAbove()
    {
        var area = MaskFilter.TopDownArea(new[] { new Vector3(1, 9, -2), new Vector3(-3, 0, 5), new Vector3(2, -4, 1) });
        Assert.Equal(new Rect2(-3, -2, 5, 7), area);
        Assert.Equal(new Vector2(0.5f, 0.5f), MaskFilter.TopDownUv(Vector3.Zero, new Rect2(0, 0, 0, 0)));
    }

    // A flat 10 x 10 quad at y = 0 whose UVs all sit on the black pixel, so only the projection can tell its halves apart.
    private static SurfaceData Quad() => SurfaceData.Build(new[]
    {
        new SurfaceArrays
        {
            Vertices = new[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(0, 0, 10) },
            Uv = new[] { new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f) },
            Indices = new[] { 0, 2, 1, 0, 3, 2 },
        },
    }, Transform3D.Identity);

    [Fact]
    public void TopDownSamplesByPositionAndUvByUv()
    {
        // Left half black, right half white.
        var mask = new MaskImage(2, 1, new[] { 0f, 1f });
        var surface = Quad();
        var right = new SurfacePoint(0, new Vector3(0.1f, 0.8f, 0.1f), surface.PositionAt(0, new Vector3(0.1f, 0.8f, 0.1f)));
        var left = new SurfacePoint(1, new Vector3(0.1f, 0.8f, 0.1f), surface.PositionAt(1, new Vector3(0.1f, 0.8f, 0.1f)));
        Assert.True(right.Position.X > 7 && left.Position.X < 3);

        var projected = new MaskFilter(mask, MaskMode.Threshold, 0.5f, topDownArea: MaskFilter.TopDownArea(surface.Positions));
        Assert.True(projected.Accept(surface, right, new Pcg32(1)));
        Assert.False(projected.Accept(surface, left, new Pcg32(1)));

        // By UV every point is on the black pixel.
        var byUv = new MaskFilter(mask, MaskMode.Threshold, 0.5f);
        Assert.False(byUv.Accept(surface, right, new Pcg32(1)));
    }
}
