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
/// Positions must fit in 16 bits, which the dialog's bounds check guarantees. A pinned cell
/// (brief §12.3) is kept by the next generation like a hand-placed one; the flag is the top bit of
/// the orientation byte, which 1.1 never set.
/// </summary>
public sealed class GeneratedRecord
{
    private const byte FormatVersion = 1;
    private const int PinnedBit = 0x80;

    private readonly Dictionary<Vector3I, (int Item, int Orientation, bool Pinned)> _cells = new();

    public int Count => _cells.Count;

    public IEnumerable<KeyValuePair<Vector3I, (int Item, int Orientation, bool Pinned)>> Cells => _cells;

    public void Set(Vector3I position, int item, int orientation, bool pinned = false) => _cells[position] = (item, orientation, pinned);

    public bool IsPinned(Vector3I position) => _cells.TryGetValue(position, out var written) && written.Pinned;

    /// <summary>Pins or unpins the recorded cells inside a box that pass the filter. Returns how many changed.</summary>
    public int SetPinnedInside(Vector3I origin, Vector3I size, bool pinned, Func<Vector3I, (int Item, int Orientation), bool> filter = null)
    {
        int changed = 0;
        foreach (var (position, written) in _cells.ToArray())
        {
            if (written.Pinned == pinned || !Inside(position, origin, size)
                || (filter != null && !filter(position, (written.Item, written.Orientation))))
                continue;
            _cells[position] = written with { Pinned = pinned };
            changed++;
        }
        return changed;
    }

    public void Remove(Vector3I position) => _cells.Remove(position);

    /// <summary>True when the cell still holds exactly what the generation wrote.</summary>
    public bool Matches(Vector3I position, int item, int orientation) =>
        _cells.TryGetValue(position, out var written) && written.Item == item && written.Orientation == orientation;

    public bool Contains(Vector3I position) => _cells.ContainsKey(position);

    /// <summary>
    /// Removes every cell inside the box from origin (inclusive) to origin + size (exclusive),
    /// except pinned cells when <paramref name="keepPinned"/> is set.
    /// </summary>
    public void RemoveInside(Vector3I origin, Vector3I size, bool keepPinned = false)
    {
        foreach (var (position, written) in _cells.ToArray())
            if (Inside(position, origin, size) && !(keepPinned && written.Pinned))
                _cells.Remove(position);
    }

    private static bool Inside(Vector3I p, Vector3I origin, Vector3I size)
    {
        var end = origin + size;
        return p.X >= origin.X && p.Y >= origin.Y && p.Z >= origin.Z && p.X < end.X && p.Y < end.Y && p.Z < end.Z;
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
            foreach (var c in sorted) writer.Write((byte)(checked((byte)c.Value.Orientation) | (c.Value.Pinned ? PinnedBit : 0)));
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
            {
                int flags = reader.ReadByte();
                record.Set(new Vector3I(x[i], y[i], z[i]), items[i], flags & ~PinnedBit, (flags & PinnedBit) != 0);
            }
        }
        catch (Exception e) when (e is IOException or InvalidDataException or EndOfStreamException)
        {
            return new GeneratedRecord();
        }
        return record;
    }
}
