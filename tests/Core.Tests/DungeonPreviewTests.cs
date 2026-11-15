using System.Collections.Generic;
using Godot;
using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonPreviewTests
{
    private static DungeonRequest Request(long seed) => new()
    {
        Layout = new LayoutSettings { Bounds = new Vector2I(40, 40), RoomCount = 8 },
        Origin = new Vector3I(2, 0, -3),
        RoomTiles = DungeonTestTiles.Room(),
        DoorTile = DungeonTestTiles.Door,
        CorridorTiles = DungeonTestTiles.Corridor(),
        Seed = seed,
    };

    [Fact]
    public void The_preview_shows_the_layout_and_corridors_generate_uses()
    {
        for (long seed = 0; seed < 20; seed++)
        {
            var preview = DungeonPreview.Plan(Request(seed));
            var generated = new DungeonJob(Request(seed)).RunNow();
            Assert.True(preview.IsValid);
            Assert.Equal(DungeonStatus.Solved, generated.Status);
            Assert.Equal(generated.LayoutAttempt, preview.LayoutAttempt);
            Assert.Equal(generated.Layout.Rooms, preview.Layout.Rooms);
            Assert.Equal(generated.Layout.Doors, preview.Layout.Doors);
            Assert.Equal(generated.CorridorFaces, preview.Corridors.Faces);
        }
    }

    [Fact]
    public void Invalid_settings_and_blocked_corridors_explain_themselves()
    {
        var invalid = DungeonPreview.Plan(Request(1) with { Layout = new LayoutSettings { RoomCount = 1 } });
        Assert.False(invalid.IsValid);
        Assert.StartsWith("Room Count is 1", Assert.Single(invalid.Errors));

        var request = Request(2) with { LayoutAttempts = 2 };
        var wall = new HashSet<Vector3I>();
        for (int z = 0; z < 40; z++)
            wall.Add(request.Origin + new Vector3I(20, 0, z));
        var blocked = DungeonPreview.Plan(request with { HandPlaced = wall });
        Assert.False(blocked.IsValid);
        Assert.Equal(2, blocked.Failures.Count);
        Assert.All(blocked.Failures, f => Assert.StartsWith("Corridors: No corridor could join", f.Message));
    }
}
