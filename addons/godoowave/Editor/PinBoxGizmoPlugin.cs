#if TOOLS
using Godot;
using Godoodads.Common;
using Godoodads.Common.Editor;

namespace Godoodads.Wave.Editor;

/// <summary>The *godoowave pin boxes* entry of the viewport's gizmo menu: outlines and face handles of <see cref="WfcPinBox"/> nodes.</summary>
[Tool]
public partial class PinBoxGizmoPlugin : GridMapBoxGizmoPlugin
{
    public override string _GetGizmoName() => "godoowave pin boxes";

    protected override bool Handles(GridMapBox box) => box is WfcPinBox;
}
#endif
