using Godot;
using Xunit;

namespace Godoodads.Tests;

// Guards the assumption that GodotSharp value types work without a running engine.
public class GodotMathSmokeTests
{
    [Fact]
    public void Vector3_and_Quaternion_work_outside_the_engine()
    {
        var q = new Quaternion(Vector3.Up, Mathf.Pi / 2);
        var v = q * Vector3.Right;
        Assert.True(v.IsEqualApprox(Vector3.Forward));
        Assert.True(new Basis(q).IsEqualApprox(Basis.Identity.Rotated(Vector3.Up, Mathf.Pi / 2)));
    }

    [Fact]
    public void Arc_quaternion_handles_opposite_vectors()
    {
        var q = new Quaternion(Vector3.Up, Vector3.Down);
        var v = q * Vector3.Up;
        Assert.False(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z));
        Assert.True(v.IsEqualApprox(Vector3.Down));
    }
}
