using System;
using Godot;

namespace Godoodads.Curves.Core;

/// <summary>The shapes of brief godoocurves §4.1, in the order of its table.</summary>
public enum ShapeKind
{
    Straight,
    Parabolic,
    Circle,
    Semicircle,
    Loop,
    Wave,
    Spiral,
    Arc,
    SBend,
    Ellipse,
}

/// <summary>
/// The numbers every shape reads from. Each shape uses only some of them (brief §4.1); the
/// defaults are BezierPathGenerator's. Lengths are in the path's local units, angles in degrees.
/// </summary>
public sealed class ShapeParameters
{
    public float Length { get; init; } = 10;
    public float Height { get; init; } = 5;
    public float Radius { get; init; } = 5;
    public float Diameter { get; init; } = 6;
    public float SideShift { get; init; }
    public float Amplitude { get; init; } = 3;
    public int Cycles { get; init; } = 2;
    public float Turns { get; init; } = 2;
    public float AngleDegrees { get; init; } = 90;
    public float Offset { get; init; } = 3;
    public float RadiusForward { get; init; } = 6;
    public float RadiusSide { get; init; } = 3;
    public Bend Bend { get; init; } = Bend.Right;
}

/// <summary>A built shape in local coordinates, or the reason it cannot be built.</summary>
public readonly record struct ShapeResult(ShapeCurve Curve, string Error)
{
    public bool IsValid => Error == null;
}

/// <summary>
/// Builds shapes in local coordinates: X forward, Y up, Z side (<see cref="ShapeFrame"/>). Every
/// shape starts at the origin heading +X, except Parabolic, which leaves at its slope.
/// </summary>
public static class Shapes
{
    /// <summary>Every shape stays within this share of its size of the exact shape (brief §4.1, §8).</summary>
    public const float Accuracy = 1e-3f;

    /// <summary>Fitted shapes aim at half the accuracy, leaving room for float rounding.</summary>
    private const float FitShare = 0.5f;

    private static readonly Vector3 LocalUp = Vector3.Up;

    public static ShapeResult Build(ShapeKind kind, ShapeParameters p)
    {
        if (Check(kind, p) is { } error)
            return new ShapeResult(null, error);
        var curve = kind switch
        {
            ShapeKind.Straight => Straight(p),
            ShapeKind.Parabolic => Parabolic(p),
            ShapeKind.Circle => Arc(p.Radius, Mathf.Tau, p.Bend, closed: true),
            ShapeKind.Semicircle => Arc(p.Radius, Mathf.Pi, p.Bend, closed: false),
            ShapeKind.Loop => Loop(p),
            ShapeKind.Wave => Wave(p, Tolerance(kind, p)),
            ShapeKind.Spiral => Spiral(p, Tolerance(kind, p)),
            ShapeKind.Arc => Arc(p.Radius, Mathf.DegToRad(p.AngleDegrees), p.Bend, closed: p.AngleDegrees >= 360),
            ShapeKind.SBend => SBend(p),
            ShapeKind.Ellipse => Ellipse(p, Tolerance(kind, p)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        return new ShapeResult(curve, null);
    }

    /// <summary>The size accuracy is measured against: the shape's largest extent.</summary>
    public static float Size(ShapeKind kind, ShapeParameters p) => kind switch
    {
        ShapeKind.Straight => p.Length,
        ShapeKind.Parabolic => Math.Max(p.Length, Math.Abs(p.Height)),
        ShapeKind.Circle or ShapeKind.Semicircle => 2 * p.Radius,
        ShapeKind.Loop => Math.Max(Math.Max(p.Length, p.Diameter), Math.Abs(p.SideShift)),
        ShapeKind.Wave => Math.Max(p.Length, p.Amplitude),
        ShapeKind.Spiral => Math.Max(p.Length, 2 * p.Radius),
        ShapeKind.Arc => 2 * p.Radius,
        ShapeKind.SBend => Math.Max(p.Length, p.Offset),
        ShapeKind.Ellipse => 2 * Math.Max(p.RadiusForward, p.RadiusSide),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Why the settings cannot make this shape, or null.</summary>
    public static string Check(ShapeKind kind, ShapeParameters p)
    {
        static bool Bad(float value) => !float.IsFinite(value) || value <= 0;
        return kind switch
        {
            ShapeKind.Straight when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.Parabolic when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.Parabolic when !float.IsFinite(p.Height) => "Height must be a number.",
            ShapeKind.Circle or ShapeKind.Semicircle when Bad(p.Radius) => "Radius must be above 0.",
            ShapeKind.Loop when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.Loop when Bad(p.Diameter) => "Diameter must be above 0.",
            ShapeKind.Loop when !float.IsFinite(p.SideShift) => "Side Shift must be a number.",
            ShapeKind.Wave when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.Wave when Bad(p.Amplitude) => "Amplitude must be above 0. Use Bend Down for dips.",
            ShapeKind.Wave when p.Cycles < 1 => "Cycles must be at least 1.",
            ShapeKind.Spiral when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.Spiral when Bad(p.Radius) => "Radius must be above 0.",
            ShapeKind.Spiral when Bad(p.Turns) => "Turns must be above 0.",
            ShapeKind.Arc when Bad(p.Radius) => "Radius must be above 0.",
            ShapeKind.Arc when !float.IsFinite(p.AngleDegrees) || p.AngleDegrees <= 0 || p.AngleDegrees > 360 => "Angle must be above 0 and at most 360 degrees. Use Bend for the side.",
            ShapeKind.SBend when Bad(p.Length) => "Length must be above 0.",
            ShapeKind.SBend when Bad(p.Offset) => "Offset must be above 0. Use Bend for the side.",
            ShapeKind.Ellipse when Bad(p.RadiusForward) => "Radius Forward must be above 0.",
            ShapeKind.Ellipse when Bad(p.RadiusSide) => "Radius Side must be above 0.",
            _ => null,
        };
    }

    private static float Tolerance(ShapeKind kind, ShapeParameters p) => Math.Max(1e-5f, Accuracy * FitShare * Size(kind, p));

    private static ShapeCurve Straight(ShapeParameters p)
    {
        var builder = new CurveBuilder();
        builder.Add(new CurvePoint(Vector3.Zero, Vector3.Zero, Vector3.Zero, LocalUp));
        builder.LineTo(new Vector3(p.Length, 0, 0), LocalUp);
        return builder.Build(false);
    }

    /// <summary>A quadratic Bezier from the origin to (Length, 0, 0) peaking at Height, raised to a cubic.</summary>
    private static ShapeCurve Parabolic(ShapeParameters p)
    {
        // Quadratic control point (L/2, 2H) peaks at H; the cubic handles are 2/3 of the way to it.
        var builder = new CurveBuilder();
        builder.Add(new CurvePoint(Vector3.Zero, Vector3.Zero, new Vector3(p.Length / 3, 4 * p.Height / 3, 0), LocalUp));
        builder.Add(new CurvePoint(new Vector3(p.Length, 0, 0), new Vector3(-p.Length / 3, 4 * p.Height / 3, 0), Vector3.Zero, LocalUp));
        return builder.Build(false);
    }

    /// <summary>
    /// An arc that starts at the origin heading +X and turns toward <paramref name="bend"/>. The
    /// wanted up turns with it, so arcs in the vertical plane keep it across the track.
    /// </summary>
    private static ShapeCurve Arc(float radius, float angle, Bend bend, bool closed)
    {
        var b = bend.Local();
        var axis = Vector3.Right.Cross(b);
        var builder = new CurveBuilder();
        Arcs.Add(builder, b * radius, -b * radius, axis, angle, Vector3.Zero, (_, fraction) => LocalUp.Rotated(axis, angle * fraction));
        return builder.Build(closed);
    }

    /// <summary>
    /// A straight run-in to Length/2, a vertical loop of Diameter that drifts Side Shift to the
    /// side, and a straight run-out to Length (brief §4.1). Inside the loop the wanted up faces its
    /// centre; on the straights it is the frame's up.
    /// </summary>
    private static ShapeCurve Loop(ShapeParameters p)
    {
        float r = p.Diameter / 2, middle = p.Length / 2;
        var builder = new CurveBuilder();
        builder.Add(new CurvePoint(Vector3.Zero, Vector3.Zero, Vector3.Zero, LocalUp));
        builder.LineTo(new Vector3(middle, 0, 0), LocalUp);
        Arcs.Add(builder, new Vector3(middle, r, 0), new Vector3(0, -r, 0), Vector3.Back, Mathf.Tau, new Vector3(0, 0, p.SideShift),
            (radial, _) => -radial);
        builder.LineTo(new Vector3(p.Length, 0, p.SideShift), LocalUp);
        return builder.Build(false);
    }

    /// <summary>Crests of height Amplitude toward Bend: (A/2)(1 − cos(2π·c·x/L)), level at both ends.</summary>
    private static ShapeCurve Wave(ShapeParameters p, float tolerance)
    {
        var b = p.Bend.Local();
        double k = Math.Tau * p.Cycles / p.Length, half = p.Amplitude / 2.0;
        Vector3 Position(double x) => new Vector3((float)x, 0, 0) + b * (float)(half * (1 - Math.Cos(k * x)));
        Vector3 Derivative(double x) => new Vector3(1, 0, 0) + b * (float)(half * k * Math.Sin(k * x));
        var builder = new CurveBuilder();
        Fitter.Add(builder, Position, Derivative, 0, p.Length, tolerance, _ => LocalUp, minSegments: 2 * p.Cycles);
        return builder.Build(false);
    }

    /// <summary>
    /// A right-handed helix around the forward axis, starting below it. The radius grows from 0
    /// over the first half turn and shrinks back over the last (smoothstep), so the spiral starts
    /// and ends on the axis heading +X. Under one turn each ease covers half the spiral, so the
    /// radius stays smooth where they meet.
    /// </summary>
    private static ShapeCurve Spiral(ShapeParameters p, float tolerance)
    {
        double total = Math.Tau * p.Turns, advance = p.Length / total, ease = Math.Min(Math.PI, total / 2);
        (double R, double dR) Radius(double angle)
        {
            double fromStart = angle / ease, fromEnd = (total - angle) / ease;
            double u = Math.Min(1, Math.Min(fromStart, fromEnd));
            double du = u >= 1 ? 0 : fromStart <= fromEnd ? 1 / ease : -1 / ease;
            return (p.Radius * u * u * (3 - 2 * u), p.Radius * 6 * u * (1 - u) * du);
        }
        Vector3 Position(double a)
        {
            var (r, _) = Radius(a);
            return new Vector3((float)(advance * a), (float)(-r * Math.Cos(a)), (float)(-r * Math.Sin(a)));
        }
        Vector3 Derivative(double a)
        {
            var (r, dr) = Radius(a);
            return new Vector3((float)advance, (float)(-dr * Math.Cos(a) + r * Math.Sin(a)), (float)(-dr * Math.Sin(a) - r * Math.Cos(a)));
        }
        var builder = new CurveBuilder();
        Fitter.Add(builder, Position, Derivative, 0, total, tolerance, _ => LocalUp, minSegments: (int)Math.Ceiling(4 * p.Turns));
        return builder.Build(false);
    }

    /// <summary>
    /// Two equal arcs that turn toward Bend and back: the end is Length along and Offset to the
    /// side, heading +X again. Angle θ = 2·atan(Offset/Length), radius Length/(2·sin θ).
    /// </summary>
    private static ShapeCurve SBend(ShapeParameters p)
    {
        var b = p.Bend.Local();
        var axis = Vector3.Right.Cross(b);
        float angle = 2 * Mathf.Atan(p.Offset / p.Length), radius = p.Length / (2 * Mathf.Sin(angle));
        var middle = new Vector3(p.Length / 2, 0, 0) + b * (p.Offset / 2);
        var end = new Vector3(p.Length, 0, 0) + b * p.Offset;
        var builder = new CurveBuilder();
        Arcs.Add(builder, b * radius, -b * radius, axis, angle, Vector3.Zero, (_, f) => LocalUp.Rotated(axis, angle * f));
        var second = end - b * radius;
        Arcs.Add(builder, second, middle - second, axis, -angle, Vector3.Zero, (_, f) => LocalUp.Rotated(axis, angle * (1 - f)));
        return builder.Build(false);
    }

    /// <summary>
    /// A closed ellipse with semi-axes Radius Forward (along +X) and Radius Side (toward Bend),
    /// starting at its point nearest the origin side: (a·sin φ, c·(1 − cos φ)). The wanted up turns
    /// with the tangent, as on a circle.
    /// </summary>
    private static ShapeCurve Ellipse(ShapeParameters p, float tolerance)
    {
        var b = p.Bend.Local();
        var axis = Vector3.Right.Cross(b);
        float a = p.RadiusForward, c = p.RadiusSide;
        Vector3 Position(double phi) => new Vector3(a * (float)Math.Sin(phi), 0, 0) + b * (c * (float)(1 - Math.Cos(phi)));
        Vector3 Derivative(double phi) => new Vector3(a * (float)Math.Cos(phi), 0, 0) + b * (c * (float)Math.Sin(phi));
        Vector3 Up(double phi)
        {
            // The tangent's angle from +X, unwrapped: it stays within 90° of phi for positive radii.
            double raw = Math.Atan2(c * Math.Sin(phi), a * Math.Cos(phi));
            double turned = raw + Math.Tau * Math.Round((phi - raw) / Math.Tau);
            return LocalUp.Rotated(axis, (float)turned);
        }
        var builder = new CurveBuilder();
        Fitter.Add(builder, Position, Derivative, 0, Math.Tau, tolerance, Up, minSegments: 4);
        return builder.Build(true);
    }
}
