using System;
using System.Collections.Generic;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>Engine-free options of one scatter run (mirrors the ScatterSettings resource).</summary>
public sealed class ScatterOptions
{
    public int Amount = 128;
    public int Seed;
    /// <summary>
    /// Mixed into <see cref="Seed"/> when non-zero. Append mode passes the existing instance count,
    /// so repeated appends with one seed don't replay the same positions.
    /// </summary>
    public ulong SeedSalt;
    public SamplerMode Sampler = SamplerMode.AreaWeighted;

    /// <summary>Target direction V of the orientation blend (world space).</summary>
    public Vector3 TargetDirection = Vector3.Up;
    public NormalSource NormalSource = NormalSource.Face;
    /// <summary>Required for <see cref="NormalSource.NormalMap"/>.</summary>
    public NormalMapOptions NormalMap;
    /// <summary>0 follows the surface normal, 1 follows <see cref="TargetDirection"/>.</summary>
    public float Alignment = 1f;
    /// <summary>Per-instance jitter on <see cref="Alignment"/>, in [-r, r].</summary>
    public float AlignmentRandomness;
    public MeshAxis MeshUpAxis = MeshAxis.PositiveY;

    /// <summary>Slope filter range: angle between face normal and the slope reference, in degrees.</summary>
    public float SlopeMinDegrees;
    public float SlopeMaxDegrees = 180f;
    /// <summary>Slope reference direction; null uses <see cref="TargetDirection"/>.</summary>
    public Vector3? SlopeReference;

    public float BaseScale = 1f;
    public float RandomScale = 0.3f;
    public float RandomYawDegrees = 360f;
    public float RandomTiltDegrees;

    /// <summary>Hard minimum distance between instances; 0 disables the spacing stage.</summary>
    public float MinDistance;
    /// <summary>Candidates generated per requested instance when spacing is on.</summary>
    public int CandidateFactor = 5;
    /// <summary>Sampling attempts allowed per needed candidate before giving up (rejection filters).</summary>
    public int MaxAttemptsFactor = 20;
}

/// <summary>
/// A per-candidate accept/reject stage (slope filter, mask filter, ...). Filters run in the
/// order given and the first rejecting filter is credited in <see cref="ScatterResult.Rejected"/>.
/// </summary>
public interface ICandidateFilter
{
    string Name { get; }
    bool Accept(SurfaceData surface, in SurfacePoint point, Pcg32 rng);
}

public sealed class ScatterResult
{
    /// <summary>World-space instance transforms.</summary>
    public Transform3D[] Transforms = Array.Empty<Transform3D>();
    public int Requested;
    public int Attempts;
    public int Candidates;
    public int RemovedBySpacing;
    /// <summary>Fraction of the surface area the slope filter kept (1 without filtering).</summary>
    public float SlopeAreaFraction = 1f;
    /// <summary>Rejected attempts per filter name.</summary>
    public Dictionary<string, int> Rejected = new();
    public int Placed => Transforms.Length;
}

/// <summary>
/// Runs the scatter stages in order: slope filter (as sampler weights), sample, filter, spacing,
/// orientation and randomisation.
/// Each stage draws from its own random stream so changing one stage's settings does not
/// reshuffle the others.
/// </summary>
public static class ScatterPipeline
{
    private const ulong SamplingStream = 1;
    private const ulong TransformStream = 2;

    public static ScatterResult Run(SurfaceData surface, ScatterOptions options,
        IReadOnlyList<ICandidateFilter> filters = null, IReadOnlyList<Vector3> fixedPoints = null,
        float[] triangleWeights = null)
    {
        var result = new ScatterResult { Requested = Math.Max(0, options.Amount) };
        var targetDirection = Normalized(options.TargetDirection, Vector3.Up);

        // Slope filter: per-triangle weights, combined with any caller weights.
        var slope = OrientationMath.SlopeWeights(surface, Normalized(options.SlopeReference ?? targetDirection, targetDirection),
            options.SlopeMinDegrees, options.SlopeMaxDegrees);
        if (slope != null)
        {
            double keptArea = 0;
            for (int t = 0; t < slope.Length; t++)
            {
                if (triangleWeights != null)
                    slope[t] *= triangleWeights[t];
                keptArea += slope[t] > 0 ? surface.Areas[t] : 0;
            }
            result.SlopeAreaFraction = surface.TotalArea > 0 ? (float)(keptArea / surface.TotalArea) : 0f;
            triangleWeights = slope;
        }

        var sampler = new TriangleSampler(surface, options.Sampler, triangleWeights);
        if (sampler.IsEmpty || result.Requested == 0)
            return result;

        ulong seed = MixSeed(unchecked((ulong)(uint)options.Seed), options.SeedSalt);
        bool spacing = options.MinDistance > 0f;
        int wanted = spacing ? checked(result.Requested * Math.Max(1, options.CandidateFactor)) : result.Requested;
        long maxAttempts = (long)wanted * Math.Max(1, options.MaxAttemptsFactor);

        // Sample and filter.
        var rng = new Pcg32(seed, SamplingStream);
        var points = new List<SurfacePoint>(wanted);
        while (points.Count < wanted && result.Attempts < maxAttempts)
        {
            result.Attempts++;
            var point = sampler.Sample(rng);
            if (Accepted(surface, point, filters, rng, result.Rejected))
                points.Add(point);
        }
        result.Candidates = points.Count;

        // Spacing.
        IReadOnlyList<int> kept;
        if (spacing)
        {
            var positions = new Vector3[points.Count];
            for (int i = 0; i < positions.Length; i++)
                positions[i] = points[i].Position;
            float acceptedArea = sampler.SampledArea * points.Count / Math.Max(1, result.Attempts);
            var elimination = SampleElimination.Eliminate(positions, result.Requested, options.MinDistance,
                acceptedArea, fixedPoints);
            kept = elimination.Kept;
            result.RemovedBySpacing = elimination.RemovedForSpacing;
        }
        else
        {
            var all = new int[points.Count];
            for (int i = 0; i < all.Length; i++)
                all[i] = i;
            kept = all;
        }

        // Orientation and randomisation.
        var transformRng = new Pcg32(seed, TransformStream);
        result.Transforms = new Transform3D[kept.Count];
        for (int i = 0; i < kept.Count; i++)
        {
            var point = points[kept[i]];
            var normal = OrientationMath.SurfaceNormal(surface, point, options.NormalSource, options.NormalMap);
            result.Transforms[i] = BuildTransform(point.Position, normal, targetDirection, options, transformRng);
        }
        return result;
    }

    private static Vector3 Normalized(Vector3 v, Vector3 fallback) => v.LengthSquared() > 1e-12f ? v.Normalized() : fallback;

    private static ulong MixSeed(ulong seed, ulong salt) => salt == 0 ? seed : SplitMix64(seed ^ SplitMix64(salt));

    private static ulong SplitMix64(ulong x)
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }

    private static bool Accepted(SurfaceData surface, in SurfacePoint point, IReadOnlyList<ICandidateFilter> filters,
        Pcg32 rng, Dictionary<string, int> rejected)
    {
        if (filters == null)
            return true;
        foreach (var filter in filters)
        {
            if (filter.Accept(surface, point, rng))
                continue;
            rejected[filter.Name] = rejected.GetValueOrDefault(filter.Name) + 1;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Computes the instance up as <c>slerp(normal, target, b_i)</c>, aligns the mesh up axis to it,
    /// then applies random yaw around that up axis, random tilt, scale and position, in that order.
    /// </summary>
    internal static Transform3D BuildTransform(Vector3 position, Vector3 normal, Vector3 target, ScatterOptions o, Pcg32 rng)
    {
        // Draw every random number unconditionally so the stream stays aligned across settings.
        float yaw = Mathf.DegToRad(o.RandomYawDegrees) * rng.NextFloat();
        float tilt = Mathf.DegToRad(o.RandomTiltDegrees) * rng.NextFloat();
        float tiltDirection = Mathf.Tau * rng.NextFloat();
        float scale = MathF.Max(0.001f, o.BaseScale + o.RandomScale * (2f * rng.NextFloat() - 1f));
        float alignment = Math.Clamp(o.Alignment + o.AlignmentRandomness * (2f * rng.NextFloat() - 1f), 0f, 1f);

        var up = OrientationMath.SlerpDirection(Normalized(normal, target), target, alignment);
        var align = new Quaternion(o.MeshUpAxis.ToVector(), up);
        var rotation = new Quaternion(up, yaw) * align;
        if (tilt > 0f)
        {
            var (t1, t2) = OrientationMath.Perpendiculars(up);
            var axis = t1 * MathF.Cos(tiltDirection) + t2 * MathF.Sin(tiltDirection);
            rotation = new Quaternion(axis, tilt) * rotation;
        }

        var basis = new Basis(rotation.Normalized()) * Basis.FromScale(new Vector3(scale, scale, scale));
        return new Transform3D(basis, position);
    }
}
