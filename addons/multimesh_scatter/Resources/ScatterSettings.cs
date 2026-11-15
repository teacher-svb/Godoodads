using Godot;

namespace Godoodads.Scatter.Legacy;

/// <summary>
/// Keeps files that name this script by its path from before 1.3.0 loading. The addon moved from
/// addons/multimesh_scatter to addons/godooscatter and the class keeps its uid there, so files saved in
/// the editor find it by uid; files saved by scripts outside the editor have no uid and load this
/// subclass instead. Not a global class, so it never shows in Create Resource.
/// </summary>
[Tool]
public partial class ScatterSettings : Godoodads.Scatter.ScatterSettings
{
}
