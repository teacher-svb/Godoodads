using System.Linq;

namespace Godoodads.WfcGenerator.Core;

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
