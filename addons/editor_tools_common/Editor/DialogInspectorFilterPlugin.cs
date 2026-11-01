#if TOOLS
using Godot;
using Godot.Collections;

namespace Godoodads.Common;

/// <summary>
/// Hides generic Object and Resource rows (script, resource path and name, ...) for the one object
/// a <see cref="ToolDialog"/> is editing, so the dialog only shows the tool's own settings. Other
/// inspectors, such as the Inspector dock, are not affected.
/// </summary>
[Tool]
public partial class DialogInspectorFilterPlugin : EditorInspectorPlugin
{
    private static readonly string[] Hidden =
        { "script", "resource_local_to_scene", "resource_path", "resource_name", "resource_scene_unique_id" };

    /// <summary>The object whose generic rows are hidden; set by the dialog when it edits one.</summary>
    public GodotObject Target { get; set; }

    private Dictionary<StringName, bool> _hidden = new();

    public DialogInspectorFilterPlugin()
    {
        foreach (var name in Hidden)
            _hidden[name] = true;
    }

    public override bool _CanHandle(GodotObject @object) => @object != null && @object == Target;

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType,
        string hintString, PropertyUsageFlags usageFlags, bool wide) => _hidden.ContainsKey(name);
}
#endif
