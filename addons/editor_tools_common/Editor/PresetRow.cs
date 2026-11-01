#if TOOLS
using Godot;

namespace Godoodads.Common;

/// <summary>
/// Preset controls for a <see cref="ToolDialog"/> header: a dropdown of the presets shipped in a
/// folder, plus Load… and Save… buttons for .tres files anywhere in the project. The row only
/// loads and picks paths; the owner applies the preset and writes the file.
/// </summary>
[Tool]
public partial class PresetRow : HBoxContainer
{
    /// <summary>A preset was chosen from the dropdown or loaded from a file.</summary>
    [Signal] public delegate void PresetLoadedEventHandler(Resource preset);
    /// <summary>The user picked a path to save the current settings to.</summary>
    [Signal] public delegate void SaveRequestedEventHandler(string path);

    private string _presetDirectory;
    private OptionButton _list;
    private EditorFileDialog _fileDialog;

    public PresetRow() { }

    /// <param name="presetDirectory">res:// folder with the shipped .tres presets.</param>
    public PresetRow(string presetDirectory)
    {
        _presetDirectory = presetDirectory;
        AddChild(new Label { Text = "Preset", SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 0.5f });

        _list = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true, FitToLongestItem = false };
        _list.ItemSelected += OnListSelected;
        AddChild(_list);

        var load = new Button { Text = "Load…", TooltipText = "Load settings from a .tres preset." };
        load.Pressed += OnLoadPressed;
        AddChild(load);
        var save = new Button { Text = "Save…", TooltipText = "Save the current settings as a .tres preset." };
        save.Pressed += OnSavePressed;
        AddChild(save);

        _fileDialog = new EditorFileDialog { Access = EditorFileDialog.AccessEnum.Resources };
        _fileDialog.AddFilter("*.tres", "Preset");
        _fileDialog.FileSelected += OnFileSelected;
        AddChild(_fileDialog);
        Refresh();
    }

    /// <summary>Re-reads the shipped presets, for example when the dialog opens.</summary>
    public void Refresh()
    {
        _list.Clear();
        _list.AddItem("Choose a preset…");
        _list.SetItemDisabled(0, true);
        foreach (var file in DirAccess.GetFilesAt(_presetDirectory))
        {
            if (!file.EndsWith(".tres"))
                continue;
            string path = _presetDirectory.PathJoin(file);
            var preset = ResourceLoader.Load(path);
            _list.AddItem(string.IsNullOrEmpty(preset?.ResourceName) ? file.GetBaseName() : preset.ResourceName);
            _list.SetItemMetadata(_list.ItemCount - 1, path);
        }
        _list.Select(0);
    }

    private void OnListSelected(long index)
    {
        if (index <= 0)
            return;
        Emit(_list.GetItemMetadata((int)index).AsString());
        _list.Select(0); // a dropdown choice is an action, not a state
    }

    private void OnLoadPressed()
    {
        _fileDialog.FileMode = EditorFileDialog.FileModeEnum.OpenFile;
        _fileDialog.Title = "Load Preset";
        _fileDialog.PopupFileDialog();
    }

    private void OnSavePressed()
    {
        _fileDialog.FileMode = EditorFileDialog.FileModeEnum.SaveFile;
        _fileDialog.Title = "Save Preset";
        _fileDialog.PopupFileDialog();
    }

    private void OnFileSelected(string path)
    {
        if (_fileDialog.FileMode == EditorFileDialog.FileModeEnum.SaveFile)
            EmitSignal(SignalName.SaveRequested, path);
        else
            Emit(path);
    }

    private void Emit(string path)
    {
        var preset = ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Ignore);
        if (preset != null)
            EmitSignal(SignalName.PresetLoaded, preset);
    }
}
#endif
