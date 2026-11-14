using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godoodads.MultiMeshScatter.Core;

/// <summary>
/// The GridMap item filter (brief §10.1): a list of item names to include or exclude, where an
/// entry is an exact name or a glob pattern with Godot's <c>String.match</c> semantics
/// (<c>*</c> matches any run of characters, <c>?</c> one character other than <c>.</c>),
/// case-sensitive. An empty list accepts every name.
/// </summary>
public sealed class NameFilter
{
    private readonly string[] _entries;
    private readonly bool _exclude;

    public NameFilter(IEnumerable<string> entries, bool exclude)
    {
        _entries = (entries ?? Enumerable.Empty<string>()).Select(e => e ?? "").ToArray();
        _exclude = exclude;
    }

    public bool IsEmpty => _entries.Length == 0;

    /// <summary>
    /// True when <paramref name="name"/> equals <paramref name="entry"/> or matches it as a pattern.
    /// The equality check keeps exact names, also empty ones, working as before 1.2.0.
    /// </summary>
    public static bool Matches(string name, string entry)
    {
        name ??= "";
        entry ??= "";
        return name == entry || name.Match(entry);
    }

    /// <summary>True when any entry matches the name.</summary>
    public bool IsListed(string name) => _entries.Any(e => Matches(name, e));

    /// <summary>Whether an item with this name is used: listed for Include, not listed for Exclude.</summary>
    public bool Accepts(string name) => IsEmpty || IsListed(name) != _exclude;

    /// <summary>The entries, in list order and without repeats, that match none of <paramref name="names"/>.</summary>
    public List<string> Unmatched(IEnumerable<string> names)
    {
        var all = (names ?? Enumerable.Empty<string>()).ToArray();
        return _entries.Distinct().Where(e => !all.Any(n => Matches(n, e))).ToList();
    }
}
