namespace Godoodads.Tests.Wave;

using System.Linq;
using Godot;
using Godoodads.Common.Wfc;
using Godoodads.Common.Wfc.Core;
using GdUnit4;
using static GdUnit4.Assertions;

// Brief §13.1: the preview edits a socket through the property of the tile's own face.
[TestSuite]
public class WfcSocketPropertyTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Each_direction_maps_to_its_socket_property()
    {
        var tile = new WfcTile
        {
            SocketPosX = "px", SocketNegX = "nx", SocketPosY = "py", SocketNegY = "ny", SocketPosZ = "pz", SocketNegZ = "nz",
        };
        var sockets = tile.ToDefinition().Sockets;
        foreach (int direction in Directions.All3D)
        {
            AssertThat(tile.GetSocket(direction)).IsEqual(sockets[direction]);
            tile.Set(WfcTile.SocketProperty(direction), "edited");
            AssertThat(tile.ToDefinition().Sockets[direction]).IsEqual("edited");
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void An_edit_on_a_rotated_variant_changes_the_tiles_own_face()
    {
        // A quarter-turned variant shows the tile's +X face at -Z.
        var tile = new WfcTile { SocketPosX = "old", Rotation = TileRotation.FourWay };
        int local = SocketEditing.LocalFace(Directions.NegZ, 1);
        tile.Set(WfcTile.SocketProperty(local), SocketEditing.ToLocalLabel("new", Directions.NegZ, 1));
        AssertThat(tile.SocketPosX).IsEqual("new");
        AssertThat(tile.SocketNegZ).IsEqual("");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Socket_fields_show_the_comma_list_format_when_empty()
    {
        // godoowave 1.5: the inspector shows the placeholder in empty socket fields.
        var properties = new WfcTile().GetPropertyList();
        foreach (int direction in Directions.All3D)
        {
            string name = WfcTile.SocketProperty(direction);
            var property = properties.First(p => p["name"].AsString() == name);
            AssertThat(property["hint"].AsInt32()).IsEqual((int)PropertyHint.PlaceholderText);
            AssertThat(property["hint_string"].AsString()).IsEqual(WfcTile.SocketPlaceholder);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_face_with_a_list_fits_next_to_each_listed_neighbour()
    {
        var tiles = new[]
        {
            new WfcTile { Item = 0, SocketPosX = "wall,window", SocketNegX = "x", SocketPosY = "v", SocketNegY = "v", SocketPosZ = "x", SocketNegZ = "x" },
            new WfcTile { Item = 1, SocketPosX = "x", SocketNegX = "window", SocketPosY = "v", SocketNegY = "v", SocketPosZ = "x", SocketNegZ = "x" },
        };
        var set = CompiledTileSet.Compile(tiles.Select(t => t.ToDefinition()).ToArray(), Directions.All3D);
        AssertThat(set.IsValid).IsTrue();
        AssertThat(set.Allows(0, Directions.PosX, 1)).IsTrue();
    }
}
