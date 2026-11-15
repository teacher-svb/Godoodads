using Godot;

namespace Godoodads.WfcGenerator.Legacy;

/// <summary>
/// Keeps files that name this script by its path from before 1.3.3 loading. The class moved to
/// addons/editor_tools_common/Wfc/Resources and keeps its uid there, so files saved in the editor find it
/// by uid; files saved by scripts outside the editor have no uid and load this subclass instead.
/// See docs/briefs/dungeon_generator.md §4.1. Not a global class, so it never shows in Create Resource.
/// </summary>
[Tool]
public partial class WfcTile : Godoodads.Common.Wfc.WfcTile
{
}
