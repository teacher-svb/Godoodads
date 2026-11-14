using System.Collections.Generic;
using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Adapters;

/// <summary>
/// Which cells of a GridMap surface are used (brief §10.1): items by name or wildcard pattern
/// (see <see cref="NameFilter"/>), included or excluded (none listed means all), and a box of
/// cells (a size of 0 means the whole map).
/// </summary>
public sealed record GridMapSelection(IReadOnlyList<string> Items = null, bool Exclude = false, Vector3I BoxOrigin = default, Vector3I BoxSize = default)
{
    public bool HasBox => BoxSize.X > 0 && BoxSize.Y > 0 && BoxSize.Z > 0;

    public bool Contains(Vector3I cell) => !HasBox
        || (cell.X >= BoxOrigin.X && cell.Y >= BoxOrigin.Y && cell.Z >= BoxOrigin.Z
            && cell.X < BoxOrigin.X + BoxSize.X && cell.Y < BoxOrigin.Y + BoxSize.Y && cell.Z < BoxOrigin.Z + BoxSize.Z);
}

/// <summary>
/// What a GridMap surface was built from, for the report. <see cref="UnmatchedItems"/> lists the
/// item filter entries that match no item of the MeshLibrary.
/// </summary>
public readonly record struct GridMapSurfaceStats(int Cells, int CoveredTriangles, IReadOnlyList<string> UnmatchedItems = null);

/// <summary>Reads the triangles of a MeshInstance3D, root CSG shape or GridMap into <see cref="SurfaceData"/>.</summary>
public static class SurfaceSource
{
    public static bool IsSupported(Node node) => node is MeshInstance3D or CsgShape3D or GridMap;

    /// <summary>Returns null and sets <paramref name="error"/> if the node has no usable triangles.</summary>
    public static SurfaceData FromNode(Node3D node, out string error) => FromNode(node, false, out error, out _);

    /// <param name="ensureTangents">Generate tangents, on a copy, for surfaces that have none.</param>
    /// <param name="generatedTangents">True when tangents had to be generated.</param>
    public static SurfaceData FromNode(Node3D node, bool ensureTangents, out string error, out bool generatedTangents,
        GridMapSelection selection = null)
    {
        generatedTangents = false;
        error = null;
        if (node is GridMap map)
            return FromGridMap(map, selection, ensureTangents, out error, out generatedTangents, out _);
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
        return SurfaceData.Build(ReadSurfaces(mesh, ensureTangents, ref generatedTangents), toWorld);
    }

    private static List<SurfaceArrays> ReadSurfaces(Mesh mesh, bool ensureTangents, ref bool generatedTangents)
    {
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
        return surfaces;
    }

    /// <summary>
    /// The item meshes of a GridMap's used cells, placed as GridMap renders them (brief §10.1).
    /// A triangle on a boundary face of its cell is left out when the neighbouring cell on that
    /// side is occupied, also outside the selection box. The MeshLibrary is only read.
    /// </summary>
    public static SurfaceData FromGridMap(GridMap map, GridMapSelection selection, bool ensureTangents,
        out string error, out bool generatedTangents, out GridMapSurfaceStats stats)
    {
        error = null;
        generatedTangents = false;
        stats = default;
        selection ??= new GridMapSelection();
        var library = map.MeshLibrary;
        if (library == null)
        {
            error = $"'{map.Name}' has no MeshLibrary.";
            return null;
        }

        var filter = new NameFilter(selection.Items, selection.Exclude);
        List<string> unmatched = null;
        if (!filter.IsEmpty)
        {
            var itemNames = new List<string>();
            foreach (int id in library.GetItemList())
                itemNames.Add(library.GetItemName(id));
            unmatched = filter.Unmatched(itemNames);
        }
        // Matched once per item, not per cell.
        var accepted = new Dictionary<int, bool>();
        var itemSurfaces = new Dictionary<int, List<SurfaceArrays>>();
        var faceCache = new Dictionary<(int Item, int Orientation), int[][]>();
        var cellSize = map.CellSize;
        var surfaces = new List<SurfaceArrays>();
        int cells = 0, covered = 0;

        foreach (var cell in map.GetUsedCells())
        {
            if (!selection.Contains(cell))
                continue;
            int item = map.GetCellItem(cell);
            if (!filter.IsEmpty)
            {
                if (!accepted.TryGetValue(item, out bool use))
                    accepted[item] = use = filter.Accepts(library.GetItemName(item));
                if (!use)
                    continue;
            }
            if (!itemSurfaces.TryGetValue(item, out var parts))
            {
                var mesh = library.GetItemMesh(item);
                parts = mesh == null ? new List<SurfaceArrays>() : ReadSurfaces(mesh, ensureTangents, ref generatedTangents);
                itemSurfaces[item] = parts;
            }
            if (parts.Count == 0)
                continue;
            cells++;

            int orientation = map.GetCellItemOrientation(cell);
            var placement = new Transform3D(map.GetCellItemBasis(cell).Scaled(Vector3.One * map.CellScale), map.MapToLocal(cell))
                * library.GetItemMeshTransform(item);
            var cellMin = new Vector3(cell.X * cellSize.X, cell.Y * cellSize.Y, cell.Z * cellSize.Z);

            // Which face each triangle lies on depends only on the item and its orientation.
            if (!faceCache.TryGetValue((item, orientation), out var faces))
            {
                var relative = placement;
                relative.Origin -= cellMin;
                faces = new int[parts.Count][];
                for (int s = 0; s < parts.Count; s++)
                    faces[s] = TriangleFaces(parts[s], relative, cellSize);
                faceCache[(item, orientation)] = faces;
            }

            for (int s = 0; s < parts.Count; s++)
            {
                var part = parts[s];
                var kept = new List<int>();
                int triangles = faces[s].Length;
                for (int tri = 0; tri < triangles; tri++)
                {
                    int face = faces[s][tri];
                    if (face >= 0 && map.GetCellItem(cell + GridFaces.Offsets[face]) != GridMap.InvalidCellItem)
                    {
                        covered++;
                        continue;
                    }
                    for (int k = 0; k < 3; k++)
                        kept.Add(part.Indices != null ? part.Indices[tri * 3 + k] : tri * 3 + k);
                }
                if (kept.Count == 0)
                    continue;
                surfaces.Add(new SurfaceArrays
                {
                    Vertices = part.Vertices, Normals = part.Normals, Tangents = part.Tangents, Uv = part.Uv, Uv2 = part.Uv2,
                    Indices = kept.ToArray(), Local = placement,
                });
            }
        }

        stats = new GridMapSurfaceStats(cells, covered, unmatched);
        if (cells == 0)
        {
            error = $"No cells of '{map.Name}' match the GridMap filter and box.";
            return null;
        }
        var data = SurfaceData.Build(surfaces, map.GlobalTransform);
        if (data.TriangleCount == 0)
        {
            error = $"Every face of the chosen cells of '{map.Name}' is covered by a neighbouring cell.";
            return null;
        }
        return data;
    }

    /// <summary>Per triangle, the cell face it lies on (see <see cref="GridFaces"/>) or -1, with the cell at the origin.</summary>
    private static int[] TriangleFaces(SurfaceArrays part, Transform3D placement, Vector3 cellSize)
    {
        int n = part.Vertices?.Length ?? 0;
        int count = (part.Indices?.Length ?? n) / 3;
        var faces = new int[count];
        int Index(int i) => part.Indices != null ? part.Indices[i] : i;
        for (int tri = 0; tri < count; tri++)
            faces[tri] = GridFaces.BoundaryFace(placement * part.Vertices[Index(tri * 3)], placement * part.Vertices[Index(tri * 3 + 1)],
                placement * part.Vertices[Index(tri * 3 + 2)], Vector3.Zero, cellSize);
        return faces;
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
