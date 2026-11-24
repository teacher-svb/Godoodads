using System;
using System.Collections.Generic;
using System.Linq;

namespace Godoodads.Common.Wfc.Core;

public enum SocketKind
{
    HorizontalSymmetric,
    HorizontalPlus,
    HorizontalMinus,
    VerticalAny,
    VerticalRotated,
}

/// <summary>
/// A parsed socket label. Horizontal faces use <c>path</c>, <c>path+</c> and <c>path-</c>;
/// vertical faces use <c>stair</c> and <c>stair@0</c> to <c>stair@3</c>.
/// </summary>
public readonly record struct Socket(string Name, SocketKind Kind, int Rotation = 0)
{
    public static bool TryParse(string label, bool vertical, out Socket socket, out string error)
    {
        socket = default;
        error = null;
        if (string.IsNullOrEmpty(label))
        {
            error = "The socket label is empty.";
            return false;
        }

        string name = label;
        var kind = vertical ? SocketKind.VerticalAny : SocketKind.HorizontalSymmetric;
        int rotation = 0;

        if (vertical)
        {
            int at = label.IndexOf('@');
            if (at >= 0)
            {
                string suffix = label[(at + 1)..];
                if (suffix.Length != 1 || suffix[0] < '0' || suffix[0] > '3')
                {
                    error = $"'{label}': a vertical rotation must be @0, @1, @2 or @3.";
                    return false;
                }
                name = label[..at];
                kind = SocketKind.VerticalRotated;
                rotation = suffix[0] - '0';
            }
        }
        else if (label.EndsWith('+') || label.EndsWith('-'))
        {
            name = label[..^1];
            kind = label.EndsWith('+') ? SocketKind.HorizontalPlus : SocketKind.HorizontalMinus;
        }

        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            error = $"'{label}' is not a valid {(vertical ? "vertical" : "horizontal")} socket. "
                + "Names use letters, digits and underscores.";
            return false;
        }

        socket = new Socket(name, kind, rotation);
        return true;
    }

    /// <summary>True when this socket can face <paramref name="other"/>.</summary>
    public bool Matches(Socket other)
    {
        if (Name != other.Name)
            return false;
        return Kind switch
        {
            SocketKind.HorizontalSymmetric => other.Kind == SocketKind.HorizontalSymmetric,
            SocketKind.HorizontalPlus => other.Kind == SocketKind.HorizontalMinus,
            SocketKind.HorizontalMinus => other.Kind == SocketKind.HorizontalPlus,
            SocketKind.VerticalAny => other.Kind == SocketKind.VerticalAny,
            SocketKind.VerticalRotated => other.Kind == SocketKind.VerticalRotated && Rotation == other.Rotation,
            _ => false,
        };
    }

    /// <summary>The socket after the tile turns around Y. Only rotation-specific vertical sockets change.</summary>
    public Socket RotatedY(int quarterTurns) => Kind == SocketKind.VerticalRotated
        ? this with { Rotation = (((Rotation + quarterTurns) % 4) + 4) % 4 }
        : this;

    public override string ToString() => Kind switch
    {
        SocketKind.HorizontalPlus => Name + "+",
        SocketKind.HorizontalMinus => Name + "-",
        SocketKind.VerticalRotated => $"{Name}@{Rotation}",
        _ => Name,
    };
}

/// <summary>
/// The sockets on one face: one or more labels separated by commas, such as <c>wall, window</c>
/// (spaces around the commas are optional). Two faces fit when any label on one matches any
/// label on the other.
/// </summary>
public readonly struct SocketSet : IEquatable<SocketSet>
{
    public const char Separator = ',';

    /// <summary>The rule shown next to socket fields in the editor.</summary>
    public const string Help = "One or more socket labels separated by commas, such as 'wall, window'. "
        + "Faces fit when any label on one matches any label on the other.";

    private readonly Socket[] _labels;

    public SocketSet(params Socket[] labels) => _labels = labels ?? Array.Empty<Socket>();

    public IReadOnlyList<Socket> Labels => _labels ?? Array.Empty<Socket>();

    public static implicit operator SocketSet(Socket socket) => new(socket);

    /// <summary>Splits <paramref name="label"/> on commas and parses each entry with <see cref="Socket.TryParse"/>.</summary>
    public static bool TryParse(string label, bool vertical, out SocketSet set, out string error)
    {
        set = default;
        error = null;
        if (string.IsNullOrWhiteSpace(label))
        {
            error = "The socket label is empty.";
            return false;
        }

        string[] entries = label.Split(Separator);
        var labels = new List<Socket>(entries.Length);
        foreach (string raw in entries)
        {
            string entry = raw.Trim();
            if (entry.Length == 0)
            {
                error = $"'{label.Trim()}' has an empty entry. Separate socket labels with single commas.";
                return false;
            }
            if (!Socket.TryParse(entry, vertical, out var socket, out error))
                return false;
            if (labels.Contains(socket))
            {
                error = $"'{label.Trim()}' lists '{entry}' twice.";
                return false;
            }
            labels.Add(socket);
        }
        set = new SocketSet(labels.ToArray());
        return true;
    }

    /// <summary>True when any label of this face can face any label of <paramref name="other"/>.</summary>
    public bool Matches(SocketSet other)
    {
        foreach (var a in Labels)
            foreach (var b in other.Labels)
                if (a.Matches(b))
                    return true;
        return false;
    }

    /// <summary>True when any label of this face can face <paramref name="other"/>.</summary>
    public bool Matches(Socket other) => Labels.Any(a => a.Matches(other));

    /// <summary>True when any label has the name <paramref name="name"/>, in any +/-/@n form.</summary>
    public bool HasName(string name) => Labels.Any(l => l.Name == name);

    public SocketSet RotatedY(int quarterTurns) => new(Labels.Select(l => l.RotatedY(quarterTurns)).ToArray());

    public bool Equals(SocketSet other) => Labels.SequenceEqual(other.Labels);

    public override bool Equals(object obj) => obj is SocketSet other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var label in Labels)
            hash.Add(label);
        return hash.ToHashCode();
    }

    public static bool operator ==(SocketSet a, SocketSet b) => a.Equals(b);

    public static bool operator !=(SocketSet a, SocketSet b) => !a.Equals(b);

    public override string ToString() => string.Join(Separator + " ", Labels);
}
