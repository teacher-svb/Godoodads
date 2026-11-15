#if TOOLS
using Godot;
using Godot.Collections;

namespace Godoodads.Common.Wfc.Editor;

/// <summary>
/// Adds the socket preview to the top of every WfcTileSet inspector. Each addon that edits tile
/// sets registers its own instance through <see cref="Register"/>; only the first live one in the
/// shared list handles tile sets, so the preview shows once, and the next one takes over when an
/// addon is disabled. The list is metadata on the editor's base control, which survives assembly
/// reloads (brief godoodungeon §10.4).
/// </summary>
[Tool]
public partial class TileSetInspectorPlugin : EditorInspectorPlugin
{
    private const string ListMeta = "godoodads_tileset_inspectors";

    public TileSetInspectorPlugin() { }

    /// <summary>Creates an instance, adds it to the shared list and to <paramref name="plugin"/>.</summary>
    public static TileSetInspectorPlugin Register(EditorPlugin plugin)
    {
        var inspector = new TileSetInspectorPlugin();
        var list = List();
        list.Add(inspector);
        Holder.SetMeta(ListMeta, list);
        plugin.AddInspectorPlugin(inspector);
        return inspector;
    }

    /// <summary>Removes an instance made by <see cref="Register"/> from the shared list and from <paramref name="plugin"/>.</summary>
    public static void Unregister(EditorPlugin plugin, TileSetInspectorPlugin inspector)
    {
        if (inspector == null)
            return;
        var list = List();
        list.Remove(inspector);
        Holder.SetMeta(ListMeta, list);
        plugin.RemoveInspectorPlugin(inspector);
    }

    private static Control Holder => EditorInterface.Singleton.GetBaseControl();

    /// <summary>The registered instances that are still alive, in registration order.</summary>
    private static Array<GodotObject> List()
    {
        var list = new Array<GodotObject>();
        if (Holder.HasMeta(ListMeta))
            foreach (var entry in Holder.GetMeta(ListMeta).AsGodotArray())
                if (entry.AsGodotObject() is { } inspector && IsInstanceValid(inspector))
                    list.Add(inspector);
        return list;
    }

    private bool IsElected()
    {
        var list = List();
        return list.Count > 0 && list[0] == this;
    }

    public override bool _CanHandle(GodotObject @object) => @object is WfcTileSet && IsElected();

    public override void _ParseBegin(GodotObject @object) => AddCustomControl(new TileSetPreview((WfcTileSet)@object));
}
#endif
