namespace Godoodads.MultiMeshScatter.Core;

public enum MaskMode
{
    /// <summary>Place only where the mask value is at least the threshold.</summary>
    Threshold,
    /// <summary>Place with a probability equal to the mask value, so grey areas are sparser.</summary>
    Density,
}

/// <summary>Accepts or rejects candidates by the mask value at their UV coordinate.</summary>
public sealed class MaskFilter : ICandidateFilter
{
    private readonly MaskImage _mask;
    private readonly MaskMode _mode;
    private readonly float _threshold;
    private readonly bool _invert;
    private readonly bool _secondUvSet;

    public string Name => "mask";

    public MaskFilter(MaskImage mask, MaskMode mode, float threshold = 0.5f, bool invert = false, bool secondUvSet = false)
    {
        _mask = mask;
        _mode = mode;
        _threshold = threshold;
        _invert = invert;
        _secondUvSet = secondUvSet;
    }

    public float ValueAt(SurfaceData surface, in SurfacePoint point)
    {
        float value = _mask.SampleBilinear(surface.UvAt(point.Triangle, point.Barycentric, _secondUvSet));
        return _invert ? 1f - value : value;
    }

    public bool Accept(SurfaceData surface, in SurfacePoint point, Pcg32 rng)
    {
        float value = ValueAt(surface, point);
        // Density draws a number for every candidate, Threshold none, so a mode switch is the
        // only thing that shifts the sampling stream.
        return _mode == MaskMode.Density ? rng.NextFloat() < value : value >= _threshold;
    }
}
