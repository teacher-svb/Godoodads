using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

public class Pcg32Tests
{
    [Fact]
    public void Same_seed_and_stream_give_same_sequence()
    {
        var a = new Pcg32(42, 1);
        var b = new Pcg32(42, 1);
        for (int i = 0; i < 1000; i++)
            Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void Different_seeds_or_streams_differ()
    {
        Assert.NotEqual(new Pcg32(1, 1).NextUInt(), new Pcg32(2, 1).NextUInt());
        Assert.NotEqual(new Pcg32(1, 1).NextUInt(), new Pcg32(1, 2).NextUInt());
    }

    [Fact]
    public void Floats_are_in_unit_range_and_roughly_uniform()
    {
        var rng = new Pcg32(7);
        var buckets = new int[10];
        const int n = 100_000;
        for (int i = 0; i < n; i++)
        {
            float f = rng.NextFloat();
            Assert.InRange(f, 0f, 0.99999994f);
            buckets[(int)(f * 10)]++;
        }
        foreach (var count in buckets)
            Assert.InRange(count, n / 10 * 0.95, n / 10 * 1.05);
    }

    [Fact]
    public void NextInt_stays_below_bound()
    {
        var rng = new Pcg32(3);
        for (int i = 0; i < 10_000; i++)
            Assert.InRange(rng.NextInt(7), 0, 6);
    }
}
