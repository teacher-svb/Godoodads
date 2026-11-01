using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.Tests;

/// <summary>Hand-built meshes with Godot's clockwise front-face winding.</summary>
internal static class TestMeshes
{
    /// <summary>Indexed quad on the XZ plane, centred on the origin, facing +Y.</summary>
    public static SurfaceArrays Quad(float size = 1f)
    {
        float h = size / 2;
        return new SurfaceArrays
        {
            Vertices = new[] { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h) },
            Normals = new[] { Vector3.Up, Vector3.Up, Vector3.Up, Vector3.Up },
            Uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
            // Clockwise seen from +Y.
            Indices = new[] { 0, 1, 2, 0, 2, 3 },
        };
    }

    /// <summary>The same quad without indices.</summary>
    public static SurfaceArrays QuadNonIndexed(float size = 1f)
    {
        var q = Quad(size);
        var v = new Vector3[6];
        var n = new Vector3[6];
        var uv = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            v[i] = q.Vertices[q.Indices[i]];
            n[i] = q.Normals[q.Indices[i]];
            uv[i] = q.Uv[q.Indices[i]];
        }
        return new SurfaceArrays { Vertices = v, Normals = n, Uv = uv };
    }

    public static SurfaceData Plane(float size = 10f) =>
        SurfaceData.Build(new[] { Quad(size) }, Transform3D.Identity);

    /// <summary>UV sphere of radius 1 around the origin with radial vertex normals.</summary>
    public static SurfaceData Sphere(int rings = 32, int segments = 64)
    {
        var vertices = new List<Vector3>();
        for (int r = 0; r <= rings; r++)
        {
            float phi = Mathf.Pi * r / rings;
            for (int s = 0; s <= segments; s++)
            {
                float theta = Mathf.Tau * s / segments;
                vertices.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)));
            }
        }
        var indices = new List<int>();
        int row = segments + 1;
        for (int r = 0; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                int i = r * row + s;
                AddOutward(indices, vertices, i, i + 1, i + row + 1, Vector3.Zero);
                AddOutward(indices, vertices, i, i + row + 1, i + row, Vector3.Zero);
            }
        var normals = vertices.Select(v => v.Normalized()).ToArray();
        return SurfaceData.Build(new[] { new SurfaceArrays { Vertices = vertices.ToArray(), Normals = normals, Indices = indices.ToArray() } }, Transform3D.Identity);
    }

    /// <summary>Axis-aligned cube of the given size around the origin, without vertex normals.</summary>
    public static SurfaceData Cube(float size = 2f)
    {
        float h = size / 2;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        foreach (var n in new[] { Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right, Vector3.Forward, Vector3.Back })
        {
            var (t1, t2) = OrientationMath.Perpendiculars(n);
            int b = vertices.Count;
            vertices.Add((n - t1 - t2) * h);
            vertices.Add((n + t1 - t2) * h);
            vertices.Add((n + t1 + t2) * h);
            vertices.Add((n - t1 + t2) * h);
            AddOutward(indices, vertices, b, b + 1, b + 2, Vector3.Zero);
            AddOutward(indices, vertices, b, b + 2, b + 3, Vector3.Zero);
        }
        return SurfaceData.Build(new[] { new SurfaceArrays { Vertices = vertices.ToArray(), Indices = indices.ToArray() } }, Transform3D.Identity);
    }

    // Adds a triangle wound clockwise as seen from outside (Godot's front face), skipping degenerate ones.
    private static void AddOutward(List<int> indices, List<Vector3> v, int a, int b, int c, Vector3 center)
    {
        var cross = (v[c] - v[a]).Cross(v[b] - v[a]);
        if (cross.LengthSquared() < 1e-14f)
            return;
        var centroid = (v[a] + v[b] + v[c]) / 3f;
        if (cross.Dot(centroid - center) < 0)
            (b, c) = (c, b);
        indices.AddRange(new[] { a, b, c });
    }
}
