#if TOOLS
using Godot;
using Godot.Collections;

namespace Godoodads.Common;

/// <summary>
/// Shows groups of <c>[ExportToolButton]</c> properties side by side in one inspector row instead
/// of one full-width row each. The row appears where the group's first button would be, and each
/// button calls that property's Callable.
/// </summary>
[Tool]
public partial class ToolButtonRowInspectorPlugin : EditorInspectorPlugin
{
    private StringName _scriptClass;
    // Each row is a list of tool-button property names, keyed by its first property.
    private Dictionary<StringName, Array<StringName>> _rows = new();
    private Dictionary<StringName, bool> _grouped = new();

    public ToolButtonRowInspectorPlugin() { }

    /// <param name="scriptClass">Global class name of the script, for example "ScatterSettings".</param>
    public ToolButtonRowInspectorPlugin(string scriptClass) => _scriptClass = scriptClass;

    public void AddRow(params string[] properties)
    {
        var row = new Array<StringName>();
        foreach (var p in properties)
        {
            row.Add(p);
            _grouped[p] = true;
        }
        _rows[properties[0]] = row;
    }

    public override bool _CanHandle(GodotObject @object) =>
        @object?.GetScript().As<Script>() is { } script && script.GetGlobalName() == _scriptClass;

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType,
        string hintString, PropertyUsageFlags usageFlags, bool wide)
    {
        if (!_grouped.ContainsKey(name))
            return false;
        if (_rows.TryGetValue(name, out var row))
            AddCustomControl(BuildRow(@object, row));
        return true; // hide the individual full-width buttons
    }

    private static Control BuildRow(GodotObject target, Array<StringName> properties)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        var hints = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var info in target.GetPropertyList())
            hints[info["name"].AsString()] = info["hint_string"].AsString();

        foreach (var property in properties)
        {
            // A tool button's hint string is "Text" or "Text,Icon".
            var parts = (hints.TryGetValue(property, out var hint) && hint.Length > 0 ? hint : property.ToString()).Split(',');
            var button = new ToolRowButton
            {
                Text = parts[0],
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Target = target,
                Property = property,
            };
            if (parts.Length > 1 && parts[1].Length > 0)
                button.Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon(parts[1], "EditorIcons");
            box.AddChild(button);
        }
        return box;
    }
}
#endif
