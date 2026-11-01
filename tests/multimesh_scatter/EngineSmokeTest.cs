namespace Godoodads.Tests.MultiMeshScatter;

using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

// Guards which engine APIs keep their data in a headless test run (dummy rendering server).
[TestSuite]
public class EngineSmokeTest
{
    [TestCase]
    [RequireGodotRuntime]
    public void ArrayMesh_surface_arrays_survive_headless()
    {
        var mesh = new PlaneMesh { Size = new Vector2(2, 2) };
        var arrays = mesh.SurfaceGetArrays(0);
        var verts = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        AssertThat(verts.Length).IsGreater(0);

        var am = new ArrayMesh();
        am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var back = am.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        AssertThat(back.Length).IsEqual(verts.Length);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MultiMesh_buffer_survives_headless()
    {
        // GetInstanceTransform returns zeros under the dummy rendering server; Buffer round-trips.
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, InstanceCount = 2 };
        var buf = new float[24];
        buf[3] = 5f;
        mm.Buffer = buf;
        AssertThat(mm.Buffer.Length).IsEqual(24);
        AssertThat(mm.Buffer[3]).IsEqual(5f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Image_pixels_survive_headless()
    {
        var img = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        img.SetPixel(1, 2, Colors.Red);
        AssertThat(img.GetPixel(1, 2)).IsEqual(Colors.Red);
    }
}
