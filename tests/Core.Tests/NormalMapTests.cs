using System;
using System.Linq;
using Godot;
using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class NormalMapTests
{
    /// <summary>Quad facing +Y with Godot PlaneMesh-style tangents: +X (along +U), binormal sign +1.</summary>
    private static SurfaceData Plane(Transform3D? transform = null)
    {
        var quad = TestMeshes.Quad(10);
        quad.Tangents = Enumerable.Range(0, 4).SelectMany(_ => new[] { 1f, 0f, 0f, 1f }).ToArray();
        return SurfaceData.Build(new[] { quad }, transform ?? Transform3D.Identity);
    }

    /// <summary>A 2×2 map holding one encoded vector, quantised to 8 bits like a real texture.</summary>
    private static NormalMap Constant(Vector3 v, bool flipGreen = false)
    {
        byte Encode(float c) => (byte)Math.Round((c + 1f) / 2f * 255f);
        var rgba = Enumerable.Range(0, 4).SelectMany(_ => new[] { Encode(v.X), Encode(v.Y), Encode(v.Z), (byte)255 }).ToArray();
        return NormalMap.FromRgba8(2, 2, rgba, flipGreen);
    }

    private static Transform3D[] Run(SurfaceData surface, NormalMap map, NormalMapSpace space = NormalMapSpace.Tangent,
        float strength = 1f, NormalSource source = NormalSource.NormalMap) =>
        ScatterPipeline.Run(surface, new ScatterOptions
        {
            Amount = 200, Seed = 5, Alignment = 0, RandomScale = 0, NormalSource = source,
            NormalMap = new NormalMapOptions { Map = map, Space = space, Strength = strength },
        }).Transforms;

    private static float Degrees(Vector3 a, Vector3 b) => Mathf.RadToDeg(a.AngleTo(b));

    private static Vector3 Tilted(float degrees, Vector3 axis) =>
        Mathf.Sin(Mathf.DegToRad(degrees)) * axis + Mathf.Cos(Mathf.DegToRad(degrees)) * Vector3.Back; // z = blue

    // Brief §8: a flat normal map (RGB 128, 128, 255) gives the same result as no normal map.
    [Fact]
    public void Neutral_map_matches_no_map()
    {
        var with = Run(Plane(), NormalMap.FromRgba8(1, 1, new byte[] { 128, 128, 255, 255 }, false));
        var without = Run(Plane(), null, source: NormalSource.Smooth);
        Assert.Equal(without.Length, with.Length);
        for (int i = 0; i < with.Length; i++)
        {
            Assert.Equal(without[i].Origin, with[i].Origin);
            Assert.True(Degrees(with[i].Basis.Y, without[i].Basis.Y) < 0.5f);
        }
    }

    // Brief §8: a constant tilted map tilts every instance by the expected angle (±1°). The other
    // axis is 128/255 → 0.0039 after 8-bit encoding, hence the 0.01 tolerance there.
    [Fact]
    public void Red_tilt_leans_toward_the_tangent()
    {
        foreach (var t in Run(Plane(), Constant(Tilted(20, Vector3.Right))))
        {
            Assert.InRange(Degrees(t.Basis.Y, Vector3.Up), 19f, 21f);
            Assert.True(t.Basis.Y.X > 0 && Mathf.Abs(t.Basis.Y.Z) < 0.01f, t.Basis.Y.ToString());
        }
    }

    // Brief §8: with and without green flip. Godot's binormal is cross(N, T) * sign = -Z here, so a
    // positive green (OpenGL) leans toward -Z, the top of the image; flipping green leans toward +Z.
    [Fact]
    public void Green_tilt_follows_the_binormal_and_flips()
    {
        var map = Tilted(20, Vector3.Up); // texel (0, sin, cos)
        foreach (var t in Run(Plane(), Constant(map)))
        {
            Assert.InRange(Degrees(t.Basis.Y, Vector3.Up), 19f, 21f);
            Assert.True(t.Basis.Y.Z < 0 && Mathf.Abs(t.Basis.Y.X) < 0.01f, t.Basis.Y.ToString());
        }
        foreach (var t in Run(Plane(), Constant(map, flipGreen: true)))
        {
            Assert.InRange(Degrees(t.Basis.Y, Vector3.Up), 19f, 21f);
            Assert.True(t.Basis.Y.Z > 0, t.Basis.Y.ToString());
        }
    }

    [Fact]
    public void Strength_scales_the_effect()
    {
        var map = Constant(Tilted(40, Vector3.Right));
        Assert.All(Run(Plane(), map, strength: 0f), t => Assert.True(Degrees(t.Basis.Y, Vector3.Up) < 0.01f));
        Assert.All(Run(Plane(), map, strength: 0.5f), t => Assert.InRange(Degrees(t.Basis.Y, Vector3.Up), 15f, 25f));
    }

    [Fact]
    public void Object_space_maps_use_the_mesh_basis()
    {
        // Mesh rotated 90° about X: local +Y faces world +Z, and local +X stays world +X.
        var rotated = new Transform3D(Basis.Identity.Rotated(Vector3.Right, Mathf.Pi / 2), Vector3.Zero);
        foreach (var t in Run(Plane(rotated), Constant(Vector3.Right), NormalMapSpace.Object))
            Assert.True(Degrees(t.Basis.Y, Vector3.Right) < 1f, t.Basis.Y.ToString());
    }

    [Fact]
    public void Face_source_ignores_the_map_and_missing_tangents_fall_back()
    {
        var map = Constant(Tilted(20, Vector3.Right));
        Assert.All(Run(Plane(), map, source: NormalSource.Face), t => Assert.True(Degrees(t.Basis.Y, Vector3.Up) < 0.01f));
        // Without tangent data a fixed perpendicular is used; still a valid, 20° tilt.
        Assert.All(Run(TestMeshes.Plane(10), map), t => Assert.InRange(Degrees(t.Basis.Y, Vector3.Up), 19f, 21f));
    }
}
