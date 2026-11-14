using Godoodads.MultiMeshScatter.Core;
using Xunit;

namespace Godoodads.Tests;

// Brief §10.1: GridMap item names, exact or as wildcard patterns.
public class NameFilterTests
{
    private static readonly string[] Library =
    {
        "border_full", "border_edge_N", "border_edge_S", "border_outer_NE", "path_end_1", "path_end_2", "path_end_10", "floor", "Floor_big",
    };

    [Theory]
    [InlineData("border_full", "border_full", true)]
    [InlineData("border_full", "border_ful", false)]
    [InlineData("border_full", "Border_full", false)]   // case-sensitive, like the exact match
    [InlineData("border_edge_N", "border_*", true)]
    [InlineData("border_", "border_*", true)]            // * matches no characters too
    [InlineData("border_outer_NE", "*_NE", true)]
    [InlineData("border_outer_NE", "border*NE", true)]
    [InlineData("path_end_1", "path_end_?", true)]
    [InlineData("path_end_10", "path_end_?", false)]
    [InlineData("path_end_", "path_end_?", false)]       // ? needs exactly one character
    [InlineData("tile.v2", "tile?v2", false)]            // Godot's ? never matches a dot
    [InlineData("tile.v2", "tile*v2", true)]
    [InlineData("anything", "*", true)]
    [InlineData("", "", true)]                           // exact empty names still match themselves
    [InlineData("", "*", false)]                         // Godot's match never matches an empty string
    public void NamesMatchExactlyOrByPattern(string name, string entry, bool expected) =>
        Assert.Equal(expected, NameFilter.Matches(name, entry));

    [Fact]
    public void AnEmptyListAcceptsEveryName()
    {
        foreach (bool exclude in new[] { false, true })
            Assert.All(Library, n => Assert.True(new NameFilter(System.Array.Empty<string>(), exclude).Accepts(n)));
        Assert.True(new NameFilter(null, false).IsEmpty);
    }

    [Fact]
    public void IncludeUsesListedNamesAndExcludeTheOthers()
    {
        var entries = new[] { "border_*", "floor" };
        var include = new NameFilter(entries, false);
        var exclude = new NameFilter(entries, true);
        foreach (var name in Library)
        {
            bool listed = name.StartsWith("border_") || name == "floor";
            Assert.Equal(listed, include.Accepts(name));
            Assert.Equal(!listed, exclude.Accepts(name));
        }
    }

    [Fact]
    public void OverlappingPatternsListANameOnce()
    {
        var filter = new NameFilter(new[] { "border_*", "*_NE", "border_outer_NE", "border_edge_?" }, false);
        Assert.True(filter.Accepts("border_outer_NE"));
        Assert.True(filter.Accepts("border_edge_N"));
        Assert.False(filter.Accepts("path_end_1"));
        Assert.Empty(filter.Unmatched(Library));
        Assert.False(new NameFilter(new[] { "border_*", "*_NE" }, true).Accepts("border_outer_NE"));
    }

    [Fact]
    public void EntriesThatMatchNoNameAreReportedOnceInListOrder()
    {
        var filter = new NameFilter(new[] { "bordr_*", "path_end_?", "wall", "bordr_*", "floor_*" }, false);
        Assert.Equal(new[] { "bordr_*", "wall", "floor_*" }, filter.Unmatched(Library));   // Floor_big differs in case
        Assert.Equal(new[] { "x" }, new NameFilter(new[] { "x" }, true).Unmatched(System.Array.Empty<string>()));
    }
}
