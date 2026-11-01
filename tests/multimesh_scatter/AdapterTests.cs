namespace Godoodads.Tests.MultiMeshScatter;

using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class AdapterTests
{
    // Verifies the winding assumption in SurfaceData against Godot's own primitive meshes:
    // every computed face normal must agree with the mesh's vertex normals.
    [TestCase]
    [RequireGodotRuntime]
    public void Face_normals_point_outward_on_primitive_meshes()
    {
        var meshes = new PrimitiveMesh[] { new PlaneMesh(), new BoxMesh(), new SphereMesh { RadialSegments = 32, Rings = 16 } };
        foreach (var mesh in meshes)
        {
            var data = SurfaceSource.FromMesh(mesh, Transform3D.Identity);
            AssertThat(data.TriangleCount).IsGreater(0);
            for (int t = 0; t < data.TriangleCount; t++)
            {
                if (data.Areas[t] <= 1e-8f)
                    continue; // sphere poles have degenerate triangles
                var smooth = data.SmoothNormalAt(t, new Vector3(1f / 3, 1f / 3, 1f / 3));
                AssertThat(data.FaceNormals[t].Dot(smooth)).OverrideFailureMessage($"{mesh.GetClass()} triangle {t}").IsGreater(0.5f);
            }
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Primitive_mesh_attributes_are_read()
    {
        var data = SurfaceSource.FromMesh(new BoxMesh(), Transform3D.Identity);
        AssertThat(data.HasNormals).IsTrue();
        AssertThat(data.HasUv).IsTrue();
        AssertThat(data.HasTangents).IsTrue();
        AssertThat(data.TotalArea).IsEqualApprox(6f, 1e-3f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Writer_keeps_flags_and_does_not_touch_the_source()
    {
        var mesh = new BoxMesh();
        var source = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            UseCustomData = true,
            Mesh = mesh,
        };
        var transforms = new[] { new Transform3D(Basis.Identity, new Vector3(1, 2, 3)), new Transform3D(Basis.Identity, new Vector3(4, 5, 6)) };

        var first = MultiMeshWriter.Build(source, transforms, append: false);
        AssertThat(source.InstanceCount).IsEqual(0);
        AssertThat(first.UseColors).IsTrue();
        AssertThat(first.UseCustomData).IsTrue();
        AssertThat(first.Mesh).IsSame(mesh);
        AssertThat(first.InstanceCount).IsEqual(2);
        AssertThat(first.Buffer.Length).IsEqual(40);
        AssertThat(InstanceBuffer.ReadTransform(first.Buffer, 20).Origin).IsEqual(new Vector3(4, 5, 6));

        var appended = MultiMeshWriter.Build(first, new[] { new Transform3D(Basis.Identity, new Vector3(7, 8, 9)) }, append: true);
        AssertThat(appended.InstanceCount).IsEqual(3);
        AssertThat(InstanceBuffer.ReadTransform(appended.Buffer, 0).Origin).IsEqual(new Vector3(1, 2, 3));
        AssertThat(InstanceBuffer.ReadTransform(appended.Buffer, 40).Origin).IsEqual(new Vector3(7, 8, 9));
        AssertThat(first.InstanceCount).IsEqual(2);

        var replaced = MultiMeshWriter.Build(appended, transforms, append: false);
        AssertThat(replaced.InstanceCount).IsEqual(2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Writer_converts_an_empty_2d_multimesh()
    {
        var source = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D };
        var result = MultiMeshWriter.Build(source, new[] { Transform3D.Identity }, append: false);
        AssertThat(result.TransformFormat).IsEqual(MultiMesh.TransformFormatEnum.Transform3D);
        AssertThat(result.InstanceCount).IsEqual(1);
    }
}
