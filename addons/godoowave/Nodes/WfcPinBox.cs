using Godot;
using Godoodads.Common;

namespace Godoodads.Wave;

/// <summary>
/// A pin box (WFC brief §16): a child of the GridMap whose cells Generate and Clear never change.
/// Occupied cells inside stay and constrain their neighbours; empty cells stay empty. Disable or
/// delete the box to let the next Generate replace its cells. No runtime behaviour.
/// </summary>
[Tool, GlobalClass]
public partial class WfcPinBox : GridMapBox
{
    /// <summary>The pin colour, as the 1.4 pin box outline.</summary>
    public override Color BoxColour => new(0.2f, 0.75f, 1f);
}
