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
                    double a = (d - m) / r, u = a / Math.Tau;
                    return new Vector3(m + r * (float)Math.Sin(a), r - r * (float)Math.Cos(a), (float)(p.SideShift * u * u * (3 - 2 * u)));
                };
            }
            case ShapeKind.Wave:
                return t =>
                {
                    double x = t * p.Length;
                    return new Vector3((float)x, 0, 0) + b * (float)(p.Amplitude / 2 * (1 - Math.Cos(Math.Tau * p.Cycles * x / p.Length)));
                };
            case ShapeKind.Arc:
            {
                var axis = Vector3.Right.Cross(b);
                double angle = Mathf.DegToRad(p.AngleDegrees);
                return t => b * p.Radius + (-b * p.Radius).Rotated(axis, (float)(t * angle));
            }
            case ShapeKind.SBend:
            {
                var axis = Vector3.Right.Cross(b);
                double angle = 2 * Math.Atan(p.Offset / p.Length), r = p.Length / (2 * Math.Sin(angle));
                var end = new Vector3(p.Length, 0, 0) + b * p.Offset;
                return t => t <= 0.5
                    ? b * (float)r + (-b * (float)r).Rotated(axis, (float)(2 * t * angle))
                    : end - b * (float)r + (b * (float)r).Rotated(axis, (float)((2 - 2 * t) * angle));
            }
            case ShapeKind.Ellipse:
                return t => new Vector3(p.RadiusForward * (float)Math.Sin(t * Math.Tau), 0, 0)
                    + b * (p.RadiusSide * (float)(1 - Math.Cos(t * Math.Tau)));
            case ShapeKind.Helix:
            {
                var axis = Vector3.Right.Cross(b);
                double total = Math.Tau * p.Turns;
                return t => b * p.Radius + (-b * p.Radius).Rotated(axis, (float)(t * total)) + new Vector3(0, (float)(t * p.Rise), 0);
            }
            case ShapeKind.BankedTurn:
                return Exact(ShapeKind.Arc, p);
            case ShapeKind.FigureEight:
            {
                // By arc length: 135° round the first lobe, a straight, 270° round the second, a straight, 135°.
                double r = p.Radius, arc1 = 0.75 * Math.PI * r, line = 2 * r, arc2 = 1.5 * Math.PI * r, total = 2 * arc1 + 2 * line + arc2;
                var axis = Vector3.Right.Cross(b);
                Vector3 L(double u, double w) => new Vector3((float)u, 0, 0) + b * (float)w;
                var c1 = L(0, r); var c2 = L(0, r * (1 + 2 * Math.Sqrt(2)));
                double d = r / Math.Sqrt(2);
                return t =>
                {
                    double s = t * total;
                    if (s <= arc1) return c1 + L(0, -r).Rotated(axis, (float)(s / r));
                    s -= arc1;
                    if (s <= line) return (c1 + L(d, d)).Lerp(c2 + L(-d, -d), (float)(s / line));
                    s -= line;
                    if (s <= arc2) return c2 + L(-d, -d).Rotated(axis, (float)(-s / r));
                    s -= arc2;
                    if (s <= line) return (c2 + L(d, -d)).Lerp(c1 + L(-d, d), (float)(s / line));
                    s -= line;
                    return c1 + L(-d, d).Rotated(axis, (float)(s / r));
                };
            }
            case ShapeKind.Spiral:
            case ShapeKind.Corkscrew:
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
            (ShapeKind.Arc, new()),
            (ShapeKind.Arc, new() { Radius = 12, AngleDegrees = 30, Bend = Bend.Up }),
            (ShapeKind.Arc, new() { Radius = 2, AngleDegrees = 250, Bend = Bend.Left }),
            (ShapeKind.Arc, new() { Radius = 3, AngleDegrees = 360 }),
            (ShapeKind.SBend, new()),
            (ShapeKind.SBend, new() { Length = 40, Offset = 1, Bend = Bend.Left }),
            (ShapeKind.SBend, new() { Length = 4, Offset = 10, Bend = Bend.Up }),
            (ShapeKind.Ellipse, new()),
            (ShapeKind.Ellipse, new() { RadiusForward = 1, RadiusSide = 9, Bend = Bend.Down }),
            (ShapeKind.Ellipse, new() { RadiusForward = 20, RadiusSide = 0.5f, Bend = Bend.Left }),
            (ShapeKind.Helix, new()),
            (ShapeKind.Helix, new() { Radius = 3, Turns = 3.5f, Rise = -6, Bend = Bend.Left }),
            (ShapeKind.Corkscrew, new()),
            (ShapeKind.Corkscrew, new() { Length = 16, Radius = 1.5f, Turns = 1 }),
            (ShapeKind.BankedTurn, new()),
            (ShapeKind.BankedTurn, new() { Radius = 20, AngleDegrees = 180, BankDegrees = -45, Bend = Bend.Left }),
            (ShapeKind.FigureEight, new()),
            (ShapeKind.FigureEight, new() { Radius = 1.5f, Bend = Bend.Up }),
            (ShapeKind.FigureEight, new() { Radius = 30, Bend = Bend.Left }),
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
        var expected = kind switch
        {
            ShapeKind.Parabolic => new Vector3(p.Length, 4 * p.Height, 0).Normalized(),
            ShapeKind.Helix => (Vector3.Right * (Mathf.Tau * p.Turns * p.Radius) + Vector3.Up * p.Rise).Normalized(),
            _ => Vector3.Right,
        };
        Assert.True(AngleDegrees(curve.StartTangent, expected) < 0.01f, $"case {index}: starts {curve.StartTangent}");
        Assert.True(curve.Points[0].Position.IsEqualApprox(Vector3.Zero));
        if (kind is ShapeKind.Loop or ShapeKind.Wave or ShapeKind.Spiral or ShapeKind.Straight or ShapeKind.SBend or ShapeKind.Corkscrew)
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
        foreach (var kind in Enum.GetValues<ShapeKind>())
            Assert.Equal(Shapes.IsClosed(kind, new()), Build(kind, new()).Closed);
        Assert.True(Shapes.IsClosed(ShapeKind.Ellipse, new()));
        Assert.False(Shapes.IsClosed(ShapeKind.Arc, new()));
        Assert.True(Build(ShapeKind.Arc, new() { AngleDegrees = 360 }).Closed);
    }

    [Theory]
    [InlineData(10f, 3f, Bend.Right)]
    [InlineData(40f, 1f, Bend.Left)]
    [InlineData(4f, 10f, Bend.Up)]
    public void An_s_bend_ends_parallel_to_its_start_offset_to_the_side(float length, float offset, Bend bend)
    {
        var curve = Build(ShapeKind.SBend, new() { Length = length, Offset = offset, Bend = bend });
        Assert.True(curve.Points[^1].Position.DistanceTo(new Vector3(length, 0, 0) + bend.Local() * offset) < 1e-4f * length);
        Assert.True(AngleDegrees(curve.EndTangent, Vector3.Right) < 0.01f);
        Assert.True(curve.Points[^1].Up.IsEqualApprox(Vector3.Up));
    }

    [Fact]
    public void An_ellipse_with_equal_radii_is_the_circle()
    {
        var ellipse = Build(ShapeKind.Ellipse, new() { RadiusForward = 4, RadiusSide = 4, Bend = Bend.Up });
        var circle = new ShapeParameters { Radius = 4, Bend = Bend.Up };
        Assert.True(Deviation(ellipse, Exact(ShapeKind.Circle, circle)) <= Shapes.Accuracy * 8);
        Assert.True(ellipse.Sample(ellipse.SegmentCount - 1, 1).IsEqualApprox(ellipse.Points[0].Position));
        // Its wanted up turns with it, as the circle's does: at the top it points down, to the centre.
        var top = ellipse.Points.OrderByDescending(q => q.Position.Y).First();
        Assert.True(top.Up.IsEqualApprox(Vector3.Down));
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
    [InlineData(ShapeKind.Arc, "Angle")]
    [InlineData(ShapeKind.SBend, "Offset")]
    [InlineData(ShapeKind.Ellipse, "Radius Side")]
    [InlineData(ShapeKind.Helix, "Bend")]
    [InlineData(ShapeKind.BankedTurn, "Bank")]
    [InlineData(ShapeKind.RoundedRectangle, "Corner Radius")]
    [InlineData(ShapeKind.Polygon, "Sides")]
    public void Impossible_settings_are_errors(ShapeKind kind, string setting)
    {
        var p = setting switch
        {
            "Length" => new ShapeParameters { Length = 0 },
            "Radius" => new ShapeParameters { Radius = -1 },
            "Diameter" => new ShapeParameters { Diameter = 0 },
            "Cycles" => new ShapeParameters { Cycles = 0 },
            "Angle" => new ShapeParameters { AngleDegrees = -90 },
            "Offset" => new ShapeParameters { Offset = 0 },
            "Radius Side" => new ShapeParameters { RadiusSide = 0 },
            "Bend" => new ShapeParameters { Bend = Bend.Up },
            "Bank" => new ShapeParameters { BankDegrees = 95 },
            "Corner Radius" => new ShapeParameters { CornerRadius = 4 },
            "Sides" => new ShapeParameters { Sides = 2 },
            _ => new ShapeParameters { Turns = float.NaN },
        };
        var result = Shapes.Build(kind, p);
        Assert.False(result.IsValid);
        Assert.StartsWith(setting, result.Error);
    }

    [Fact]
    public void A_corkscrew_needs_whole_turns()
    {
        Assert.StartsWith("Turns must be a whole number", Shapes.Build(ShapeKind.Corkscrew, new() { Turns = 1.5f }).Error);
        Assert.True(Shapes.Build(ShapeKind.Spiral, new() { Turns = 1.5f }).IsValid);
    }

    [Fact]
    public void A_corkscrew_faces_its_axis_and_ends_upright()
    {
        var p = new ShapeParameters { Length = 12, Radius = 2, Turns = 2 };
        var curve = Build(ShapeKind.Corkscrew, p);
        Assert.True(curve.Points[0].Up.IsEqualApprox(Vector3.Up));
        Assert.True(curve.Points[^1].Up.IsEqualApprox(Vector3.Up));
        int checkedPoints = 0;
        foreach (var point in curve.Points)
        {
            var toAxis = new Vector3(point.Position.X, 0, 0) - point.Position;
            if (toAxis.Length() < 0.2f * p.Radius)
                continue;   // near the eased ends the axis is too close to give a direction
            Assert.True(point.Up.AngleTo(toAxis) < 1e-3f, $"{point.Position}: up {point.Up}");
            checkedPoints++;
        }
        Assert.True(checkedPoints >= 8);
    }

    [Fact]
    public void A_helix_climbs_its_rise_at_a_constant_slope()
    {
        var p = new ShapeParameters { Radius = 4, Turns = 2.25f, Rise = 9 };
        var curve = Build(ShapeKind.Helix, p);
        Assert.Equal(9f, curve.Points[^1].Position.Y, 4);
        Assert.Equal(curve.StartTangent.Y, curve.EndTangent.Y, 4);
    }

    [Fact]
    public void A_banked_turn_rolls_into_the_turn_and_back()
    {
        var p = new ShapeParameters { Radius = 10, AngleDegrees = 90, BankDegrees = 30, Bend = Bend.Right };
        var curve = Build(ShapeKind.BankedTurn, p);
        Assert.True(curve.Points.Count >= 17);
        Assert.True(curve.Points[0].Up.IsEqualApprox(Vector3.Up));
        Assert.True(curve.Points[^1].Up.IsEqualApprox(Vector3.Up));
        var middle = curve.Points[curve.Points.Count / 2];
        Assert.Equal(30f, Mathf.RadToDeg(middle.Up.AngleTo(Vector3.Up)), 2);
        var toCentre = new Vector3(0, 0, 10) - middle.Position;
        Assert.True(middle.Up.Dot(toCentre) > 0);   // leans into the turn
        Assert.True(Mathf.Abs(middle.Up.Dot(curve.Derivative(curve.Points.Count / 2, 0).Normalized())) < 1e-4f);
    }

    /// <summary>
    /// Signed distance from a point (x along +X, y toward Bend) to a convex polygon widened by
    /// <paramref name="radius"/>: the ideal rounded outline.
    /// </summary>
    private static float OutlineDistance(Vector2 point, Vector2[] inner, float radius)
    {
        float edge = float.MaxValue;
        bool inside = true;
        for (int k = 0; k < inner.Length; k++)
        {
            var a = inner[k];
            var ab = inner[(k + 1) % inner.Length] - a;
            float t = ab.LengthSquared() < 1e-12f ? 0 : Math.Clamp((point - a).Dot(ab) / ab.LengthSquared(), 0, 1);
            edge = Math.Min(edge, point.DistanceTo(a + ab * t));
            if (ab.X * (point.Y - a.Y) - ab.Y * (point.X - a.X) <= 0)   // a flat inner polygon has no inside
                inside = false;
        }
        return (inside ? -edge : edge) - radius;
    }

    private static Vector2[] Inset(Vector2[] corners, Vector2 centre, float inradiusRatio) =>
        corners.Select(c => centre + (c - centre) * inradiusRatio).ToArray();

    public static IEnumerable<object[]> OutlineCases() => new[]
    {
        new object[] { ShapeKind.RoundedRectangle, new ShapeParameters() },
        new object[] { ShapeKind.RoundedRectangle, new ShapeParameters { Width = 4, Depth = 8, CornerRadius = 0, Bend = Bend.Left } },
        new object[] { ShapeKind.RoundedRectangle, new ShapeParameters { Width = 30, Depth = 2, CornerRadius = 1, Bend = Bend.Up } },
        new object[] { ShapeKind.Polygon, new ShapeParameters() },
        new object[] { ShapeKind.Polygon, new ShapeParameters { Sides = 3, Radius = 8, CornerRadius = 0 } },
        new object[] { ShapeKind.Polygon, new ShapeParameters { Sides = 12, Radius = 3, CornerRadius = 0.5f, Bend = Bend.Down } },
    };

    [Theory]
    [MemberData(nameof(OutlineCases))]
    public void Outlines_follow_their_rounded_polygon(ShapeKind kind, ShapeParameters p)
    {
        var curve = Build(kind, p);
        var b = p.Bend.Local();
        Vector2[] corners;
        Vector2 centre;
        float radius = p.CornerRadius;
        if (kind == ShapeKind.RoundedRectangle)
        {
            float w = p.Width / 2, r = radius;
            corners = new[] { new Vector2(w - r, r), new Vector2(w - r, p.Depth - r), new Vector2(-w + r, p.Depth - r), new Vector2(-w + r, r) };
            centre = new Vector2(0, p.Depth / 2);
        }
        else
        {
            float half = Mathf.Pi / p.Sides, inner = p.Radius * Mathf.Cos(half);
            centre = new Vector2(0, inner);
            corners = Enumerable.Range(0, p.Sides).Select(k =>
            {
                float a = half + Mathf.Tau * k / p.Sides;
                return new Vector2(p.Radius * Mathf.Sin(a), inner - p.Radius * Mathf.Cos(a));
            }).ToArray();
            corners = Inset(corners, centre, (inner - radius) / inner);
        }
        float size = Shapes.Size(kind, p);
        foreach (var sample in CurveSamples(curve))
        {
            Assert.True(Mathf.Abs(sample.Y * (b.Y == 0 ? 1 : 0)) < 1e-4f || b.Y != 0);   // flat in its plane
            var flat = new Vector2(sample.X, sample.Dot(b));
            Assert.True(Mathf.Abs(OutlineDistance(flat, corners, radius)) <= Shapes.Accuracy * size, $"{kind} {flat}");
        }
        float perimeter = Enumerable.Range(0, corners.Length).Sum(k => corners[k].DistanceTo(corners[(k + 1) % corners.Length])) + Mathf.Tau * radius;
        Assert.InRange(curve.Length, perimeter * (1 - Shapes.Accuracy), perimeter * (1 + Shapes.Accuracy));
        Assert.True(curve.Closed);
        Assert.True(Mathf.RadToDeg(curve.StartTangent.AngleTo(Vector3.Right)) < 0.01f);
        Assert.True(curve.Points[0].Position.IsEqualApprox(Vector3.Zero));
    }

    [Fact]
    public void A_square_without_corner_radius_has_sharp_corners()
    {
        var curve = Build(ShapeKind.RoundedRectangle, new() { Width = 4, Depth = 4, CornerRadius = 0 });
        Assert.Equal(5, curve.Points.Count);   // the start and four corners
        Assert.All(curve.Points, q => Assert.True(q.In == Vector3.Zero && q.Out == Vector3.Zero));
    }

    [Fact]
    public void A_polygon_rounded_to_its_inner_radius_is_a_circle()
    {
        var p = new ShapeParameters { Sides = 6, Radius = 4, CornerRadius = 4 * Mathf.Cos(Mathf.Pi / 6) };
        var curve = Build(ShapeKind.Polygon, p);
        var centre = new Vector3(0, 0, p.CornerRadius);
        foreach (var sample in CurveSamples(curve))
            Assert.InRange(sample.DistanceTo(centre), p.CornerRadius * (1 - 3e-4f), p.CornerRadius * (1 + 3e-4f));
    }

    [Fact]
    public void A_figure_eight_has_round_lobes_crossing_at_right_angles()
    {
        var curve = Build(ShapeKind.FigureEight, new() { Radius = 4 });
        Assert.True(curve.Closed);
        Assert.InRange(curve.Length, (3 * Math.PI + 4) * 4 * 0.999, (3 * Math.PI + 4) * 4 * 1.001);
        // The two straights cross in the middle, at 90°.
        var straights = Enumerable.Range(0, curve.SegmentCount).Where(s => curve.Points[s].Out == Vector3.Zero && curve.Points[(s + 1) % curve.Points.Count].In == Vector3.Zero).ToList();
        Assert.Equal(2, straights.Count);
        var one = curve.Sample(straights[0], 1) - curve.Sample(straights[0], 0);
        var two = curve.Sample(straights[1], 1) - curve.Sample(straights[1], 0);
        Assert.Equal(90f, Mathf.RadToDeg(one.AngleTo(two)), 2);
        Assert.True(curve.Sample(straights[0], 0.5f).IsEqualApprox(new Vector3(0, 0, 4 * (1 + Mathf.Sqrt2))));
        Assert.True(curve.Points.All(q => q.Up.IsEqualApprox(Vector3.Up)));
        Assert.Equal(2 * 4 * (1 + Mathf.Sqrt2), Shapes.Size(ShapeKind.FigureEight, new() { Radius = 4 }), 4);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(3f)]
    [InlineData(-2f)]
    public void A_shifted_loop_enters_and_leaves_along_the_direction(float shift)
    {
        // Board, M4 review: a linear Side Shift left the straight about 4° sideways, a jolt to the side.
        var curve = Build(ShapeKind.Loop, new() { Length = 30, Diameter = 14, SideShift = shift });
        int entry = 1, exit = curve.Points.Count - 2;
        Assert.True(curve.Points[entry].Out.Z == 0 && curve.Points[exit].In.Z == 0);
        Assert.True(AngleDegrees(curve.Points[entry].Out, Vector3.Right) < 0.01f);
        Assert.True(AngleDegrees(-curve.Points[exit].In, Vector3.Right) < 0.01f);
        Assert.Equal(shift, curve.Points[^1].Position.Z, 4);
    }
}
