using System;
using System.Collections.Generic;
using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Adapters;

public static class MultiMeshWriter
{
    /// <summary>
    /// Returns a copy of <paramref name="source"/> holding <paramref name="localTransforms"/>, either
    /// replacing or appended after its current instances. The copy keeps the mesh and settings such
    /// as use_colors, use_custom_data and the custom AABB; <paramref name="source"/> is not modified.
    /// </summary>
    public static MultiMesh Build(MultiMesh source, IReadOnlyList<Transform3D> localTransforms, bool append)
    {
        var mm = (MultiMesh)source.Duplicate();
        if (mm.TransformFormat != MultiMesh.TransformFormatEnum.Transform3D)
        {
            if (append && source.InstanceCount > 0)
                throw new InvalidOperationException("Cannot append 3D instances to a MultiMesh that uses 2D transforms.");
            mm.InstanceCount = 0; // the transform format can only change while empty
            mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        }

        int keep = append ? source.InstanceCount : 0;
        float[] existing = keep > 0 ? source.Buffer : Array.Empty<float>();
        var buffer = InstanceBuffer.Compose(existing, keep, localTransforms, mm.UseColors, mm.UseCustomData);

        mm.InstanceCount = keep + localTransforms.Count;
        mm.VisibleInstanceCount = -1;
        if (mm.InstanceCount > 0)
            mm.Buffer = buffer;
        return mm;
    }

    /// <summary>World positions of a MultiMeshInstance3D's instances, read from the buffer.</summary>
    public static List<Vector3> WorldPositions(MultiMeshInstance3D instance)
    {
        var result = new List<Vector3>();
        var mm = instance.Multimesh;
        if (mm == null || mm.InstanceCount == 0 || mm.TransformFormat != MultiMesh.TransformFormatEnum.Transform3D)
            return result;
        var buffer = mm.Buffer;
        int stride = InstanceBuffer.Stride(mm.UseColors, mm.UseCustomData);
        var toWorld = instance.GlobalTransform;
        for (int i = 0; i < mm.InstanceCount && (i + 1) * stride <= buffer.Length; i++)
            result.Add(toWorld * InstanceBuffer.ReadTransform(buffer, i * stride).Origin);
        return result;
    }
}
