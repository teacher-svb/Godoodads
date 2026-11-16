namespace Godoodads.Tests.Curves;

using Godot;
using GdUnit4;
using Godoodads.Curves;
using static GdUnit4.Assertions;

// Brief godoocurves §10.4: settings stored in a Path3D's metadata survive a save and reload.
[TestSuite]
public class CurveSettingsTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Settings_stored_on_a_path_survive_a_save_and_reload_of_the_scene()
    {
        var root = new Node3D { Name = "Root" };
        var path = new Path3D { Name = "Path", Curve = new Curve3D() };
        root.AddChild(path);
        path.Owner = root;
        CurveSettings.StoreOn(path, new CurveSettings { ResourceName = "stored" });
        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string file = "user://curve_settings_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, file)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(file, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var stored = CurveSettings.StoredOn(loaded.GetNode<Path3D>("Path"));
        AssertThat(stored).IsNotNull();
        AssertThat(stored.ResourceName).IsEqual("stored");
        loaded.Free();
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(file));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Storing_null_removes_the_entry()
    {
        var path = AutoFree(new Path3D());
        CurveSettings.StoreOn(path, new CurveSettings());
        CurveSettings.StoreOn(path, null);
        AssertThat(path.HasMeta(CurveSettings.MetaKey)).IsFalse();
        AssertThat(CurveSettings.StoredOn(path)).IsNull();
    }
}
