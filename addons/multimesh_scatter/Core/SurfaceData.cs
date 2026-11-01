using System.Collections.Generic;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>Raw triangle-list arrays of one mesh surface, in the mesh's local space.</summary>
public sealed class SurfaceArrays
{
    public Vector3[] Vertices;
    public Vector3[] Normals;
    /// <summary>Four floats per vertex (xyz plus binormal sign), as in Godot's ARRAY_TANGENT.</summary>
    public float[] Tangents;
    public Vector2[] Uv;
    public Vector2[] Uv2;
    /// <summary>Triangle indices; null for non-indexed surfaces.</summary>
    public int[] Indices;
}

/// <summary>
/// All triangles of a surface in world space, with per-vertex attributes and per-triangle
/// face normals and areas. Surfaces are concatenated; attributes missing on some surface are
/// zero-filled there and reported through the Has* flags.
/// </summary>
public sealed class SurfaceData
{
    public Vector3[] Positions { get; private init; }
    public Vector3[] Normals { get; private init; }
    public float[] Tangents { get; private init; }
    public Vector2[] Uv { get; private init; }
    public Vector2[] Uv2 { get; private init; }
    public int[] Indices { get; private init; }

    public Vector3[] FaceNormals { get; private init; }
    public float[] Areas { get; private init; }
    public float TotalArea { get; private init; }
    /// <summary>Transforms mesh-local normals to world space (inverse transpose of the mesh basis).</summary>
    public Basis NormalBasis { get; private init; } = Basis.Identity;

    public bool HasNormals { get; private init; }
    public bool HasTangents { get; private init; }
    public bool HasUv { get; private init; }
    public bool HasUv2 { get; private init; }

    public int TriangleCount => Indices.Length / 3;

    public static SurfaceData Build(IReadOnlyList<SurfaceArrays> surfaces, Transform3D toWorld)
    {
        int vertexCount = 0, indexCount = 0;
        bool hasNormals = true, hasTangents = true, hasUv = true, hasUv2 = true;
        foreach (var s in surfaces)
        {
            int n = s.Vertices?.Length ?? 0;
            vertexCount += n;
            indexCount += s.Indices != null ? s.Indices.Length - s.Indices.Length % 3 : n - n % 3;
            hasNormals &= s.Normals?.Length == n;
            hasTangents &= s.Tangents?.Length == n * 4;
            hasUv &= s.Uv?.Length == n;
            hasUv2 &= s.Uv2?.Length == n;
        }

        var positions = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var tangents = new float[vertexCount * 4];
        var uv = new Vector2[vertexCount];
        var uv2 = new Vector2[vertexCount];
        var indices = new int[indexCount];

        var basis = toWorld.Basis;
        var normalBasis = basis.Inverse().Transposed();
        float handedness = basis.Determinant() < 0 ? -1f : 1f;

        int vBase = 0, iBase = 0;
        foreach (var s in surfaces)
        {
            int n = s.Vertices?.Length ?? 0;
            for (int v = 0; v < n; v++)
            {
                positions[vBase + v] = toWorld * s.Vertices[v];
                if (s.Normals?.Length == n)
                    normals[vBase + v] = (normalBasis * s.Normals[v]).Normalized();
                if (s.Tangents?.Length == n * 4)
                {
                    int t = v * 4, o = (vBase + v) * 4;
                    var tangent = (basis * new Vector3(s.Tangents[t], s.Tangents[t + 1], s.Tangents[t + 2])).Normalized();
                    tangents[o] = tangent.X;
                    tangents[o + 1] = tangent.Y;
                    tangents[o + 2] = tangent.Z;
                    tangents[o + 3] = s.Tangents[t + 3] * handedness;
                }
                if (s.Uv?.Length == n)
                    uv[vBase + v] = s.Uv[v];
                if (s.Uv2?.Length == n)
                    uv2[vBase + v] = s.Uv2[v];
            }

            if (s.Indices != null)
            {
                int count = s.Indices.Length - s.Indices.Length % 3;
                for (int i = 0; i < count; i++)
                    indices[iBase + i] = vBase + s.Indices[i];
                iBase += count;
            }
            else
            {
                int count = n - n % 3;
                for (int i = 0; i < count; i++)
                    indices[iBase + i] = vBase + i;
                iBase += count;
            }
            vBase += n;
        }

        int triangles = indexCount / 3;
        var faceNormals = new Vector3[triangles];
        var areas = new float[triangles];
        double total = 0;
        for (int t = 0; t < triangles; t++)
        {
            var a = positions[indices[t * 3]];
            var b = positions[indices[t * 3 + 1]];
            var c = positions[indices[t * 3 + 2]];
            // Godot front faces wind clockwise, so the outward normal is (c - a) x (b - a).
            // Positions are already in world space; a mirroring transform flips the winding back.
            var cross = (c - a).Cross(b - a);
            float length = cross.Length();
            areas[t] = length * 0.5f;
            faceNormals[t] = length > 0 ? cross / length * handedness : Vector3.Zero;
            total += areas[t];
        }

        return new SurfaceData
        {
            Positions = positions,
            Normals = normals,
            Tangents = tangents,
            Uv = uv,
            Uv2 = uv2,
            Indices = indices,
            FaceNormals = faceNormals,
            Areas = areas,
            TotalArea = (float)total,
            NormalBasis = normalBasis,
            HasNormals = hasNormals && vertexCount > 0,
            HasTangents = hasTangents && vertexCount > 0,
            HasUv = hasUv && vertexCount > 0,
            HasUv2 = hasUv2 && vertexCount > 0,
        };
    }

    public (int A, int B, int C) Triangle(int triangle) =>
        (Indices[triangle * 3], Indices[triangle * 3 + 1], Indices[triangle * 3 + 2]);

    /// <summary>World position at barycentric coordinates on a triangle.</summary>
    public Vector3 PositionAt(int triangle, Vector3 bary)
    {
        var (a, b, c) = Triangle(triangle);
        return Positions[a] * bary.X + Positions[b] * bary.Y + Positions[c] * bary.Z;
    }

    /// <summary>Interpolated vertex normal, or the face normal if the surface has no normals.</summary>
    public Vector3 SmoothNormalAt(int triangle, Vector3 bary)
    {
        if (!HasNormals)
            return FaceNormals[triangle];
        var (a, b, c) = Triangle(triangle);
        var n = Normals[a] * bary.X + Normals[b] * bary.Y + Normals[c] * bary.Z;
        return n.LengthSquared() > 1e-12f ? n.Normalized() : FaceNormals[triangle];
    }

    /// <summary>
    /// Interpolated tangent at a sample, made orthogonal to <paramref name="normal"/>, and the
    /// binormal sign. Without tangent data a fixed perpendicular of the normal is used.
    /// </summary>
    public (Vector3 Tangent, float Sign) TangentAt(int triangle, Vector3 bary, Vector3 normal)
    {
        if (HasTangents)
        {
            var (a, b, c) = Triangle(triangle);
            var t = TangentVector(a) * bary.X + TangentVector(b) * bary.Y + TangentVector(c) * bary.Z;
            t -= normal * normal.Dot(t);
            if (t.LengthSquared() > 1e-12f)
                return (t.Normalized(), Tangents[a * 4 + 3] < 0 ? -1f : 1f);
        }
        return (OrientationMath.Perpendiculars(normal).Item1, 1f);
    }

    private Vector3 TangentVector(int vertex) =>
        new(Tangents[vertex * 4], Tangents[vertex * 4 + 1], Tangents[vertex * 4 + 2]);

    public Vector2 UvAt(int triangle, Vector3 bary, bool secondSet = false)
    {
        var set = secondSet ? Uv2 : Uv;
        var (a, b, c) = Triangle(triangle);
        return set[a] * bary.X + set[b] * bary.Y + set[c] * bary.Z;
    }
}
