namespace Godoodads.Tests.WfcGenerator;

using System.Reflection;
using System.Threading;
using Godot;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Core;
using Godoodads.WfcGenerator.Editor;
using GdUnit4;
using static GdUnit4.Assertions;

// Brief §13.2: the preview's Measure button, run without the editor UI.
[TestSuite]
public class WfcPreviewMeasureTests
{
    private static WfcTileSet TileSet(string side)
    {
        var library = new MeshLibrary();
        library.CreateItem(0);
        library.SetItemMesh(0, new BoxMesh());
        var tiles = new Godot.Collections.Array<WfcTile>
        {
            new() { Item = 0, SocketPosX = side, SocketNegX = side, SocketPosY = "v", SocketNegY = "v", SocketPosZ = "a", SocketNegZ = "a" },
        };
        return new WfcTileSet { Library = library, Tiles = tiles };
    }

    private static string Measure(WfcTileSet tileSet)
    {
        var preview = new TileSetPreview(tileSet);
        try
        {
            preview.Call("OnMeasurePressed");
            var job = typeof(TileSetPreview).GetField("_measureJob", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 500 && job.GetValue(preview) != null; i++)
            {
                Thread.Sleep(10);
                preview.Call("PollMeasure");
            }
            var text = (RichTextLabel)typeof(TileSetPreview).GetField("_sharesText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview);
            return text.Text;
        }
        finally
        {
            preview.Free();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_solvable_tile_set_shows_its_shares()
    {
        string text = Measure(TileSet("a"));
        AssertThat(text).Contains("Weight share / measured share");
        AssertThat(text).Contains("100.0% / 100.0%");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Stop_cancels_a_running_measurement()
    {
        var preview = new TileSetPreview(TileSet("a"));
        try
        {
            foreach (var (field, value) in new[] { ("_sampleX", 128), ("_sampleY", 64), ("_sampleZ", 128) })
                ((SpinBox)typeof(TileSetPreview).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview)).Value = value;
            preview.Call("OnMeasurePressed");
            preview.Call("OnMeasurePressed");
            var text = (RichTextLabel)typeof(TileSetPreview).GetField("_sharesText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview);
            AssertThat(text.Text).IsEqual("Measuring was stopped.");
            AssertThat(typeof(TileSetPreview).GetField("_measureJob", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preview)).IsNull();
        }
        finally
        {
            preview.Free();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void An_unsolvable_sample_shows_the_failure()
    {
        // "a+" needs an "a-" partner that no tile has, so no two cells can sit side by side.
        string text = Measure(TileSet("a+"));
        AssertThat(text).Contains("no solution for the sample");
        AssertThat(text).NotContains("measured share");
    }
}
