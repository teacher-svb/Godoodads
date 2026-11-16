namespace Godoodads.Tests.Curves;

using System;
using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Curves;
using Godoodads.Curves.Core;
using static GdUnit4.Assertions;

// Brief godoocurves §4.2, §4.3, §5: the settings resource the dialog shows and the Path3D stores.
[TestSuite]
public class CurveSettingsTests
{
    private static bool Shown(CurveSettings settings, string name) =>
        ((PropertyUsageFlags)settings.GetPropertyList().First(p => p["name"].AsString() == name)["usage"].AsInt64()).HasFlag(PropertyUsageFlags.Editor);

    private static string HintString(CurveSettings settings, string name) =>
        settings.GetPropertyList().First(p => p["name"].AsString() == name)["hint_string"].AsString();

    [TestCase]
    [RequireGodotRuntime]
    public void Only_the_shapes_settings_and_placement_in_replace_mode_are_shown()
    {
        var settings = new CurveSettings { Shape = ShapeKind.Loop };
        foreach (var name in new[] { "Length", "Diameter", "SideShift", "Start", "Direction", "Up", "Mode", "Shape" })
            AssertThat(Shown(settings, name)).OverrideFailureMessage(name).IsTrue();
        foreach (var name in new[] { "Radius", "Bend", "Turns", "Width", "Sides" })
            AssertThat(Shown(settings, name)).OverrideFailureMessage(name).IsFalse();

        settings.Mode = CurveMode.Append;
        foreach (var name in new[] { "Start", "Direction", "Up" })
            AssertThat(Shown(settings, name)).OverrideFailureMessage(name).IsFalse();
        AssertThat(Shown(settings, "Length")).IsTrue();

        settings.Shape = ShapeKind.Polygon;
        foreach (var name in new[] { "Sides", "Radius", "CornerRadius", "Bend" })
            AssertThat(Shown(settings, name)).OverrideFailureMessage(name).IsTrue();
        AssertThat(Shown(settings, "Length")).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Every_shape_lists_settings_that_exist()
    {
        var properties = new CurveSettings().GetPropertyList().Select(p => p["name"].AsString()).ToHashSet();
        foreach (var kind in Enum.GetValues<ShapeKind>())
        {
            AssertThat(CurveSettings.UsedSettings.ContainsKey(kind)).OverrideFailureMessage(kind.ToString()).IsTrue();
            foreach (var name in CurveSettings.UsedSettings[kind])
                AssertThat(properties.Contains(name)).OverrideFailureMessage(name).IsTrue();
        }
        AssertThat(CurveSettings.ShapeLabels.Length).IsEqual(Enum.GetValues<ShapeKind>().Length);
        string shapes = HintString(new CurveSettings(), "Shape");
        foreach (var label in new[] { "S-bend:8", "Banked Turn:12", "Figure Eight:15" })
            AssertThat(shapes).Contains(label);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Shapes_that_turn_around_up_offer_only_left_and_right()
    {
        AssertThat(HintString(new CurveSettings { Shape = ShapeKind.Helix }, "Bend")).IsEqual("Left:0,Right:1");
        AssertThat(HintString(new CurveSettings { Shape = ShapeKind.BankedTurn }, "Bend")).IsEqual("Left:0,Right:1");
        AssertThat(HintString(new CurveSettings { Shape = ShapeKind.Arc }, "Bend")).Contains("Up");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void To_parameters_copies_every_setting()
    {
        var settings = new CurveSettings
        {
            Length = 11, Height = 12, Radius = 13, Diameter = 14, SideShift = 15, Amplitude = 16, Cycles = 17, Turns = 18,
            Angle = 19, Offset = 20, RadiusForward = 21, RadiusSide = 22, Rise = 23, Bank = 24, Width = 25, Depth = 26,
            CornerRadius = 27, Sides = 28, Bend = Bend.Down,
        };
        var p = settings.ToParameters();
        var names = typeof(ShapeParameters).GetProperties().Select(property => property.Name).ToList();
        foreach (var name in names)
        {
            string setting = name switch { "AngleDegrees" => "Angle", "BankDegrees" => "Bank", _ => name };
            var expected = typeof(CurveSettings).GetProperty(setting)!.GetValue(settings);
            var actual = typeof(ShapeParameters).GetProperty(name)!.GetValue(p);
            AssertThat(Equals(actual, expected)).OverrideFailureMessage($"{name}: {actual} instead of {expected}").IsTrue();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Check_reports_closed_shapes_in_append_mode_and_bad_frames()
    {
        AssertThat(new CurveSettings().Check()).IsNull();
        AssertThat(new CurveSettings { Shape = ShapeKind.Circle, Mode = CurveMode.Append }.Check()).StartsWith("Circle is a closed shape");
        AssertThat(new CurveSettings { Shape = ShapeKind.Circle }.Check()).IsNull();
        AssertThat(new CurveSettings { Direction = Vector3.Up }.Check()).StartsWith("Up is zero or parallel");
        AssertThat(new CurveSettings { Direction = Vector3.Up, Mode = CurveMode.Append }.Check()).IsNull();   // the curve's end decides there
        AssertThat(new CurveSettings { Shape = ShapeKind.Helix, Bend = Bend.Up }.Check()).StartsWith("Bend must be Left or Right");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Settings_stored_on_a_path_survive_a_save_and_reload_of_the_scene()
    {
        var root = new Node3D { Name = "Root" };
        var path = new Path3D { Name = "Path", Curve = new Curve3D() };
        root.AddChild(path);
        path.Owner = root;
        CurveSettings.StoreOn(path, new CurveSettings
        {
            Mode = CurveMode.Append, Shape = ShapeKind.BankedTurn, Radius = 7.5f, Angle = 135, Bank = -20, Bend = Bend.Left,
            Start = new Vector3(1, 2, 3), Direction = new Vector3(0, 0, -1), Up = new Vector3(0, 1, 0.1f),
        });
        var packed = new PackedScene();
        AssertThat(packed.Pack(root)).IsEqual(Error.Ok);
        const string file = "user://curve_settings_roundtrip.tscn";
        AssertThat(ResourceSaver.Save(packed, file)).IsEqual(Error.Ok);
        root.Free();

        var loaded = ResourceLoader.Load<PackedScene>(file, null, ResourceLoader.CacheMode.Ignore).Instantiate();
        var stored = CurveSettings.StoredOn(loaded.GetNode<Path3D>("Path"));
        AssertThat(stored).IsNotNull();
        AssertThat(stored.Mode).IsEqual(CurveMode.Append);
        AssertThat(stored.Shape).IsEqual(ShapeKind.BankedTurn);
        AssertThat(stored.Radius).IsEqual(7.5f);
        AssertThat(stored.Angle).IsEqual(135f);
        AssertThat(stored.Bank).IsEqual(-20f);
        AssertThat(stored.Bend).IsEqual(Bend.Left);
        AssertThat(stored.Start).IsEqual(new Vector3(1, 2, 3));
        AssertThat(stored.Direction).IsEqual(new Vector3(0, 0, -1));
        AssertThat(stored.Up).IsEqual(new Vector3(0, 1, 0.1f));
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
