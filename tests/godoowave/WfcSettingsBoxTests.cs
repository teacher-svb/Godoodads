namespace Godoodads.Tests.Wave;

using Godot;
using GdUnit4;
using Godoodads.Common.Wfc;
using static GdUnit4.Assertions;

// 1.4.1: a box drag's undo step targets the settings it changed, and a run keeps its own copy of
// the settings. The plugin needs the editor's undo manager, so these check the parts it relies on.
[TestSuite]
public class WfcSettingsBoxTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Undoing_a_box_resize_changes_only_the_settings_that_were_dragged()
    {
        // The same do and undo pair as the plugin's CommitBox, on a plain UndoRedo.
        var dragged = new WfcSettings { BoundsOrigin = new Vector3I(1, 2, 3), BoundsSize = new Vector3I(4, 5, 6) };
        var other = new WfcSettings { BoundsOrigin = new Vector3I(-7, 0, 7), BoundsSize = new Vector3I(9, 9, 9) };
        var undo = new UndoRedo();

        foreach (int box in new[] { 0, 1 })
        {
            var (fromOrigin, fromSize) = (dragged.BoxOrigin(box), dragged.BoxSize(box));
            dragged.SetBox(box, new Vector3I(10, 0, 10), new Vector3I(2, 2, 2));   // as the drag left it
            var (toOrigin, toSize) = (dragged.BoxOrigin(box), dragged.BoxSize(box));
            // Called by name, as the editor's undo manager calls it after an assembly reload.
            undo.CreateAction("Resize");
            undo.AddDoMethod(Callable.From(() => dragged.Call(WfcSettings.MethodName.SetBox, box, toOrigin, toSize)));
            undo.AddUndoMethod(Callable.From(() => dragged.Call(WfcSettings.MethodName.SetBox, box, fromOrigin, fromSize)));
            undo.CommitAction(false);
        }
        AssertThat(dragged.HasPinBox).IsTrue();

        // The dialog is opened for another GridMap, so it edits other settings; then Ctrl+Z twice.
        undo.Undo();
        undo.Undo();
        AssertThat(dragged.BoundsOrigin).IsEqual(new Vector3I(1, 2, 3));
        AssertThat(dragged.BoundsSize).IsEqual(new Vector3I(4, 5, 6));
        AssertThat(dragged.HasPinBox).IsFalse();
        AssertThat(other.BoundsOrigin).IsEqual(new Vector3I(-7, 0, 7));
        AssertThat(other.BoundsSize).IsEqual(new Vector3I(9, 9, 9));
        AssertThat(other.HasPinBox).IsFalse();

        undo.Redo();
        AssertThat(dragged.BoundsOrigin).IsEqual(new Vector3I(10, 0, 10));
        AssertThat(other.BoundsOrigin).IsEqual(new Vector3I(-7, 0, 7));
        undo.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_runs_settings_copy_keeps_its_values_when_the_dialog_settings_change()
    {
        var tileSet = new WfcTileSet();
        var dialog = new WfcSettings
        {
            TileSet = tileSet, Seed = 5, BoundsOrigin = new Vector3I(0, 0, 0), BoundsSize = new Vector3I(8, 2, 8),
            BoundaryNegY = "rock", PathSocket = "path",
        };
        dialog.PlacePinBox();
        var run = (WfcSettings)dialog.Duplicate();   // as OnActionPressed takes it

        // Edits made in the inspector and with the handles while the solve runs.
        dialog.Seed = 6;
        dialog.SetBox(0, new Vector3I(3, 0, 3), new Vector3I(20, 4, 20));
        dialog.SetBox(1, new Vector3I(1, 1, 1), new Vector3I(1, 1, 1));
        dialog.BoundaryNegY = "";
        dialog.PathSocket = "road";

        AssertThat(run.Seed).IsEqual(5L);
        AssertThat(run.BoundsOrigin).IsEqual(Vector3I.Zero);
        AssertThat(run.BoundsSize).IsEqual(new Vector3I(8, 2, 8));
        AssertThat(run.PinOrigin).IsEqual(Vector3I.Zero);
        AssertThat(run.PinSize).IsEqual(new Vector3I(8, 2, 8));
        AssertThat(run.BoundaryNegY).IsEqual("rock");
        AssertThat(run.PathSocket).IsEqual("path");
        AssertThat(run.TileSet == tileSet).IsTrue();   // the tile set asset is shared, not copied
    }
}
