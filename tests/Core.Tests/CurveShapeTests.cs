using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.Curves.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

// godoocurves brief §4.1, §8: the seven existing shapes, against exact formulas written separately here.
public class CurveShapeTests
{
    private readonly ITestOutputHelper _output;

    public CurveShapeTests(ITestOutputHelper output) => _output = output;

    private static ShapeCurve Build(ShapeKind kind, ShapeParameters p)
    {
        var result = Shapes.Build(kind, p);
        Assert.True(result.IsValid, result.Error);
        return result.Curve;
    }

    /// <summary>The exact shape, as a function of t from 0 to 1.</summary>
    private static Func<double, Vector3> Exact(ShapeKind kind, ShapeParameters p)
    {
        var b = p.Bend.Local();
        switch (kind)
        {
            case ShapeKind.Straight:
                return t => new Vector3((float)(t * p.Length), 0, 0);
            case ShapeKind.Parabolic:
                return t => new Vector3((float)(t * p.Length), (float)(4 * p.Height * t * (1 - t)), 0);
            case ShapeKind.Circle:
            case ShapeKind.Semicircle:
            {
                double angle = kind == ShapeKind.Circle ? Math.Tau : Math.PI;
                var axis = Vector3.Right.Cross(b);
                return t => b * p.Radius + (-b * p.Radius).Rotated(axis, (float)(t * angle));
            }
            case ShapeKind.Loop:
            {
                float r = p.Diameter / 2, m = p.Length / 2, loop = Mathf.Tau * r;
                double total = p.Length + loop;
                return t =>
                {
                    double d = t * total;
                    if (d <= m)
                        return new Vector3((float)d, 0, 0);
                    if (d >= m + loop)
                        return new Vector3((float)(d - loop), 0, p.SideShift);
                    double a = (d - m) / r;
                    return new Vector3(m + r * (float)Math.Sin(a), r - r * (float)Math.Cos(a), (float)(p.SideShift * a / Math.Tau));
                };
            }
            case ShapeKind.Wave:
                return t =>
                {
                    double x = t * p.Length;
                    return new Vector3((float)x, 0, 0) + b * (float)(p.Amplitude / 2 * (1 - Math.Cos(Math.Tau * p.Cycles * x / p.Length)));
                };
            case ShapeKind.Spiral:
            {
                double total = Math.Tau * p.Turns, ease = Math.Min(Math.PI, total / 2);
                return t =>
                {
                    double a = t * total;
                    double u = Math.Min(1, Math.Min(a, total - a) / ease);
                    double r = p.Radius * (3 * u * u - 2 * u * u * u);
                    return new Vector3((float)(t * p.Length), (float)(-r * Math.Cos(a)), (float)(-r * Math.Sin(a)));
                };
            }
        }
        throw new ArgumentOutOfRangeException(nameof(kind));
    }

    private static List<Vector3> CurveSamples(ShapeCurve curve, int perSegment = 40)
    {
        var samples = new List<Vector3>();
        for (int s = 0; s < curve.SegmentCount; s++)
            for (int i = 0; i <= perSegment; i++)
                samples.Add(curve.Sample(s, i / (float)perSegment));
        return samples;
    }

    private static float Distance(Vector3 p, IReadOnlyList<Vector3> polyline)
    {
        float best = float.MaxValue;
        for (int i = 0; i + 1 < polyline.Count; i++)
        {
            var a = polyline[i];
            var ab = polyline[i + 1] - a;
            float t = ab.LengthSquared() < 1e-12f ? 0 : Math.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0, 1);
            best = Math.Min(best, p.DistanceTo(a + ab * t));
        }
        return best;
    }

    /// <summary>The largest distance either way between the curve and the exact shape.</summary>
    private static float Deviation(ShapeCurve curve, Func<double, Vector3> exact)
    {
        var exactLine = Enumerable.Range(0, 4001).Select(i => exact(i / 4000.0)).ToList();
        var curveSamples = CurveSamples(curve);
        float there = curveSamples.Max(p => Distance(p, exactLine));
        float back = Enumerable.Range(0, 401).Select(i => exact(i / 400.0)).Max(p => Distance(p, curveSamples));
        return Math.Max(there, back);
    }

    public static IEnumerable<object[]> Cases()
    {
        var cases = new (ShapeKind Kind, ShapeParameters P)[]
        {
            (ShapeKind.Straight, new()),
            (ShapeKind.Straight, new() { Length = 250 }),
            (ShapeKind.Parabolic, new()),
            (ShapeKind.Parabolic, new() { Length = 30, Height = -2 }),
            (ShapeKind.Circle, new()),
            (ShapeKind.Circle, new() { Radius = 0.5f, Bend = Bend.Up }),
            (ShapeKind.Semicircle, new()),
            (ShapeKind.Semicircle, new() { Radius = 40, Bend = Bend.Left }),
            (ShapeKind.Loop, new()),
            (ShapeKind.Loop, new() { Length = 20, Diameter = 8, SideShift = 2 }),
            (ShapeKind.Loop, new() { Length = 4, Diameter = 10, SideShift = -1 }),
            (ShapeKind.Wave, new()),
            (ShapeKind.Wave, new() { Length = 40, Amplitude = 1, Cycles = 5, Bend = Bend.Up }),
            (ShapeKind.Wave, new() { Length = 6, Amplitude = 8, Cycles = 1, Bend = Bend.Left }),
            (ShapeKind.Spiral, new()),
            (ShapeKind.Spiral, new() { Length = 30, Radius = 4, Turns = 5 }),
            (ShapeKind.Spiral, new() { Length = 5, Radius = 1, Turns = 0.75f }),
        };
        return cases.Select((c, i) => new object[] { i, c.Kind, c.P });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_shape_stays_within_its_accuracy(int index, ShapeKind kind, ShapeParameters p)
    {
        var curve = Build(kind, p);
        float size = Shapes.Size(kind, p);
        float deviation = Deviation(curve, Exact(kind, p));
        _output.WriteLine($"case {index} {kind}: {curve.Points.Count} points, deviation {deviation / size:P4} of size {size}");
        Assert.True(deviation <= Shapes.Accuracy * size, $"{kind}: deviation {deviation} over {Shapes.Accuracy * size}");
    }

    private static float AngleDegrees(Vector3 a, Vector3 b) => Mathf.RadToDeg(a.AngleTo(b));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Every_shape_starts_along_the_direction(int index, ShapeKind kind, ShapeParameters p)
    {
        var curve = Build(kind, p);
        var expected = kind == ShapeKind.Parabolic ? new Vector3(p.Length, 4 * p.Height, 0).Normalized() : Vector3.Right;
        Assert.True(AngleDegrees(curve.StartTangent, expected) < 0.01f, $"case {index}: starts {curve.StartTangent}");
        Assert.True(curve.Points[0].Position.IsEqualApprox(Vector3.Zero));
        if (kind is ShapeKind.Loop or ShapeKind.Wave or ShapeKind.Spiral or ShapeKind.Straight)
            Assert.True(AngleDegrees(curve.EndTangent, Vector3.Right) < 0.01f, $"case {index}: ends {curve.EndTangent}");
    }

    [Fact]
    public void Shapes_end_where_their_definition_says()
    {
        Assert.True(Build(ShapeKind.Parabolic, new()).Points[^1].Position.IsEqualApprox(new Vector3(10, 0, 0)));
        var semi = Build(ShapeKind.Semicircle, new() { Radius = 3, Bend = Bend.Left });
        Assert.True(semi.Points[^1].Position.IsEqualApprox(new Vector3(0, 0, -6)));
        Assert.True(semi.EndTangent.IsEqualApprox(Vector3.Left));
        Assert.True(Build(ShapeKind.Loop, new() { Length = 12, SideShift = 1.5f }).Points[^1].Position.IsEqualApprox(new Vector3(12, 0, 1.5f)));
        Assert.True(Build(ShapeKind.Wave, new() { Length = 20 }).Points[^1].Position.IsEqualApprox(new Vector3(20, 0, 0)));
        Assert.True(Build(ShapeKind.Spiral, new() { Length = 20 }).Points[^1].Position.IsEqualApprox(new Vector3(20, 0, 0)));
    }

    [Fact]
    public void The_circle_is_closed_and_the_others_open()
    {
        var circle = Build(ShapeKind.Circle, new() { Radius = 2 });
        Assert.True(circle.Closed);
        Assert.Equal(4, circle.Points.Count);
        Assert.True(circle.Sample(circle.SegmentCount - 1, 1).IsEqualApprox(circle.Points[0].Position));
        foreach (var kind in Enum.GetValues<ShapeKind>().Where(k => k != ShapeKind.Circle))
            Assert.False(Build(kind, new()).Closed);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(2f)]
    public void The_loops_wanted_up_faces_its_centre_and_is_level_on_the_straights(float shift)
    {
        var p = new ShapeParameters { Length = 12, Diameter = 6, SideShift = shift };
        var curve = Build(ShapeKind.Loop, p);
        Assert.True(curve.Points[0].Up.IsEqualApprox(Vector3.Up));
        Assert.True(curve.Points[^1].Up.IsEqualApprox(Vector3.Up));
        foreach (var point in curve.Points.Skip(1).SkipLast(1))
        {
            var toCentre = new Vector3(6, 3, point.Position.Z) - point.Position;
            Assert.True(point.Up.AngleTo(toCentre) < 1e-3f, $"{point.Position}: up {point.Up}");
        }
    }

    [Fact]
    public void Vertical_arcs_turn_their_wanted_up_with_them()
    {
        var curve = Build(ShapeKind.Semicircle, new() { Radius = 2, Bend = Bend.Up });
        Assert.True(curve.Points[0].Up.IsEqualApprox(Vector3.Up));
        Assert.True(curve.Points[1].Up.IsEqualApprox(Vector3.Left));   // a quarter round, heading up: up faces back to the centre
        Assert.True(curve.Points[2].Up.IsEqualApprox(Vector3.Down));
    }

    [Theory]
    [InlineData(ShapeKind.Straight, "Length")]
    [InlineData(ShapeKind.Circle, "Radius")]
    [InlineData(ShapeKind.Loop, "Diameter")]
    [InlineData(ShapeKind.Wave, "Cycles")]
    [InlineData(ShapeKind.Spiral, "Turns")]
    public void Impossible_settings_are_errors(ShapeKind kind, string setting)
    {
        var p = setting switch
        {
            "Length" => new ShapeParameters { Length = 0 },
            "Radius" => new ShapeParameters { Radius = -1 },
            "Diameter" => new ShapeParameters { Diameter = 0 },
            "Cycles" => new ShapeParameters { Cycles = 0 },
            _ => new ShapeParameters { Turns = float.NaN },
        };
        var result = Shapes.Build(kind, p);
        Assert.False(result.IsValid);
        Assert.StartsWith(setting, result.Error);
    }
}
