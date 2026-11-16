using System;
using System.Linq;
using Godot;
using Godoodads.Curves.Core;
using Xunit;

namespace Godoodads.Tests;

// godoocurves brief §4.1, §4.2: frames, exact arcs and the fitter every shape is built from.
public class CurveSegmentTests
{
    private static Vector3 Up(Vector3 radial, float fraction) => Vector3.Up;

    [Fact]
    public void A_frame_makes_up_perpendicular_and_side_the_right_hand()
    {
        Assert.True(ShapeFrame.TryCreate(new Vector3(1, 2, 3), new Vector3(2, 0, 0), new Vector3(1, 1, 0), out var frame, out _));
        Assert.Equal(Vector3.Right, frame.Forward);
        Assert.Equal(Vector3.Up, frame.Up);
        Assert.Equal(Vector3.Back, frame.Side);   // +X forward, +Y up: the right is +Z
        Assert.Equal(new Vector3(1 + 4, 2 + 5, 3 + 6), frame.ToPath(new Vector3(4, 5, 6)));

        // A Godot-style frame: heading -Z with +Y up has its right at +X.
        Assert.True(ShapeFrame.TryCreate(Vector3.Zero, Vector3.Forward, Vector3.Up, out var godot, out _));
        Assert.True(godot.Side.IsEqualApprox(Vector3.Right));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 1, 0, "Direction is zero")]
    [InlineData(1, 0, 0, 0, 0, 0, "Up is zero or parallel")]
    [InlineData(1, 0, 0, -3, 0, 0, "Up is zero or parallel")]
    public void A_zero_or_parallel_frame_is_an_error(float dx, float dy, float dz, float ux, float uy, float uz, string message)
    {
        Assert.False(ShapeFrame.TryCreate(Vector3.Zero, new Vector3(dx, dy, dz), new Vector3(ux, uy, uz), out _, out string error));
        Assert.StartsWith(message, error);
    }

    [Fact]
    public void A_quarter_arc_has_the_exact_handles()
    {
        var builder = new CurveBuilder();
        int segments = Arcs.Add(builder, Vector3.Zero, new Vector3(0, -2, 0), Vector3.Back, Mathf.Pi / 2, Vector3.Zero, Up);
        var curve = builder.Build(false);
        Assert.Equal(1, segments);
        Assert.Equal(2, curve.Points.Count);
        Assert.Equal(0.5523f * 2, curve.Points[0].Out.Length(), 3);
        Assert.Equal(4f / 3f * Mathf.Tan(Mathf.Pi / 8) * 2, curve.Points[1].In.Length(), 5);
        // Turning around +Z from straight below: heads +X first, ends at +X of the centre.
        Assert.True(curve.StartTangent.IsEqualApprox(Vector3.Right));
        Assert.True(curve.Points[1].Position.IsEqualApprox(new Vector3(2, 0, 0)));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(135)]
    [InlineData(180)]
    [InlineData(270)]
    [InlineData(360)]
    [InlineData(-200)]
    public void Arcs_stay_on_the_circle(float degrees)
    {
        const float r = 5;
        var builder = new CurveBuilder();
        Arcs.Add(builder, Vector3.Zero, new Vector3(r, 0, 0), Vector3.Up, Mathf.DegToRad(degrees), Vector3.Zero, Up);
        var curve = builder.Build(false);
        Assert.Equal((int)Math.Ceiling(Math.Abs(degrees) / 90 - 1e-6), curve.SegmentCount);
        for (int s = 0; s < curve.SegmentCount; s++)
            for (int i = 0; i <= 50; i++)
                Assert.InRange(curve.Sample(s, i / 50f).Length(), r * (1 - 3e-4f), r * (1 + 3e-4f));
        Assert.InRange(curve.Length, r * Mathf.DegToRad(Math.Abs(degrees)) * 0.9995, r * Mathf.DegToRad(Math.Abs(degrees)) * 1.0005);
    }

    [Fact]
    public void An_arc_with_drift_moves_linearly_along_the_axis()
    {
        var builder = new CurveBuilder();
        Arcs.Add(builder, new Vector3(0, 3, 0), new Vector3(0, -3, 0), Vector3.Back, Mathf.Tau, new Vector3(0, 0, 2), Up);
        var curve = builder.Build(false);
        Assert.True(curve.Points[^1].Position.IsEqualApprox(new Vector3(0, 0, 2)));
        // Halfway round (the top), the drift is half done; the radius in the XY plane stays 3.
        var top = curve.Points[2].Position;
        Assert.True(top.IsEqualApprox(new Vector3(0, 6, 1)));
        for (int s = 0; s < curve.SegmentCount; s++)
            for (int i = 0; i <= 20; i++)
            {
                var p = curve.Sample(s, i / 20f);
                Assert.InRange(new Vector2(p.X, p.Y - 3).Length(), 3 * (1 - 3e-4f), 3 * (1 + 3e-4f));
            }
    }

    [Fact]
    public void The_fitter_meets_its_tolerance_with_the_fewest_segments()
    {
        Vector3 Sine(double t) => new((float)t, (float)Math.Sin(t), 0);
        Vector3 SineD(double t) => new(1, (float)Math.Cos(t), 0);
        const float tolerance = 1e-3f;
        var builder = new CurveBuilder();
        int n = Fitter.Add(builder, Sine, SineD, 0, Math.Tau, tolerance, _ => Vector3.Up);
        Assert.True(Fitter.Error(Sine, SineD, 0, Math.Tau, n) <= tolerance);
        Assert.True(Fitter.Error(Sine, SineD, 0, Math.Tau, n - 1) > tolerance);
        Assert.InRange(n, 4, 12);

        var curve = builder.Build(false);
        Assert.Equal(n + 1, curve.Points.Count);
        for (int s = 0; s < curve.SegmentCount; s++)
            for (int i = 0; i <= 40; i++)
            {
                var p = curve.Sample(s, i / 40f);
                Assert.True(Math.Abs(p.Y - Math.Sin(p.X)) < tolerance * 2);
            }
    }

    [Fact]
    public void The_fitter_handles_a_helix()
    {
        Vector3 Helix(double t) => new((float)Math.Cos(t) * 2, (float)Math.Sin(t) * 2, (float)t);
        Vector3 HelixD(double t) => new(-(float)Math.Sin(t) * 2, (float)Math.Cos(t) * 2, 1);
        int n = Fitter.Add(new CurveBuilder(), Helix, HelixD, 0, 3 * Math.Tau, 2e-3f, _ => Vector3.Up);
        Assert.True(Fitter.Error(Helix, HelixD, 0, 3 * Math.Tau, n) <= 2e-3f);
        Assert.InRange(n, 8, 48);
    }

    [Fact]
    public void Pieces_that_meet_merge_and_open_ends_have_no_outer_handles()
    {
        var builder = new CurveBuilder();
        builder.Add(new CurvePoint(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Up));
        builder.LineTo(new Vector3(4, 0, 0), Vector3.Up);
        Arcs.Add(builder, new Vector3(4, 2, 0), new Vector3(0, -2, 0), Vector3.Back, Mathf.Pi, Vector3.Zero, Up);
        var curve = builder.Build(false);
        Assert.Equal(1 + 1 + 2, curve.Points.Count);           // start, the line's end = the arc's start, two arc points
        Assert.Equal(Vector3.Zero, curve.Points[1].In);         // the straight comes in without a handle
        Assert.True(curve.Points[1].Out.Length() > 0);          // and the arc leaves with one
        Assert.Equal(Vector3.Zero, curve.Points[^1].Out);
        Assert.True(curve.EndTangent.IsEqualApprox(Vector3.Left));
    }

    [Fact]
    public void A_full_circle_closes_onto_its_first_point()
    {
        var builder = new CurveBuilder();
        Arcs.Add(builder, Vector3.Zero, new Vector3(3, 0, 0), Vector3.Up, Mathf.Tau, Vector3.Zero, Up);
        var curve = builder.Build(true);
        Assert.True(curve.Closed);
        Assert.Equal(4, curve.Points.Count);
        Assert.Equal(4, curve.SegmentCount);
        Assert.True(curve.Points[0].In.Length() > 0);
        Assert.InRange(curve.Length, Mathf.Tau * 3 * 0.9995, Mathf.Tau * 3 * 1.0005);
        Assert.True(curve.Sample(3, 1).IsEqualApprox(curve.Points[0].Position));
    }

    [Fact]
    public void A_curve_maps_to_the_path_with_its_handles_and_ups()
    {
        var builder = new CurveBuilder();
        builder.Add(new CurvePoint(Vector3.Zero, Vector3.Zero, new Vector3(1, 0, 0), Vector3.Up));
        builder.Add(new CurvePoint(new Vector3(3, 0, 0), new Vector3(-1, 0, 0), Vector3.Zero, Vector3.Back));
        ShapeFrame.TryCreate(new Vector3(0, 5, 0), Vector3.Forward, Vector3.Up, out var frame, out _);
        var path = builder.Build(false).ToPath(frame);
        Assert.True(path.Points[1].Position.IsEqualApprox(new Vector3(0, 5, -3)));
        Assert.True(path.Points[1].In.IsEqualApprox(new Vector3(0, 0, 1)));
        Assert.True(path.Points[1].Up.IsEqualApprox(Vector3.Right));   // local side = the frame's right
        Assert.True(path.StartTangent.IsEqualApprox(Vector3.Forward));
    }
}
