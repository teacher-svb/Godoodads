using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.Curves.Core;

/// <summary>
/// One point of a shape, as <c>Curve3D</c> stores it: a position and its in- and out-handles,
/// relative to the position. <see cref="Up"/> is the wanted up direction there, from which the
/// adapter computes the point's tilt (brief godoocurves §4.4). Zero handles give a straight segment.
/// </summary>
public readonly record struct CurvePoint(Vector3 Position, Vector3 In, Vector3 Out, Vector3 Up);

/// <summary>A shape's points, in local or path coordinates. A closed curve joins its last point to its first.</summary>
public sealed class ShapeCurve
{
    private const int SamplesPerSegment = 64;

    public ShapeCurve(IReadOnlyList<CurvePoint> points, bool closed)
    {
        Points = points;
        Closed = closed;
    }

    public IReadOnlyList<CurvePoint> Points { get; }
    public bool Closed { get; }

    /// <summary>The number of cubic segments: one fewer than the points, or as many when closed.</summary>
    public int SegmentCount => Points.Count < 2 ? 0 : Closed ? Points.Count : Points.Count - 1;

    /// <summary>A point on segment <paramref name="segment"/> at <paramref name="t"/> from 0 to 1.</summary>
    public Vector3 Sample(int segment, float t)
    {
        var (a, b) = Ends(segment);
        return Bezier.Point(a.Position, a.Position + a.Out, b.Position + b.In, b.Position, t);
    }

    /// <summary>The derivative on segment <paramref name="segment"/> at <paramref name="t"/>.</summary>
    public Vector3 Derivative(int segment, float t)
    {
        var (a, b) = Ends(segment);
        return Bezier.Derivative(a.Position, a.Position + a.Out, b.Position + b.In, b.Position, t);
    }

    /// <summary>The direction the curve starts in, or zero for fewer than two points.</summary>
    public Vector3 StartTangent => SegmentCount == 0 ? Vector3.Zero : Tangent(0, 0);

    /// <summary>The direction the curve ends in, or zero for fewer than two points.</summary>
    public Vector3 EndTangent => SegmentCount == 0 ? Vector3.Zero : Tangent(SegmentCount - 1, 1);

    /// <summary>The length, from <see cref="SamplesPerSegment"/> chords per segment.</summary>
    public double Length
    {
        get
        {
            double length = 0;
            for (int s = 0; s < SegmentCount; s++)
            {
                var previous = Sample(s, 0);
                for (int i = 1; i <= SamplesPerSegment; i++)
                {
                    var next = Sample(s, i / (float)SamplesPerSegment);
                    length += previous.DistanceTo(next);
                    previous = next;
                }
            }
            return length;
        }
    }

    /// <summary>The same curve in the path's space.</summary>
    public ShapeCurve ToPath(ShapeFrame frame)
    {
        var points = new CurvePoint[Points.Count];
        for (int i = 0; i < points.Length; i++)
        {
            var p = Points[i];
            points[i] = new CurvePoint(frame.ToPath(p.Position), frame.ToPathDirection(p.In), frame.ToPathDirection(p.Out),
                frame.ToPathDirection(p.Up).Normalized());
        }
        return new ShapeCurve(points, Closed);
    }

    /// <summary>
    /// The tangent at a segment end. A zero handle (a straight segment, or a cusp) falls back to
    /// the other handle, then to the chord.
    /// </summary>
    private Vector3 Tangent(int segment, float t)
    {
        var d = Derivative(segment, t);
        if (d.LengthSquared() > 1e-12f)
            return d.Normalized();
        var (a, b) = Ends(segment);
        var chord = b.Position - a.Position;
        return chord.LengthSquared() > 1e-12f ? chord.Normalized() : Vector3.Zero;
    }

    private (CurvePoint A, CurvePoint B) Ends(int segment)
    {
        if (segment < 0 || segment >= SegmentCount)
            throw new ArgumentOutOfRangeException(nameof(segment));
        return (Points[segment], Points[(segment + 1) % Points.Count]);
    }
}

/// <summary>Cubic Bezier evaluation.</summary>
public static class Bezier
{
    public static Vector3 Point(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return p0 * (u * u * u) + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * (t * t * t);
    }

    public static Vector3 Derivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return (p1 - p0) * (3 * u * u) + (p2 - p1) * (6 * u * t) + (p3 - p2) * (3 * t * t);
    }
}

/// <summary>
/// Collects the points of a shape built from pieces. A piece that starts where the last one ended
/// merges into its end point: that point keeps its in-handle and takes the new out-handle and up.
/// </summary>
public sealed class CurveBuilder
{
    /// <summary>Two positions this close count as the same point.</summary>
    public const float JoinTolerance = 1e-4f;

    private readonly List<CurvePoint> _points = new();

    public int Count => _points.Count;

    public CurvePoint Last => _points[^1];

    /// <summary>Adds a point, merging it into the last one when they are at the same position.</summary>
    public void Add(CurvePoint point)
    {
        if (_points.Count > 0 && _points[^1].Position.DistanceTo(point.Position) <= JoinTolerance)
        {
            var last = _points[^1];
            _points[^1] = new CurvePoint(last.Position, last.In, point.Out, point.Up);
            return;
        }
        _points.Add(point);
    }

    /// <summary>A straight segment from the last point to <paramref name="position"/>.</summary>
    public void LineTo(Vector3 position, Vector3 up)
    {
        if (_points.Count > 0)
        {
            var last = _points[^1];
            _points[^1] = last with { Out = Vector3.Zero };
        }
        Add(new CurvePoint(position, Vector3.Zero, Vector3.Zero, up));
    }

    /// <summary>
    /// The finished curve. A closed curve whose last point is back at its first merges them: the
    /// first point takes the last one's in-handle. An open curve has no handle before its first
    /// point or after its last.
    /// </summary>
    public ShapeCurve Build(bool closed)
    {
        var points = new List<CurvePoint>(_points);
        if (closed)
        {
            if (points.Count > 1 && points[^1].Position.DistanceTo(points[0].Position) <= JoinTolerance)
            {
                points[0] = points[0] with { In = points[^1].In };
                points.RemoveAt(points.Count - 1);
            }
        }
        else if (points.Count > 0)
        {
            points[0] = points[0] with { In = Vector3.Zero };
            points[^1] = points[^1] with { Out = Vector3.Zero };
        }
        return new ShapeCurve(points, closed);
    }
}
