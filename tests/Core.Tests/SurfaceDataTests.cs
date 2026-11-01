using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class SurfaceDataTests
{
    [Fact]
    public void Indexed_and_non_indexed_quads_give_the_same_triangles()
    {
        var indexed = SurfaceData.Build(new[] { TestMeshes.Quad(2) }, Transform3D.Identity);
        var flat = SurfaceData.Build(new[] { TestMeshes.QuadNonIndexed(2) }, Transform3D.Identity);
        Assert.Equal(2, indexed.TriangleCount);
        Assert.Equal(2, flat.TriangleCount);
        for (int t = 0; t < 2; t++)
        {
            Assert.Equal(indexed.Areas[t], flat.Areas[t], 5);
            Assert.True(indexed.FaceNormals[t].IsEqualApprox(flat.FaceNormals[t]));
        }
        Assert.Equal(4f, indexed.TotalArea, 5);
    }

    [Fact]
    public void Clockwise_triangles_face_their_vertex_normals()
    {
        var data = TestMeshes.Plane();
        foreach (var n in data.FaceNormals)
            Assert.True(n.IsEqualApprox(Vector3.Up), $"face normal {n}");
    }

    [Fact]
    public void Multiple_surfaces_are_concatenated()
    {
        var data = SurfaceData.Build(new[] { TestMeshes.Quad(1), TestMeshes.QuadNonIndexed(3) }, Transform3D.Identity);
        Assert.Equal(4, data.TriangleCount);
        Assert.Equal(10f, data.TotalArea, 4);
        // Second surface indices point past the first surface's vertices.
        Assert.True(data.Indices[6] >= 4);
        Assert.True(data.HasUv);
        Assert.False(data.HasUv2);
    }

    [Fact]
    public void Areas_are_measured_in_world_space()
    {
        var stretch = new Transform3D(Basis.FromScale(new Vector3(4, 1, 0.5f)), new Vector3(10, 0, 0));
        var data = SurfaceData.Build(new[] { TestMeshes.Quad(1) }, stretch);
        Assert.Equal(2f, data.TotalArea, 4);
        Assert.Equal(1f, data.Areas[0], 4);
        Assert.True(data.Positions[0].IsEqualApprox(new Vector3(8, 0, -0.25f)));
    }

    [Fact]
    public void Normals_use_the_inverse_transpose_and_stay_outward_when_mirrored()
    {
        var shear = new Basis(new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 0, 1));
        var mirror = Basis.FromScale(new Vector3(-1, 1, 1));
        foreach (var basis in new[] { shear, mirror, mirror * Basis.FromScale(new Vector3(1, -1, 1)) })
        {
            var data = SurfaceData.Build(new[] { TestMeshes.Quad(1) }, new Transform3D(basis, Vector3.Zero));
            for (int t = 0; t < data.TriangleCount; t++)
            {
                var smooth = data.SmoothNormalAt(t, new Vector3(1f / 3, 1f / 3, 1f / 3));
                Assert.True(data.FaceNormals[t].Dot(smooth) > 0.999f, $"basis {basis}: face {data.FaceNormals[t]} vs vertex {smooth}");
            }
        }
    }

    [Fact]
    public void Missing_normals_fall_back_to_face_normals()
    {
        var quad = TestMeshes.Quad(1);
        quad.Normals = null;
        var data = SurfaceData.Build(new[] { quad }, Transform3D.Identity);
        Assert.False(data.HasNormals);
        Assert.True(data.SmoothNormalAt(0, new Vector3(1, 0, 0)).IsEqualApprox(Vector3.Up));
    }
}
