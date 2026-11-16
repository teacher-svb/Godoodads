using Godot;

namespace Godoodads.Curves;

/// <summary>
/// All options of one Generate. Shown in the dialog and stored on the Path3D afterwards, so reopening
/// the dialog continues where it left off (brief godoocurves §4.3). Shapes and placement come in M1–M3.
/// </summary>
[Tool, GlobalClass]
public partial class CurveSettings : Resource
{
    /// <summary>Metadata key under which a Path3D stores the settings of its last Generate.</summary>
    public const string MetaKey = "godoodads_curve_settings";

    /// <summary>The settings stored on a Path3D, or null.</summary>
    public static CurveSettings StoredOn(Path3D path) =>
        path.HasMeta(MetaKey) ? path.GetMeta(MetaKey).As<CurveSettings>() : null;

    /// <summary>Stores settings on a Path3D. Null removes the entry, so undo leaves nothing behind.</summary>
    public static void StoreOn(Path3D path, CurveSettings settings)
    {
        if (settings == null)
            path.RemoveMeta(MetaKey);
        else
            path.SetMeta(MetaKey, settings);
    }
}
