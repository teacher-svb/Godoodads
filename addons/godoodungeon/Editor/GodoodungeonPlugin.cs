#if TOOLS
using Godot;
using Godoodads.Common;
using Godoodads.Common.Wfc.Editor;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon.Editor;

/// <summary>
/// Adds "Generate Dungeon (GDDS)" to the GridMap menu of the 3D viewport toolbar, shared with WFC
/// Generator, and the tile set preview to WfcTileSet inspectors. In M0 the dialog is empty; layout and
/// generation follow in M1 to M3 (brief godoodungeon §7).
/// </summary>
[Tool]
public partial class GodoodungeonPlugin : EditorPlugin
{
    private const string ToolName = "Generate Dungeon (GDDS)";
    private const string ActionText = "Generate";
    private const string WfcGeneratorConfig = "res://addons/wfc_generator/plugin.cfg";

    private SpatialMenuHook _menu;
    private TileSetInspectorPlugin _tileSetInspector;
    private ToolDialog _dialog;
    private GridMap _selected;

    /// <summary>Set when an older WFC Generator is installed next to this addon, else empty.</summary>
    private string _wfcWarning = "";

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "GridMap", "GridMap");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        _tileSetInspector = TileSetInspectorPlugin.Register(this);

        _dialog = new ToolDialog(ToolName, ActionText, "godoodungeon_dialog_size");
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);

        _wfcWarning = WfcGeneratorWarning();
        if (_wfcWarning.Length > 0)
            Notify.Report(null, _wfcWarning, ToolDialog.Severity.Warning);
    }

    public override void _ExitTree()
    {
        _menu?.Remove();
        _menu = null;
        if (_tileSetInspector != null)
        {
            TileSetInspectorPlugin.Unregister(this, _tileSetInspector);
            _tileSetInspector = null;
        }
        if (_dialog != null)
        {
            _dialog.Inspector.Edit(null);
            _dialog.QueueFree();
            _dialog = null;
        }
        _selected = null;
    }

    public override bool _Handles(GodotObject @object) => @object is GridMap;

    public override void _Edit(GodotObject @object) => _selected = @object as GridMap;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    private void OpenDialog()
    {
        if (_selected == null || _dialog == null)
            return;
        _dialog.SetStatus($"Target: {_selected.Name}");
        _dialog.AddStatus("Dungeon generation is not built yet: the layout comes in milestone M1, and generation in M2 and M3.");
        if (_wfcWarning.Length > 0)
            _dialog.AddStatus(_wfcWarning, ToolDialog.Severity.Warning);
        _dialog.SetActionEnabled(false);
        _dialog.PopupForEditing();
    }

    /// <summary>A warning when WFC Generator older than 1.3.3 is installed (brief godoodungeon §4.1), else empty.</summary>
    private static string WfcGeneratorWarning()
    {
        if (!FileAccess.FileExists(WfcGeneratorConfig))
            return "";
        var config = new ConfigFile();
        if (config.Load(WfcGeneratorConfig) != Error.Ok)
            return "";
        string version = config.GetValue("plugin", "version", "").AsString();
        return WfcGeneratorVersion.NeedsUpdate(version)
            ? $"godoodungeon: WFC Generator {version} is installed. Update it to {WfcGeneratorVersion.Minimum} or later. "
              + "Older versions keep their own copy of the tile set classes, so tile sets load as types godoodungeon cannot use."
            : "";
    }
}
#endif
