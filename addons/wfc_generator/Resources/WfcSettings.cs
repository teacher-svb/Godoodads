using Godot;

namespace Godoodads.Wave.Legacy;

/// <summary>
/// Keeps files that name this script by its path from before 1.3.3 loading. The class moved to
/// addons/editor_tools_common/Wfc/Resources and keeps its uid there, so files saved in the editor find it
/// by uid; files saved by scripts outside the editor have no uid and load this subclass instead.
/// See docs/briefs/dungeon_generator.md §4.1. Not a global class, so it never shows in Create Resource.
/// The folder keeps its old name after the rename to godoowave in 1.4.0 and ships in the godoowave zip.
/// </summary>
[Tool]
public partial class WfcSettings : Godoodads.Common.Wfc.WfcSettings
{
}
