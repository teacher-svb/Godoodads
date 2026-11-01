using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class InstanceBufferTests
{
    [Fact]
    public void Transform_layout_is_row_major_3x4()
    {
        var t = new Transform3D(new Basis(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9)), new Vector3(10, 11, 12));
        var buffer = new float[12];
        InstanceBuffer.WriteTransform(buffer, 0, t);
        Assert.Equal(new float[] { 1, 4, 7, 10, 2, 5, 8, 11, 3, 6, 9, 12 }, buffer);
        Assert.True(InstanceBuffer.ReadTransform(buffer, 0).IsEqualApprox(t));
    }

    [Fact]
    public void Compose_keeps_existing_data_and_defaults_new_colors_to_white()
    {
        int stride = InstanceBuffer.Stride(useColors: true, useCustomData: true);
        Assert.Equal(20, stride);
        var existing = new float[stride];
        for (int i = 0; i < stride; i++)
            existing[i] = i + 100;

        var added = new[] { new Transform3D(Basis.Identity, new Vector3(1, 2, 3)) };
        var buffer = InstanceBuffer.Compose(existing, 1, added, true, true);

        Assert.Equal(2 * stride, buffer.Length);
        for (int i = 0; i < stride; i++)
            Assert.Equal(i + 100, buffer[i]);
        Assert.Equal(new Vector3(1, 2, 3), InstanceBuffer.ReadTransform(buffer, stride).Origin);
        Assert.Equal(new float[] { 1, 1, 1, 1 }, buffer[(stride + 12)..(stride + 16)]);
        Assert.Equal(new float[] { 0, 0, 0, 0 }, buffer[(stride + 16)..(stride + 20)]);
    }
}
