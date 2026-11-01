using System;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// One channel of a mask texture as floats in [0, 1], row-major from the top-left (matching
/// Godot's UV origin). Sampled bilinearly with pixel centres at (i + 0.5) / size and repeat wrapping.
/// </summary>
public sealed class MaskImage
{
    public int Width { get; }
    public int Height { get; }
    private readonly float[] _values;

    public MaskImage(int width, int height, float[] values)
    {
        if (width <= 0 || height <= 0 || values.Length != width * height)
            throw new ArgumentException("Mask size does not match its data.");
        Width = width;
        Height = height;
        _values = values;
    }

    public float this[int x, int y] => _values[y * Width + x];

    public float SampleBilinear(Vector2 uv)
    {
        float fx = uv.X * Width - 0.5f;
        float fy = uv.Y * Height - 0.5f;
        int x0 = (int)MathF.Floor(fx), y0 = (int)MathF.Floor(fy);
        float tx = fx - x0, ty = fy - y0;
        float top = Lerp(At(x0, y0), At(x0 + 1, y0), tx);
        float bottom = Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx);
        return Lerp(top, bottom, ty);
    }

    private float At(int x, int y) => _values[Wrap(y, Height) * Width + Wrap(x, Width)];

    private static int Wrap(int i, int size) => ((i % size) + size) % size;

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
