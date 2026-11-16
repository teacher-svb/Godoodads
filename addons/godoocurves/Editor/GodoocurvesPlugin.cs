#if TOOLS
using Godot;
using Godoodads.Common;

namespace Godoodads.Curves.Editor;

/// <summary>
/// godoocurves' editor entry: "Generate Curve (GDDS)" for a selected Path3D opens a dialog on its
/// <see cref="CurveSettings"/>. M0 skeleton: the dialog has no shapes and Generate is disabled.
/// </summary>
[Tool]
public partial class GodoocurvesPlugin : EditorPlugin
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate Curve (GDDS)";

    private SpatialMenuHook _menu;
    private DialogInspectorFilterPlugin _dialogFilter;
    private ToolDialog _dialog;
    /// <summary>The selected Path3D; follows the editor selection.</summary>
    private Path3D _selected;
    /// <summary>The Path3D the open dialog generates on, fixed when it opens. The dialog is not modal.</summary>
    private Path3D _target;
    private CurveSettings _settings = new();

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "Path3D", "Path3D");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        if (OS.IsStdOutVerbose())
            GD.Print($"godoocurves: {(_menu.IsHooked ? "added to a built-in Path3D menu" : "using its own Path3D toolbar button")}.");

        _dialogFilter = new DialogInspectorFilterPlugin();
        AddInspectorPlugin(_dialogFilter);

        _dialog = new ToolDialog(ToolName, "Generate", "godoocurves_dialog_size") { Filter = _dialogFilter };
        _dialog.ActionPressed += OnGeneratePressed;
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);
    }

    public override void _ExitTree()
    {
        _menu?.Remove();
        _menu = null;
        if (_dialogFilter != null)
        {
            _dialogFilter.Target = null;
            RemoveInspectorPlugin(_dialogFilter);
            _dialogFilter = null;
        }
        if (_dialog != null)
        {
            _dialog.Inspector.Edit(null);
            _dialog.QueueFree();
            _dialog = null;
        }
        _selected = null;
        _target = null;
    }

    public override bool _Handles(GodotObject @object) => @object is Path3D;

    public override void _Edit(GodotObject @object) => _selected = @object as Path3D;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    private void OpenDialog()
    {
        if (_selected == null || !IsInstanceValid(_selected))
            return;
        _target = _selected;
        if (CurveSettings.StoredOn(_target) is { } stored)
            _settings = (CurveSettings)stored.Duplicate();
        _dialog.Edit(_settings);
        _dialog.SetActionEnabled(false);
        _dialog.SetStatus($"Target: {_target.Name}");
        _dialog.AddStatus("godoocurves 0.1.0 is a skeleton: shapes come in the next milestones.");
        _dialog.PopupForEditing();
    }

    private void OnGeneratePressed() { }
}
#endif
