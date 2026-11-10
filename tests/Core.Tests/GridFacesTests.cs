using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §10.1: triangles on a cell's boundary faces.
public class GridFacesTests
{
    private static readonly Vector3 Min = new(2, 0, -4);
    private static readonly Vector3 Size = new(2, 1, 2);

    [Theory]
    [InlineData(0, 4, 0.2f, -3.5f, 4, 0.8f, -2.2f, 4, 0.5f, -3)]   // x = 4: +X
    [InlineData(1, 2, 0.1f, -3.9f, 2, 0.9f, -2.1f, 2, 0.4f, -3)]   // x = 2: -X
    [InlineData(2, 2.5f, 1, -3.5f, 3.5f, 1, -3.5f, 3, 1, -2.5f)]   // y = 1: +Y (top)
    [InlineData(3, 2.5f, 0, -3.5f, 3.5f, 0, -3.5f, 3, 0, -2.5f)]   // y = 0: -Y (bottom)
    [InlineData(4, 2.5f, 0.1f, -2, 3.5f, 0.9f, -2, 3, 0.5f, -2)]   // z = -2: +Z
    [InlineData(5, 2.5f, 0.1f, -4, 3.5f, 0.9f, -4, 3, 0.5f, -4)]   // z = -4: -Z
    public void ATriangleOnAFaceIsFound(int face, float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
    {
        Assert.Equal(face, GridFaces.BoundaryFace(new(ax, ay, az), new(bx, by, bz), new(cx, cy, cz), Min, Size));
        Assert.Equal(GridFaces.Offsets[face], -GridFaces.Offsets[face ^ 1]);
    }

    [Fact]
    public void TrianglesInsideTheCellAreNotOnAFace()
    {
        // A ramp's slope from the bottom of -X to the top of +X.
        Assert.Equal(-1, GridFaces.BoundaryFace(new(2, 0, -4), new(4, 1, -4), new(4, 1, -2), Min, Size));
        // A top that does not reach the cell's top (a half-height slab).
        Assert.Equal(-1, GridFaces.BoundaryFace(new(2, 0.5f, -4), new(4, 0.5f, -4), new(4, 0.5f, -2), Min, Size));
        // Only two vertices on the +X plane.
        Assert.Equal(-1, GridFaces.BoundaryFace(new(4, 0, -4), new(4, 1, -4), new(3, 1, -2), Min, Size));
    }

    [Fact]
    public void SmallErrorsFromTransformsAreTolerated()
    {
        const float e = 5e-4f;
        Assert.Equal(2, GridFaces.BoundaryFace(new(2, 1 + e, -4), new(4, 1 - e, -4), new(4, 1, -2), Min, Size));
        Assert.Equal(-1, GridFaces.BoundaryFace(new(2, 1.01f, -4), new(4, 1, -4), new(4, 1, -2), Min, Size));
    }
}
