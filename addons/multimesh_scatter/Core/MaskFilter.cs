using Godot;

namespace Godoodads.MultiMeshScatter.Core;

public enum MaskMode
{
    /// <summary>Place only where the mask value is at least the threshold.</summary>
    Threshold,
    /// <summary>Place with a probability equal to the mask value, so grey areas are sparser.</summary>
    Density,
}

/// <summary>Where on the mask a point is sampled (brief §10.2).</summary>
public enum MaskProjection
{
    /// <summary>At the point's UV coordinate on the surface.</summary>
    Uv,
    /// <summary>At the point's world X and Z, stretched over an area seen from above.</summary>
    TopDown,
}

/// <summary>Accepts or rejects candidates by the mask value at their UV coordinate, or projected from above.</summary>
public sealed class MaskFilter : ICandidateFilter
{
    private readonly MaskImage _mask;
    private readonly MaskMode _mode;
    private readonly float _threshold;
    private readonly bool _invert;
    private readonly bool _secondUvSet;
    private readonly Rect2? _topDown;

    public string Name => "mask";

    /// <param name="topDownArea">
    /// When set, the mask is projected from above over this area of world X (Rect2 X) and Z
    /// (Rect2 Y): +X runs to the right of the image and +Z down.
    /// </param>
    public MaskFilter(MaskImage mask, MaskMode mode, float threshold = 0.5f, bool invert = false, bool secondUvSet = false,
        Rect2? topDownArea = null)
    {
        _topDown = topDownArea;
        _mask = mask;
        _mode = mode;
        _threshold = threshold;
        _invert = invert;
        _secondUvSet = secondUvSet;
    }

    public float ValueAt(SurfaceData surface, in SurfacePoint point)
    {
        var uv = _topDown is { } area
            ? TopDownUv(surface.PositionAt(point.Triangle, point.Barycentric), area)
            : surface.UvAt(point.Triangle, point.Barycentric, _secondUvSet);
        float value = _mask.SampleBilinear(uv);
        return _invert ? 1f - value : value;
    }

    /// <summary>The mask coordinate of a world point projected from above over <paramref name="area"/>.</summary>
    public static Godot.Vector2 TopDownUv(Godot.Vector3 position, Rect2 area) => new(
        area.Size.X > 0 ? (position.X - area.Position.X) / area.Size.X : 0.5f,
        area.Size.Y > 0 ? (position.Z - area.Position.Y) / area.Size.Y : 0.5f);

    /// <summary>The X/Z rectangle (as Rect2 X and Y) around points, for a top-down projection.</summary>
    public static Rect2 TopDownArea(System.Collections.Generic.IEnumerable<Godot.Vector3> points)
    {
        float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var p in points)
        {
            minX = System.Math.Min(minX, p.X);
            minZ = System.Math.Min(minZ, p.Z);
            maxX = System.Math.Max(maxX, p.X);
            maxZ = System.Math.Max(maxZ, p.Z);
        }
        return minX > maxX ? new Rect2() : new Rect2(minX, minZ, maxX - minX, maxZ - minZ);
    }

    public bool Accept(SurfaceData surface, in SurfacePoint point, Pcg32 rng)
    {
        float value = ValueAt(surface, point);
        // Density draws a number for every candidate, Threshold none, so a mode switch is the
        // only thing that shifts the sampling stream.
        return _mode == MaskMode.Density ? rng.NextFloat() < value : value >= _threshold;
    }
}
