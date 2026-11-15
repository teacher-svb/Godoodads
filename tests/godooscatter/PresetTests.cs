namespace Godoodads.Tests.Scatter;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Scatter;
using Godoodads.Scatter.Adapters;
using Godoodads.Scatter.Core;
using static GdUnit4.Assertions;

[TestSuite]
public class PresetTests
{
    // Brief §4.5: the legacy preset reproduces the reference plugin, which only populated faces
    // within about 45 degrees of +Y and always kept instances upright.
    [TestCase]
    [RequireGodotRuntime]
    public void Legacy_preset_populates_only_upward_faces_upright()
    {
        var preset = GD.Load<ScatterSettings>("res://addons/godooscatter/Presets/upward_faces_legacy.tres");
        AssertThat(preset).IsNotNull();
        AssertThat(preset.SlopeMaxDegrees).IsEqual(45f);
        AssertThat(preset.Alignment).IsEqual(1f);
        AssertThat(preset.NormalSource).IsEqual(NormalSource.Face);

        var surface = SurfaceSource.FromMesh(new BoxMesh { Size = new Vector3(2, 2, 2) }, Transform3D.Identity);
        var options = preset.ToOptions();
        options.Amount = 300;
        var result = ScatterPipeline.Run(surface, options);

        AssertThat(result.Placed).IsEqual(300);
        AssertThat(result.Transforms.All(t => Mathf.Abs(t.Origin.Y - 1f) < 1e-4f)).IsTrue();
        AssertThat(result.Transforms.All(t => t.Basis.Y.Normalized().Dot(Vector3.Up) > 0.9998f)).IsTrue();
    }

    // Brief §4.7: the four shipped presets load, are named, and keep no surface path.
    [TestCase]
    [RequireGodotRuntime]
    public void Shipped_presets_load()
    {
        var expected = new[] { "Upward faces (legacy)", "Follow surface", "Grass on terrain", "Masked scatter" };
        var dir = "res://addons/godooscatter/Presets";
        var names = DirAccess.GetFilesAt(dir).Where(f => f.EndsWith(".tres"))
            .Select(f => GD.Load<ScatterSettings>(dir.PathJoin(f)))
            .Select(p => { AssertThat(p.SurfacePath.IsEmpty).IsTrue(); return p.ResourceName; })
            .ToArray();
        AssertThat(names.OrderBy(n => n).ToArray()).IsEqual(expected.OrderBy(n => n).ToArray());

        var follow = GD.Load<ScatterSettings>(dir.PathJoin("follow_surface.tres"));
        AssertThat(follow.Alignment).IsEqual(0f);
        AssertThat(follow.NormalSource).IsEqual(NormalSource.Smooth);
        AssertThat(GD.Load<ScatterSettings>(dir.PathJoin("masked_scatter.tres")).MaskMode).IsEqual(MaskMode.Density);
        AssertThat(GD.Load<ScatterSettings>(dir.PathJoin("grass_on_terrain.tres")).MinDistance).IsEqual(0.15f);
    }

    // Brief §11: settings saved before the move to addons/godooscatter keep loading. Files saved in
    // the editor carry the script's uid and find the moved class; files without one load the legacy
    // subclass left at the old path.
    private const string OldPathFile = """
        [gd_resource type="Resource" script_class="ScatterSettings" format=3]

        [ext_resource type="Script" {0}path="res://addons/multimesh_scatter/Resources/ScatterSettings.cs" id="1"]

        [resource]
        script = ExtResource("1")
        Amount = 77
        """;

    [TestCase("", typeof(Godoodads.Scatter.Legacy.ScatterSettings))]
    [TestCase("uid=\"uid://ch36n85vf88pf\" ", typeof(ScatterSettings))]
    [RequireGodotRuntime]
    public void Settings_saved_at_the_old_path_load(string uid, System.Type expected)
    {
        const string path = "user://scatter_old_path.tres";
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
            file.StoreString(string.Format(OldPathFile, uid));

        var settings = ResourceLoader.Load<ScatterSettings>(path, null, ResourceLoader.CacheMode.Ignore);
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
        AssertThat(settings).IsNotNull();
        AssertThat(settings.GetType()).IsEqual(expected);
        AssertThat(settings.Amount).IsEqual(77);
    }
}
