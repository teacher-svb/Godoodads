using System.Collections.Generic;
using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Adapters;

/// <summary>Reads the triangles of a MeshInstance3D or root CSG shape into <see cref="SurfaceData"/>.</summary>
public static class SurfaceSource
{
    public static bool IsSupported(Node node) => node is MeshInstance3D or CsgShape3D;

    /// <summary>Returns null and sets <paramref name="error"/> if the node has no usable triangles.</summary>
    public static SurfaceData FromNode(Node3D node, out string error) => FromNode(node, false, out error, out _);

    /// <param name="ensureTangents">Generate tangents, on a copy, for surfaces that have none.</param>
    /// <param name="generatedTangents">True when tangents had to be generated.</param>
    public static SurfaceData FromNode(Node3D node, bool ensureTangents, out string error, out bool generatedTangents)
    {
        generatedTangents = false;
        error = null;
        Mesh mesh = node switch
        {
            MeshInstance3D mi => mi.Mesh,
            CsgShape3D csg when !csg.IsRootShape() => null,
            CsgShape3D csg => csg.BakeStaticMesh(),
            _ => null,
        };
        if (mesh == null)
        {
            error = node is CsgShape3D ? $"'{node.Name}' is not a root CSG shape; pick the outermost CSG node." : $"'{node.Name}' has no mesh.";
            return null;
        }

        var data = FromMesh(mesh, node.GlobalTransform, ensureTangents, out generatedTangents);
        if (data.TriangleCount == 0)
            error = $"'{node.Name}' has no triangle surfaces.";
        return data.TriangleCount == 0 ? null : data;
    }

    public static SurfaceData FromMesh(Mesh mesh, Transform3D toWorld) => FromMesh(mesh, toWorld, false, out _);

    public static SurfaceData FromMesh(Mesh mesh, Transform3D toWorld, bool ensureTangents, out bool generatedTangents)
    {
        generatedTangents = false;
        var surfaces = new List<SurfaceArrays>();
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            if (mesh is ArrayMesh am && am.SurfaceGetPrimitiveType(s) != Mesh.PrimitiveType.Triangles)
                continue;
            var arrays = mesh.SurfaceGetArrays(s);
            if (ensureTangents && NeedsTangents(arrays))
            {
                arrays = WithGeneratedTangents(arrays);
                generatedTangents = true;
            }
            surfaces.Add(ToArrays(arrays));
        }
        return SurfaceData.Build(surfaces, toWorld);
    }

    private static bool NeedsTangents(Godot.Collections.Array arrays) =>
        !HasRealTangents(arrays)
        && arrays[(int)Mesh.ArrayType.Normal].VariantType != Variant.Type.Nil
        && arrays[(int)Mesh.ArrayType.TexUV].VariantType != Variant.Type.Nil;

    /// <summary>
    /// False when the surface has no tangents, or only the placeholders Godot 4.2+ stores for meshes
    /// with normals but no tangents (rendering_server.cpp: tangent = normalize(cross((n.z, -n.x, n.y), n)),
    /// w = 1). SurfaceGetArrays returns those as if they were real.
    /// </summary>
    public static bool HasRealTangents(Godot.Collections.Array arrays)
    {
        if (arrays[(int)Mesh.ArrayType.Tangent].VariantType == Variant.Type.Nil)
            return false;
        if (arrays[(int)Mesh.ArrayType.Normal].VariantType == Variant.Type.Nil)
            return true;
        var tangents = arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        for (int i = 0; i < normals.Length && i * 4 + 3 < tangents.Length; i++)
        {
            var n = normals[i];
            var placeholder = new Vector3(n.Z, -n.X, n.Y).Cross(n.Normalized()).Normalized();
            var t = new Vector3(tangents[i * 4], tangents[i * 4 + 1], tangents[i * 4 + 2]);
            // Tolerance covers Godot's 16-bit octahedral compression.
            if (tangents[i * 4 + 3] < 0 || t.Dot(placeholder) < 0.999f)
                return true;
        }
        return false;
    }

    // SurfaceGetArrays already returns a copy; SurfaceTool works on it, never on the user's mesh.
    private static Godot.Collections.Array WithGeneratedTangents(Godot.Collections.Array arrays)
    {
        var tool = new SurfaceTool();
        tool.CreateFromArrays(arrays);
        tool.GenerateTangents();
        return tool.CommitToArrays();
    }

    private static SurfaceArrays ToArrays(Godot.Collections.Array arrays)
    {
        Variant At(Mesh.ArrayType type) => arrays[(int)type];
        var result = new SurfaceArrays { Vertices = At(Mesh.ArrayType.Vertex).AsVector3Array() };
        if (At(Mesh.ArrayType.Normal).VariantType != Variant.Type.Nil)
            result.Normals = At(Mesh.ArrayType.Normal).AsVector3Array();
        if (HasRealTangents(arrays))
            result.Tangents = At(Mesh.ArrayType.Tangent).AsFloat32Array();
        if (At(Mesh.ArrayType.TexUV).VariantType != Variant.Type.Nil)
            result.Uv = At(Mesh.ArrayType.TexUV).AsVector2Array();
        if (At(Mesh.ArrayType.TexUV2).VariantType != Variant.Type.Nil)
            result.Uv2 = At(Mesh.ArrayType.TexUV2).AsVector2Array();
        if (At(Mesh.ArrayType.Index).VariantType != Variant.Type.Nil)
            result.Indices = At(Mesh.ArrayType.Index).AsInt32Array();
        return result;
    }
}
