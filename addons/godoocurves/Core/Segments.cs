using System;
using Godot;

namespace Godoodads.Curves.Core;

/// <summary>Exact circular arcs as cubic segments (brief godoocurves §4.1).</summary>
public static class Arcs
{
    /// <summary>The largest angle one cubic segment of an arc covers.</summary>
    public const float MaxSegmentAngle = Mathf.Pi / 2;

    /// <summary>Handle length of a cubic arc segment of <paramref name="angle"/> radians on radius 1: 4/3·tan(θ/4).</summary>
    public static float HandleLength(float angle) => 4f / 3f * Mathf.Tan(Mathf.Abs(angle) / 4f);

    /// <summary>
    /// Adds an arc around <paramref name="center"/>, starting at <c>center + radial</c> and turning by
    /// <paramref name="angle"/> radians around <paramref name="axis"/> (right-handed; negative turns
    /// the other way). <paramref name="drift"/> is added linearly over the arc, as a Side Shift.
    /// <paramref name="up"/> gives the wanted up from the unit radial and the fraction of the arc done.
    /// Pieces are at most 90°, and at least <paramref name="minSegments"/> (for a wanted up that
    /// changes along the arc, since Godot interpolates tilt linearly between points). Returns the
    /// number of segments added.
    /// </summary>
    public static int Add(CurveBuilder builder, Vector3 center, Vector3 radial, Vector3 axis, float angle, Vector3 drift,
        Func<Vector3, float, Vector3> up, int minSegments = 1)
    {
        axis = axis.Normalized();
        float radius = radial.Length();
        int segments = Math.Max(Math.Max(1, minSegments), (int)Math.Ceiling(Math.Abs(angle) / MaxSegmentAngle - 1e-6));
        float step = angle / segments;
        float handle = HandleLength(step) * radius;
        float sign = Math.Sign(angle);
        // The drift's handle uses the same factor per radian as the circle's (4/3·tan(Δ/4) instead of
        // Δ/3), so a helix or a shifted loop leaves in its exact direction.
        var driftHandle = Math.Abs(angle) < 1e-9f ? Vector3.Zero : drift * (HandleLength(step) / Math.Abs(angle));
        for (int i = 0; i <= segments; i++)
        {
            var r = radial.Rotated(axis, step * i);
            var tangent = axis.Cross(r).Normalized() * sign;
            var h = tangent * handle + driftHandle;
            float fraction = i / (float)segments;
            builder.Add(new CurvePoint(center + r + drift * fraction, -h, h, up(r / radius, fraction)));
        }
        return segments;
    }
}

/// <summary>
/// Fits cubic segments to a parametric curve: Hermite segments with handles from the derivative,
/// with as few segments as keep every sample within a tolerance of the exact curve.
/// </summary>
public static class Fitter
{
    private const int SamplesPerSegment = 12;
    private const int MaxSegments = 4096;

    /// <summary>
    /// Adds the fitted points for <paramref name="position"/> from <paramref name="t0"/> to
    /// <paramref name="t1"/>. <paramref name="derivative"/> is d(position)/dt. The error is measured
    /// parametrically (fitted and exact point at the same t), which bounds the distance to the curve.
    /// Returns the number of segments added.
    /// </summary>
    public static int Add(CurveBuilder builder, Func<double, Vector3> position, Func<double, Vector3> derivative,
        double t0, double t1, float tolerance, Func<double, Vector3> up, int minSegments = 1)
    {
        int n = Fewest(position, derivative, t0, t1, tolerance, Math.Max(1, minSegments));
        double dt = (t1 - t0) / n;
        for (int i = 0; i <= n; i++)
        {
            double t = t0 + dt * i;
            var h = derivative(t) * (float)(dt / 3);
            builder.Add(new CurvePoint(position(t), -h, h, up(t)));
        }
        return n;
    }

    /// <summary>The largest parametric error of an even fit with <paramref name="segments"/> segments.</summary>
    public static float Error(Func<double, Vector3> position, Func<double, Vector3> derivative, double t0, double t1, int segments)
    {
        double dt = (t1 - t0) / segments;
        float worst = 0;
        for (int s = 0; s < segments; s++)
        {
            double a = t0 + dt * s, b = a + dt;
            var p0 = position(a);
            var p3 = position(b);
            var p1 = p0 + derivative(a) * (float)(dt / 3);
            var p2 = p3 - derivative(b) * (float)(dt / 3);
            for (int i = 1; i < SamplesPerSegment; i++)
            {
                float u = i / (float)SamplesPerSegment;
                worst = Math.Max(worst, Bezier.Point(p0, p1, p2, p3, u).DistanceTo(position(a + dt * u)));
            }
        }
        return worst;
    }

    private static int Fewest(Func<double, Vector3> position, Func<double, Vector3> derivative, double t0, double t1, float tolerance, int start)
    {
        int high = start;
        while (high < MaxSegments && Error(position, derivative, t0, t1, high) > tolerance)
            high *= 2;
        if (high >= MaxSegments)
            return MaxSegments;
        // The fewest segments between the last failing count and the first passing one.
        int low = Math.Max(start, high / 2);
        if (low == high)
            return high;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (Error(position, derivative, t0, t1, mid) <= tolerance)
                high = mid;
            else
                low = mid + 1;
        }
        return high;
    }
}
