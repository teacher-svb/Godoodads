using System.Linq;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Godoodads.Tests;

/// <summary>Prints layouts for the M1 review (dgn-m1-review) and checks that they are valid.</summary>
public class DungeonLayoutReviewTests
{
    private readonly ITestOutputHelper _output;

    public DungeonLayoutReviewTests(ITestOutputHelper output) => _output = output;

    public static readonly LayoutSettings Defaults = new();

    public static readonly LayoutSettings Sparse = new()
    {
        Bounds = new Vector2I(64, 64), RoomCount = 20, MinRoomSize = new Vector2I(4, 4), MaxRoomSize = new Vector2I(12, 12),
        ExtraConnections = 0.05, MaxDoorsPerRoom = 2,
    };

    [Theory]
    [InlineData("defaults", 1)]
    [InlineData("defaults", 2)]
    [InlineData("defaults", 3)]
    [InlineData("sparse", 1)]
    [InlineData("sparse", 2)]
    [InlineData("sparse", 3)]
    public void Prints_a_layout(string name, long seed)
    {
        var settings = name == "sparse" ? Sparse : Defaults;
        var layout = LayoutGenerator.Generate(settings, seed);
        Assert.True(layout.IsValid, string.Join("; ", layout.Errors));
        _output.WriteLine($"{name}, seed {seed}: {layout.Rooms.Count} rooms, {layout.Edges.Count(e => e.IsTreeEdge)} tree edges + "
            + $"{layout.Edges.Count(e => !e.IsTreeEdge)} loops, {layout.Doors.Count} doors. {string.Join(" ", layout.Warnings)}");
        _output.WriteLine(LayoutText.Render(layout, settings.Bounds));
    }
}
