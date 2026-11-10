using System;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §11.2: what a generation wrote, stored compressed on the GridMap.
public class WfcGeneratedRecordTests
{
    [Fact]
    public void RoundTrips()
    {
        var record = new GeneratedRecord();
        record.Set(new Vector3I(0, 0, 0), 3, 0);
        record.Set(new Vector3I(-5, 12, 300), 4095, 22);
        record.Set(new Vector3I(32767, -32768, 7), 0, 16);

        var decoded = GeneratedRecord.Decode(record.Encode());
        Assert.Equal(3, decoded.Count);
        Assert.True(decoded.Matches(new Vector3I(0, 0, 0), 3, 0));
        Assert.True(decoded.Matches(new Vector3I(-5, 12, 300), 4095, 22));
        Assert.True(decoded.Matches(new Vector3I(32767, -32768, 7), 0, 16));
    }

    [Fact]
    public void MatchesOnlyTheSameItemAndOrientation()
    {
        var record = new GeneratedRecord();
        record.Set(new Vector3I(1, 2, 3), 5, 10);
        Assert.True(record.Matches(new Vector3I(1, 2, 3), 5, 10));
        Assert.False(record.Matches(new Vector3I(1, 2, 3), 5, 16));
        Assert.False(record.Matches(new Vector3I(1, 2, 3), 6, 10));
        Assert.False(record.Matches(new Vector3I(1, 2, 4), 5, 10));
    }

    [Fact]
    public void RemoveInsideKeepsCellsOutsideTheBox()
    {
        var record = new GeneratedRecord();
        foreach (int x in new[] { -1, 0, 3, 4 })
            record.Set(new Vector3I(x, 0, 0), 1, 0);
        record.RemoveInside(Vector3I.Zero, new Vector3I(4, 1, 1));
        Assert.Equal(new[] { -1, 4 }, record.Cells.Select(c => c.Key.X).OrderBy(x => x));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 9, 1, 2, 3 })]
    [InlineData(new byte[] { 1, 1, 2, 3 })]
    public void MissingOrDamagedDataGivesAnEmptyRecord(byte[] data) =>
        Assert.Equal(0, GeneratedRecord.Decode(data).Count);

    [Fact]
    public void FullSizeTerrainCompressesWell()
    {
        // A height field like the demo's: 100×100 columns, every cell up to the surface filled.
        var record = new GeneratedRecord();
        var random = new Random(3);
        for (int z = 0; z < 100; z++)
            for (int x = 0; x < 100; x++)
            {
                int height = 2 + (int)(4 + 3 * Math.Sin(x * 0.2) + 3 * Math.Cos(z * 0.15));
                for (int y = 0; y < height; y++)
                    record.Set(new Vector3I(x, y, z), y + 1 < height ? 0 : random.Next(8), y + 1 < height ? 0 : new[] { 0, 16, 10, 22 }[random.Next(4)]);
            }
        int bytes = record.Encode().Length;
        Assert.True(bytes < 100_000, $"{record.Count} cells took {bytes} bytes.");
    }
}
