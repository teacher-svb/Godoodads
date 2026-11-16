using System;
using System.Collections.Generic;
using Godot;
using Godoodads.Curves.Core;

namespace Godoodads.Curves.Adapters;

/// <summary>
/// Writes shapes into <see cref="Curve3D"/> resources (brief godoocurves §4.3, §4.4). Never changes a
/// curve it is given: Replace makes a new curve, Append adds to a duplicate.
/// </summary>
public static class CurveWriter
{
    /// <summary>Samples per segment for the offsets at which tilt is measured.</summary>
    private const int LengthSamples = 32;

    /// <summary>A wanted up this close to the tangent gives no direction; the previous tilt is kept.</summary>
    private const float MinAcross = 0.05f;

    /// <summary>The result of a write: the new curve, or why there is none, and a note for the status.</summary>
    public readonly record struct WriteResult(Curve3D Curve, string Error, string Note)
    {
        public bool IsValid => Error == null;
    }

    /// <summary>A new curve with the shape, already in path space.</summary>
    public static Curve3D Create(ShapeCurve shape)
    {
        var curve = new Curve3D { UpVectorEnabled = true, Closed = shape.Closed };
        foreach (var point in shape.Points)
            curve.AddPoint(point.Position, point.In, point.Out);
        SetTilts(curve, shape, firstPoint: 0, startTilt: 0);
        return curve;
    }

    /// <summary>
    /// A duplicate of <paramref name="existing"/> with the shape (in local coordinates) appended at
    /// its end, continuing its direction and up. A curve without a direction (empty, or a single point)
    /// starts the shape from the default frame (brief §4.3), at its point if it has one.
    /// </summary>
    public static WriteResult Append(Curve3D existing, ShapeCurve localShape)
    {
        if (localShape.Closed)
            return new WriteResult(null, "Closed shapes cannot be appended. Use Replace, or choose an open shape.", null);
        if (existing != null && existing.Closed)
            return new WriteResult(null, "The path's curve is closed, so nothing can be appended to its end. Use Replace.", null);

        if (existing == null || existing.PointCount == 0)
            return new WriteResult(Create(localShape.ToPath(ShapeFrame.Default)), null, "The path had no curve, so the shape starts at the default place: the origin, heading +X.");

        string note = null;
        if (!TryEndFrame(existing, out var frame))
        {
            frame = ShapeFrame.Default with { Origin = existing.GetPointPosition(existing.PointCount - 1) };
            note = "The curve's end has no direction, so the shape continues along +X.";
        }

        var shape = localShape.ToPath(frame);
        var curve = (Curve3D)existing.Duplicate();
        curve.UpVectorEnabled = true;
        int join = curve.PointCount - 1;
        curve.SetPointOut(join, shape.Points[0].Out);
        for (int i = 1; i < shape.Points.Count; i++)
            curve.AddPoint(shape.Points[i].Position, shape.Points[i].In, shape.Points[i].Out);
        SetTilts(curve, shape, firstPoint: join, startTilt: curve.GetPointTilt(join));
        return new WriteResult(curve, null, note);
    }

    /// <summary>
    /// The frame at the end of a curve: its last point, the direction it arrives in (the reversed
    /// in-handle, or the chord from the point before), and its up including tilt.
    /// </summary>
    public static bool TryEndFrame(Curve3D curve, out ShapeFrame frame)
    {
        frame = default;
        int last = curve.PointCount - 1;
        if (last < 0)
            return false;
        var position = curve.GetPointPosition(last);
        var direction = -curve.GetPointIn(last);
        if (direction.LengthSquared() < 1e-12f && last > 0)
            direction = position - curve.GetPointPosition(last - 1);
        if (direction.LengthSquared() < 1e-12f)
            return false;
        float length = curve.GetBakedLength();
        var up = length > 0 ? curve.SampleBakedUpVector(length, true) : Vector3.Up;
        return ShapeFrame.TryCreate(position, direction, up, out frame, out _);
    }

    /// <summary>
    /// Sets the tilt of the shape's points in <paramref name="curve"/>, from <paramref name="firstPoint"/>
    /// on: the signed angle around the tangent from Godot's untilted up to the point's wanted up,
    /// unwrapped so neighbouring tilts differ by less than half a turn (brief §4.4). The first point
    /// keeps <paramref name="startTilt"/>.
    /// </summary>
    private static void SetTilts(Curve3D curve, ShapeCurve shape, int firstPoint, float startTilt)
    {
        var offsets = PointOffsets(curve);
        float baked = curve.GetBakedLength();
        float previous = startTilt;
        curve.SetPointTilt(firstPoint, startTilt);
        for (int i = 1; i < shape.Points.Count; i++)
        {
            int index = firstPoint + i;
            var tangent = Tangent(curve, index);
            float offset = Math.Min(offsets[index], baked);
            var godotUp = Across(curve.SampleBakedUpVector(offset, false), tangent);
            var wanted = Across(shape.Points[i].Up, tangent);
            float tilt = previous;
            if (godotUp.LengthSquared() > MinAcross * MinAcross && wanted.LengthSquared() > MinAcross * MinAcross)
            {
                float angle = godotUp.SignedAngleTo(wanted, tangent);
                tilt = previous + Mathf.Wrap(angle - previous, -Mathf.Pi, Mathf.Pi);
            }
            curve.SetPointTilt(index, tilt);
            previous = tilt;
        }
    }

    private static Vector3 Across(Vector3 v, Vector3 tangent) => v - tangent * v.Dot(tangent);

    /// <summary>The direction at a point: its out-handle, its reversed in-handle, or the chord to a neighbour.</summary>
    private static Vector3 Tangent(Curve3D curve, int index)
    {
        var t = curve.GetPointOut(index);
        if (t.LengthSquared() < 1e-12f)
            t = -curve.GetPointIn(index);
        if (t.LengthSquared() < 1e-12f)
            t = index + 1 < curve.PointCount
                ? curve.GetPointPosition(index + 1) - curve.GetPointPosition(index)
                : curve.GetPointPosition(index) - curve.GetPointPosition(index - 1);
        return t.Normalized();
    }

    /// <summary>
    /// The length along the curve to every point, summed over its segments. Unlike
    /// <c>get_closest_offset</c>, this is right where the curve crosses itself.
    /// </summary>
    private static List<float> PointOffsets(Curve3D curve)
    {
        var offsets = new List<float> { 0 };
        float length = 0;
        for (int segment = 0; segment + 1 < curve.PointCount; segment++)
        {
            var previous = curve.GetPointPosition(segment);
            for (int s = 1; s <= LengthSamples; s++)
            {
                var next = curve.Sample(segment, s / (float)LengthSamples);
                length += previous.DistanceTo(next);
                previous = next;
            }
            offsets.Add(length);
        }
        return offsets;
    }
}
