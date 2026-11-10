using System;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

public enum NormalMapSpace
{
    /// <summary>Relative to the surface's normal, tangent and binormal (Godot's default).</summary>
    Tangent,
    /// <summary>Directions in the surface mesh's local space.</summary>
    Object,
}

/// <summary>
/// Decoded normal map texels (each channel mapped from [0, 1] to [-1, 1]), sampled bilinearly with
/// pixel centres at (i + 0.5) / size and repeat wrapping, like <see cref="MaskImage"/>.
/// </summary>
public sealed class NormalMap
{
    public int Width { get; }
    public int Height { get; }
    private readonly Vector3[] _texels;

    public NormalMap(int width, int height, Vector3[] texels)
    {
        if (width <= 0 || height <= 0 || texels.Length != width * height)
            throw new ArgumentException("Normal map size does not match its data.");
        Width = width;
        Height = height;
        _texels = texels;
    }

    /// <summary>Decodes RGBA8 bytes. <paramref name="flipGreen"/> converts DirectX-style (Y-) maps.</summary>
    public static NormalMap FromRgba8(int width, int height, byte[] rgba, bool flipGreen)
    {
        var texels = new Vector3[width * height];
        for (int i = 0; i < texels.Length; i++)
        {
            int o = i * 4;
            float y = rgba[o + 1] / 255f * 2f - 1f;
            texels[i] = new Vector3(rgba[o] / 255f * 2f - 1f, flipGreen ? -y : y, rgba[o + 2] / 255f * 2f - 1f);
        }
        return new NormalMap(width, height, texels);
    }

    public Vector3 SampleBilinear(Vector2 uv)
    {
        float fx = uv.X * Width - 0.5f;
        float fy = uv.Y * Height - 0.5f;
        int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        var top = At(x0, y0).Lerp(At(x0 + 1, y0), tx);
        var bottom = At(x0, y0 + 1).Lerp(At(x0 + 1, y0 + 1), tx);
        return top.Lerp(bottom, ty);
    }

    private Vector3 At(int x, int y) => _texels[Wrap(y, Height) * Width + Wrap(x, Width)];

    private static int Wrap(int i, int size) => ((i % size) + size) % size;
}

/// <summary>Normal map stage settings for <see cref="NormalSource.NormalMap"/>.</summary>
public sealed class NormalMapOptions
{
    public NormalMap Map;
    public NormalMapSpace Space = NormalMapSpace.Tangent;
    /// <summary>0 ignores the map, 1 uses the full map normal (Godot's normal_map_depth mix).</summary>
    public float Strength = 1f;
    public bool SecondUvSet;

    /// <summary>
    /// Perturbs the smooth normal at <paramref name="point"/> with the map, using the formulas of
    /// Godot's forward shader so instances follow the normal Godot renders.
    /// </summary>
    public Vector3 Apply(SurfaceData surface, in SurfacePoint point, Vector3 normal)
    {
        var texel = Map.SampleBilinear(surface.UvAt(point.Triangle, point.Barycentric, SecondUvSet));
        Vector3 mapped;
        if (Space == NormalMapSpace.Object)
        {
            mapped = surface.NormalBasisOf(point.Triangle) * texel;
        }
        else
        {
            // Godot ignores the stored blue channel and rebuilds z, so RG-packed maps work too.
            float z = MathF.Sqrt(MathF.Max(0f, 1f - texel.X * texel.X - texel.Y * texel.Y));
            var (tangent, sign) = surface.TangentAt(point.Triangle, point.Barycentric, normal);
            var binormal = normal.Cross(tangent).Normalized() * sign;
            mapped = tangent * texel.X + binormal * texel.Y + normal * z;
        }
        var result = normal.Lerp(mapped, Math.Clamp(Strength, 0f, 1f));
        return result.LengthSquared() > 1e-12f ? result.Normalized() : normal;
    }
}
