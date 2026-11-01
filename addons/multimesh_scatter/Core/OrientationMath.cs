using System;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>Which surface normal the orientation stage follows.</summary>
public enum NormalSource
{
    /// <summary>Flat normal of the sampled triangle.</summary>
    Face,
    /// <summary>Vertex normals interpolated at the sample (falls back to the face normal).</summary>
    Smooth,
    /// <summary>The smooth normal perturbed by a normal map texture.</summary>
    NormalMap,
}

/// <summary>Local axis of the instanced mesh that counts as its "up".</summary>
public enum MeshAxis { PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ }

public static class OrientationMath
{
    public static Vector3 ToVector(this MeshAxis axis) => axis switch
    {
        MeshAxis.PositiveX => Vector3.Right,
        MeshAxis.NegativeX => Vector3.Left,
        MeshAxis.NegativeY => Vector3.Down,
        MeshAxis.PositiveZ => Vector3.Back,
        MeshAxis.NegativeZ => Vector3.Forward,
        _ => Vector3.Up,
    };

    /// <summary>Normal at a sample, according to <paramref name="source"/>.</summary>
    public static Vector3 SurfaceNormal(SurfaceData surface, in SurfacePoint point, NormalSource source,
        NormalMapOptions normalMap = null)
    {
        if (source == NormalSource.Face)
            return surface.FaceNormals[point.Triangle];
        var smooth = surface.SmoothNormalAt(point.Triangle, point.Barycentric);
        return source == NormalSource.NormalMap && normalMap?.Map != null
            ? normalMap.Apply(surface, point, smooth)
            : smooth;
    }

    /// <summary>
    /// Spherical interpolation between two unit directions. When they are (almost) opposite the
    /// rotation axis is a fixed perpendicular of <paramref name="from"/>, so the result is never NaN.
    /// </summary>
    public static Vector3 SlerpDirection(Vector3 from, Vector3 to, float t)
    {
        float dot = Math.Clamp(from.Dot(to), -1f, 1f);
        if (dot > 1f - 1e-6f)
            return from.Lerp(to, t).Normalized();
        var axis = dot < -1f + 1e-6f ? Perpendiculars(from).Item1 : from.Cross(to).Normalized();
        return from.Rotated(axis, MathF.Acos(dot) * t).Normalized();
    }

    /// <summary>Two unit vectors perpendicular to <paramref name="n"/> and to each other.</summary>
    public static (Vector3, Vector3) Perpendiculars(Vector3 n)
    {
        var helper = MathF.Abs(n.Y) < 0.99f ? Vector3.Up : Vector3.Right;
        var t1 = helper.Cross(n).Normalized();
        return (t1, n.Cross(t1));
    }

    /// <summary>
    /// Slope filter as sampler weights: 1 for triangles whose face normal lies between
    /// <paramref name="minDegrees"/> and <paramref name="maxDegrees"/> from <paramref name="reference"/>,
    /// else 0. Returns null when the range is 0–180 (no filtering).
    /// </summary>
    public static float[] SlopeWeights(SurfaceData surface, Vector3 reference, float minDegrees, float maxDegrees)
    {
        if (minDegrees <= 0f && maxDegrees >= 180f)
            return null;
        reference = reference.Normalized();
        float cosMin = MathF.Cos(Mathf.DegToRad(Math.Clamp(minDegrees, 0f, 180f)));
        float cosMax = MathF.Cos(Mathf.DegToRad(Math.Clamp(maxDegrees, 0f, 180f)));
        var weights = new float[surface.TriangleCount];
        for (int t = 0; t < weights.Length; t++)
        {
            var n = surface.FaceNormals[t];
            if (n == Vector3.Zero)
                continue;
            // Angles grow as the cosine shrinks; a small tolerance keeps exact boundaries inside.
            float cos = n.Dot(reference);
            weights[t] = cos <= cosMin + 1e-5f && cos >= cosMax - 1e-5f ? 1f : 0f;
        }
        return weights;
    }
}
