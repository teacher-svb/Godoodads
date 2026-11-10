namespace Godoodads.Tests.MultiMeshScatter;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter;
using static GdUnit4.Assertions;

// Brief §10.1: the GridMap group of ScatterSettings.
[TestSuite]
public class GridMapSettingsTests
{
    private static PropertyUsageFlags UsageOf(ScatterSettings settings, string name) =>
        (PropertyUsageFlags)settings.GetPropertyList().First(p => p["name"].AsString() == name)["usage"].AsInt32();

    [TestCase]
    [RequireGodotRuntime]
    public void GridMap_options_round_trip_and_old_presets_get_defaults()
    {
        const string path = "user://scatter_gridmap_settings.tres";
        var settings = new ScatterSettings
        {
            GridMapItems = new[] { "Grass", "Ramp" }, GridMapItemMode = GridMapItemMode.Exclude,
            GridMapBoxOrigin = new Vector3I(-2, 0, 3), GridMapBoxSize = new Vector3I(8, 4, 8),
        };
        AssertThat(ResourceSaver.Save(settings, path)).IsEqual(Error.Ok);
        var loaded = ResourceLoader.Load<ScatterSettings>(path, null, ResourceLoader.CacheMode.Ignore);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        var selection = loaded.ToGridMapSelection();
        AssertThat(selection.Items.ToArray()).IsEqual(new[] { "Grass", "Ramp" });
        AssertThat(selection.Exclude).IsTrue();
        AssertThat(selection.HasBox).IsTrue();
        AssertThat(selection.Contains(new Vector3I(5, 3, 10))).IsTrue();
        AssertThat(selection.Contains(new Vector3I(6, 3, 10))).IsFalse();

        var legacy = GD.Load<ScatterSettings>("res://addons/multimesh_scatter/Presets/grass_on_terrain.tres");
        AssertThat(legacy.GridMapItems.Length).IsEqual(0);
        AssertThat(legacy.ToGridMapSelection().HasBox).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void The_GridMap_group_is_shown_only_for_GridMap_surfaces()
    {
        var settings = new ScatterSettings();
        AssertThat(UsageOf(settings, "GridMapItems").HasFlag(PropertyUsageFlags.Editor)).IsFalse();
        AssertThat(UsageOf(settings, "GridMapItems").HasFlag(PropertyUsageFlags.Storage)).IsTrue();
        settings.SurfaceIsGridMap = true;
        AssertThat(UsageOf(settings, "GridMapItems").HasFlag(PropertyUsageFlags.Editor)).IsTrue();
        AssertThat(UsageOf(settings, "GridMapBoxSize").HasFlag(PropertyUsageFlags.Editor)).IsTrue();
        AssertThat(((ScatterSettings)settings.Duplicate()).SurfaceIsGridMap).IsFalse();   // not saved
    }
}
