namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// PCG32 (XSH RR) pseudo-random generator. Fully deterministic for a given seed and stream on
/// every platform, so a scatter can be reproduced from its settings. Separate streams give
/// independent sequences from the same seed, which keeps pipeline stages from affecting each other.
/// </summary>
public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state;
    private readonly ulong _increment;

    public Pcg32(ulong seed, ulong stream = 0)
    {
        _increment = (stream << 1) | 1UL;
        _state = 0;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = _state;
        _state = unchecked(old * Multiplier + _increment);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rotation = (int)(old >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    /// <summary>Uniform float in [min, max).</summary>
    public float Range(float min, float max) => min + (max - min) * NextFloat();

    /// <summary>Uniform integer in [0, maxExclusive).</summary>
    public int NextInt(int maxExclusive) => (int)(((ulong)NextUInt() * (ulong)maxExclusive) >> 32);
}
