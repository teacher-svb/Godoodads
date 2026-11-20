using System;
using System.Linq;
using Godot;
using Godoodads.Common.Wfc.Core;
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

    /// <summary>A record in the 1.2–1.4 format, with the pinned flag (0x80) on the given cells.</summary>
    private static byte[] OldFormat((Vector3I Position, int Item, int Orientation, bool Pinned)[] cells)
    {
        using var output = new System.IO.MemoryStream();
        output.WriteByte(1);
        using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        using (var writer = new System.IO.BinaryWriter(deflate))
        {
            writer.Write(cells.Length);
            foreach (var c in cells) writer.Write((short)c.Position.X);
            foreach (var c in cells) writer.Write((short)c.Position.Y);
            foreach (var c in cells) writer.Write((short)c.Position.Z);
            foreach (var c in cells) writer.Write(c.Item);
            foreach (var c in cells) writer.Write((byte)(c.Orientation | (c.Pinned ? 0x80 : 0)));
        }
        return output.ToArray();
    }

    [Fact]
    public void PinsOfOldRecordsAreDroppedAndNewRecordsWriteNone()
    {
        var old = OldFormat(new[] { (new Vector3I(1, 0, 0), 2, 22, true), (new Vector3I(2, 0, 0), 3, 10, false) });
        var decoded = GeneratedRecord.Decode(old);
        Assert.Equal(2, decoded.Count);
        Assert.True(decoded.Matches(new Vector3I(1, 0, 0), 2, 22));
        Assert.True(decoded.Matches(new Vector3I(2, 0, 0), 3, 10));

        var plain = new GeneratedRecord();
        plain.Set(new Vector3I(1, 0, 0), 2, 22);
        plain.Set(new Vector3I(2, 0, 0), 3, 10);
        Assert.Equal(plain.Encode(), decoded.Encode());
        Assert.Equal(OldFormat(new[] { (new Vector3I(1, 0, 0), 2, 22, false), (new Vector3I(2, 0, 0), 3, 10, false) }), plain.Encode());
    }

    [Fact]
    public void RemoveInsideKeepsCellsInsideTheGivenBoxes()
    {
        var record = new GeneratedRecord();
        for (int x = 0; x < 6; x++)
            record.Set(new Vector3I(x, 0, 0), x, 0);
        record.RemoveInside(Vector3I.Zero, new Vector3I(5, 1, 1),
            new[] { new CellBox(new Vector3I(1, 0, 0), Vector3I.One), new CellBox(new Vector3I(3, 0, 0), new Vector3I(9, 1, 1)) });
        Assert.Equal(new[] { 1, 3, 4, 5 }, record.Cells.Select(c => c.Key.X).OrderBy(x => x).ToArray());
        record.RemoveInside(Vector3I.Zero, new Vector3I(5, 1, 1));
        Assert.Equal(new[] { 5 }, record.Cells.Select(c => c.Key.X).ToArray());
    }

    [Fact]
    public void CellBoxesHoldTheirCellsOnly()
    {
        var box = new CellBox(new Vector3I(-1, 0, 2), new Vector3I(2, 1, 3));
        Assert.True(box.Contains(new Vector3I(-1, 0, 2)));
        Assert.True(box.Contains(new Vector3I(0, 0, 4)));
        Assert.False(box.Contains(new Vector3I(1, 0, 2)));
        Assert.False(box.Contains(new Vector3I(0, 1, 2)));
        Assert.False(new CellBox(Vector3I.Zero, new Vector3I(3, 0, 3)).Contains(Vector3I.Zero));
        Assert.False(CellBox.AnyContains(null, Vector3I.Zero));
        Assert.True(CellBox.AnyContains(new[] { new CellBox(Vector3I.One, Vector3I.One), box }, new Vector3I(0, 0, 3)));
    }
}
