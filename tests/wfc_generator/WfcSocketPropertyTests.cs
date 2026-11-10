namespace Godoodads.Tests.WfcGenerator;

using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Core;
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
}
