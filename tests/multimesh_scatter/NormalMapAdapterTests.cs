namespace Godoodads.Tests.MultiMeshScatter;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class NormalMapAdapterTests
{
    private static readonly Vector3 Centre = new(1f / 3, 1f / 3, 1f / 3);

    private static Vector3 Binormal(SurfaceData data, int triangle)
    {
        var n = data.SmoothNormalAt(triangle, Centre);
        var (t, sign) = data.TangentAt(triangle, Centre, n);
        return n.Cross(t) * sign;
    }

    // Confirms the convention NormalMapTests assumes: on Godot's own PlaneMesh the tangent is +X
    // (+U) and the binormal cross(N, T) * w is -Z, the top of the image (-V).
    [TestCase]
    [RequireGodotRuntime]
    public void Plane_mesh_tangent_frame_matches_godot_convention()
    {
        var data = SurfaceSource.FromMesh(new PlaneMesh(), Transform3D.Identity);
        AssertThat(data.HasTangents).IsTrue();
        for (int t = 0; t < data.TriangleCount; t++)
        {
            var n = data.SmoothNormalAt(t, Centre);
            AssertThat(data.TangentAt(t, Centre, n).Tangent.Dot(Vector3.Right)).IsGreater(0.999f);
            AssertThat(Binormal(data, t).Dot(Vector3.Forward)).IsGreater(0.999f);
        }
    }

    // Godot 4.2+ stores placeholder tangents for meshes with normals but no tangents, and returns
    // them from SurfaceGetArrays; they must be treated as missing.
    [TestCase]
    [RequireGodotRuntime]
    public void Placeholder_tangents_are_detected_and_replaced_on_a_copy()
    {
        var arrays = new SphereMesh().SurfaceGetArrays(0);
        AssertThat(SurfaceSource.HasRealTangents(arrays)).IsTrue();
        arrays[(int)Mesh.ArrayType.Tangent] = default;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var stored = mesh.SurfaceGetArrays(0);
        AssertThat(stored[(int)Mesh.ArrayType.Tangent].VariantType == Variant.Type.Nil).IsFalse(); // Godot filled in placeholders
        AssertThat(SurfaceSource.HasRealTangents(stored)).IsFalse();
        var before = stored[(int)Mesh.ArrayType.Tangent].AsFloat32Array();

        var plain = SurfaceSource.FromMesh(mesh, Transform3D.Identity, ensureTangents: false, out var generated);
        AssertThat(plain.HasTangents).IsFalse();
        AssertThat(generated).IsFalse();

        var data = SurfaceSource.FromMesh(mesh, Transform3D.Identity, ensureTangents: true, out generated);
        AssertThat(generated).IsTrue();
        AssertThat(data.HasTangents).IsTrue();
        AssertThat(SurfaceSource.HasRealTangents(new Godot.Collections.Array(mesh.SurfaceGetArrays(0)))).IsFalse(); // user's mesh unchanged
        AssertThat(mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Tangent].AsFloat32Array()).IsEqual(before);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Generated_tangents_follow_the_godot_convention_on_a_plane()
    {
        var arrays = new PlaneMesh().SurfaceGetArrays(0);
        arrays[(int)Mesh.ArrayType.Tangent] = default;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var data = SurfaceSource.FromMesh(mesh, Transform3D.Identity, ensureTangents: true, out var generated);
        AssertThat(generated).IsTrue();
        for (int t = 0; t < data.TriangleCount; t++)
            AssertThat(Binormal(data, t).Dot(Vector3.Forward)).IsGreater(0.999f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Two_channel_maps_work_in_tangent_space_only()
    {
        var image = Image.CreateEmpty(4, 4, false, Image.Format.Rg8);
        image.Fill(new Color(0.5f, 0.5f, 0f));
        AssertThat(NormalMapSource.FromImage(image, NormalMapSpace.Tangent, false, out var error) != null).IsTrue();
        AssertThat(error == null).IsTrue();
        AssertThat(NormalMapSource.FromImage(image, NormalMapSpace.Object, false, out error) == null).IsTrue();
        AssertThat(error).Contains("only red and green");
    }

    // End to end on a real PlaneMesh: a green-up (OpenGL) map leans instances toward -Z.
    [TestCase]
    [RequireGodotRuntime]
    public void Green_up_map_leans_toward_the_image_top_on_a_plane_mesh()
    {
        var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        image.Fill(new Color(0.5f, 0.5f + 0.5f * Mathf.Sin(Mathf.DegToRad(20)), 1f));
        var map = NormalMapSource.FromImage(image, NormalMapSpace.Tangent, false, out _);
        var surface = SurfaceSource.FromMesh(new PlaneMesh { Size = new Vector2(4, 4) }, Transform3D.Identity);
        var result = ScatterPipeline.Run(surface, new ScatterOptions
        {
            Amount = 100, Alignment = 0, RandomScale = 0, NormalSource = NormalSource.NormalMap,
            NormalMap = new NormalMapOptions { Map = map },
        });
        AssertThat(result.Transforms.All(t => t.Basis.Y.Z < 0
            && Mathf.Abs(Mathf.RadToDeg(t.Basis.Y.AngleTo(Vector3.Up)) - 20f) < 1f)).IsTrue();
    }
}
