#if TOOLS
using Godot;

namespace Godoodads.Common;

/// <summary>
/// Button that calls a tool-button property's Callable on <see cref="Target"/> when pressed.
/// Uses the <c>_Pressed</c> override instead of a signal connection, and keeps only
/// Variant-compatible fields, so it keeps working after an assembly reload.
/// </summary>
[Tool]
public partial class ToolRowButton : Button
{
    public GodotObject Target { get; set; }
    public StringName Property { get; set; }

    public override void _Pressed()
    {
        if (IsInstanceValid(Target) && Property != null)
            Target.Get(Property).AsCallable().Call();
    }
}
#endif
