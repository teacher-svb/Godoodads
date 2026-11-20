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

    /// <summary>
    /// Where Add Pin Box puts a new box (brief §16.1): centred in the bounds, half their size on X
    /// and Z, their full height.
    /// </summary>
    public static (Vector3I Origin, Vector3I Size) CentredIn(Vector3I boundsOrigin, Vector3I boundsSize)
    {
        var size = new Vector3I(Mathf.Max(boundsSize.X / 2, 1), Mathf.Max(boundsSize.Y, 1), Mathf.Max(boundsSize.Z / 2, 1));
        var origin = boundsOrigin + new Vector3I((boundsSize.X - size.X) / 2, 0, (boundsSize.Z - size.Z) / 2);
        return (origin, size);
    }
}
