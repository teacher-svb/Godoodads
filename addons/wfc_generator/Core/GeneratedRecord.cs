using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Godot;

namespace Godoodads.WfcGenerator.Core;

/// <summary>
/// The non-empty cells a generation wrote, so the next one can replace them (brief §11.2). Stored
/// compressed: positions sorted, then each field as its own plane, which Deflate packs well.
/// Positions must fit in 16 bits, which the dialog's bounds check guarantees.
/// </summary>
public sealed class GeneratedRecord
{
    private const byte FormatVersion = 1;

    private readonly Dictionary<Vector3I, (int Item, int Orientation)> _cells = new();

    public int Count => _cells.Count;

    public IEnumerable<KeyValuePair<Vector3I, (int Item, int Orientation)>> Cells => _cells;

    public void Set(Vector3I position, int item, int orientation) => _cells[position] = (item, orientation);

    public void Remove(Vector3I position) => _cells.Remove(position);

    /// <summary>True when the cell still holds exactly what the generation wrote.</summary>
    public bool Matches(Vector3I position, int item, int orientation) =>
        _cells.TryGetValue(position, out var written) && written.Item == item && written.Orientation == orientation;

    public bool Contains(Vector3I position) => _cells.ContainsKey(position);

    /// <summary>Removes every cell inside the box from origin (inclusive) to origin + size (exclusive).</summary>
    public void RemoveInside(Vector3I origin, Vector3I size)
    {
        var end = origin + size;
        foreach (var position in _cells.Keys.ToArray())
            if (position.X >= origin.X && position.Y >= origin.Y && position.Z >= origin.Z
                && position.X < end.X && position.Y < end.Y && position.Z < end.Z)
                _cells.Remove(position);
    }

    public byte[] Encode()
    {
        var sorted = _cells.OrderBy(c => c.Key.Y).ThenBy(c => c.Key.Z).ThenBy(c => c.Key.X).ToArray();
        using var output = new MemoryStream();
        output.WriteByte(FormatVersion);
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        using (var writer = new BinaryWriter(deflate))
        {
            writer.Write(sorted.Length);
            foreach (var c in sorted) writer.Write(checked((short)c.Key.X));
            foreach (var c in sorted) writer.Write(checked((short)c.Key.Y));
            foreach (var c in sorted) writer.Write(checked((short)c.Key.Z));
            foreach (var c in sorted) writer.Write(c.Value.Item);
            foreach (var c in sorted) writer.Write(checked((byte)c.Value.Orientation));
        }
        return output.ToArray();
    }

    /// <summary>Reads an encoded record. Null, empty or unreadable data gives an empty record.</summary>
    public static GeneratedRecord Decode(byte[] data)
    {
        var record = new GeneratedRecord();
        if (data == null || data.Length < 2 || data[0] != FormatVersion)
            return record;
        try
        {
            using var input = new MemoryStream(data, 1, data.Length - 1);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var reader = new BinaryReader(deflate);
            int count = reader.ReadInt32();
            if (count < 0)
                return record;
            var x = new short[count];
            var y = new short[count];
            var z = new short[count];
            for (int i = 0; i < count; i++) x[i] = reader.ReadInt16();
            for (int i = 0; i < count; i++) y[i] = reader.ReadInt16();
            for (int i = 0; i < count; i++) z[i] = reader.ReadInt16();
            var items = new int[count];
            for (int i = 0; i < count; i++) items[i] = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                record.Set(new Vector3I(x[i], y[i], z[i]), items[i], reader.ReadByte());
        }
        catch (Exception e) when (e is IOException or InvalidDataException or EndOfStreamException)
        {
            return new GeneratedRecord();
        }
        return record;
    }
}
