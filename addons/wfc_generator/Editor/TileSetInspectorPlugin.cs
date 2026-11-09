#if TOOLS
using Godot;

namespace Godoodads.WfcGenerator.Editor;

/// <summary>Adds the socket preview to the top of every WfcTileSet inspector.</summary>
[Tool]
public partial class TileSetInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object) => @object is WfcTileSet;

    public override void _ParseBegin(GodotObject @object) => AddCustomControl(new TileSetPreview((WfcTileSet)@object));
}
#endif
