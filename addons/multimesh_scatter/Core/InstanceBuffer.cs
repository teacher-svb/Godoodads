using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// Packs transforms into the flat float layout of <c>MultiMesh.buffer</c> (3D transforms):
/// per instance a row-major 3x4 matrix (basis.x.x, basis.y.x, basis.z.x, origin.x, ...),
/// followed by 4 color floats if colors are used and 4 custom-data floats if custom data is used.
/// </summary>
public static class InstanceBuffer
{
    public const int TransformFloats = 12;

    public static int Stride(bool useColors, bool useCustomData) =>
        TransformFloats + (useColors ? 4 : 0) + (useCustomData ? 4 : 0);

    public static void WriteTransform(float[] buffer, int offset, in Transform3D t)
    {
        var b = t.Basis;
        buffer[offset + 0] = b.X.X;
        buffer[offset + 1] = b.Y.X;
        buffer[offset + 2] = b.Z.X;
        buffer[offset + 3] = t.Origin.X;
        buffer[offset + 4] = b.X.Y;
        buffer[offset + 5] = b.Y.Y;
        buffer[offset + 6] = b.Z.Y;
        buffer[offset + 7] = t.Origin.Y;
        buffer[offset + 8] = b.X.Z;
        buffer[offset + 9] = b.Y.Z;
        buffer[offset + 10] = b.Z.Z;
        buffer[offset + 11] = t.Origin.Z;
    }

    public static Transform3D ReadTransform(float[] buffer, int offset)
    {
        var x = new Vector3(buffer[offset + 0], buffer[offset + 4], buffer[offset + 8]);
        var y = new Vector3(buffer[offset + 1], buffer[offset + 5], buffer[offset + 9]);
        var z = new Vector3(buffer[offset + 2], buffer[offset + 6], buffer[offset + 10]);
        var origin = new Vector3(buffer[offset + 3], buffer[offset + 7], buffer[offset + 11]);
        return new Transform3D(new Basis(x, y, z), origin);
    }

    /// <summary>
    /// Builds a buffer holding the first <paramref name="keepCount"/> instances of
    /// <paramref name="existing"/> followed by <paramref name="added"/>. New instances get a white
    /// color and zero custom data.
    /// </summary>
    public static float[] Compose(float[] existing, int keepCount, IReadOnlyList<Transform3D> added,
        bool useColors, bool useCustomData)
    {
        int stride = Stride(useColors, useCustomData);
        var buffer = new float[(keepCount + added.Count) * stride];
        if (keepCount > 0)
            Array.Copy(existing, buffer, keepCount * stride);
        for (int i = 0; i < added.Count; i++)
        {
            int offset = (keepCount + i) * stride;
            WriteTransform(buffer, offset, added[i]);
            if (useColors)
                buffer[offset + 12] = buffer[offset + 13] = buffer[offset + 14] = buffer[offset + 15] = 1f;
        }
        return buffer;
    }
}
