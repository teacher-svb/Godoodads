#if TOOLS
using Godot;
using Godot.Collections;

namespace Godoodads.Common;

/// <summary>
/// Label plus button that opens the editor's own scene-tree node selector, filtered to
/// <paramref name="validTypes"/> (engine class names, for example "MeshInstance3D").
/// </summary>
[Tool]
public partial class NodePickerRow : HBoxContainer
{
    [Signal] public delegate void NodePickedEventHandler(Node node);

    public Node Value { get; private set; }

    private Button _button;
    private Array<StringName> _validTypes;

    public NodePickerRow() { }

    public NodePickerRow(string label, params string[] validTypes)
    {
        _validTypes = new Array<StringName>();
        foreach (var type in validTypes)
            _validTypes.Add(type);

        AddChild(new Label { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 0.5f });
        _button = new Button { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true };
        _button.Pressed += OnPressed;
        AddChild(_button);
        SetValue(null);
    }

    public void SetValue(Node node)
    {
        Value = node;
        _button.Text = node != null ? node.Name : "Pick a node…";
        _button.Icon = node != null ? EditorInterface.Singleton.GetEditorTheme().GetIcon(node.GetClass(), "EditorIcons") : null;
        _button.TooltipText = node != null ? EditorInterface.Singleton.GetEditedSceneRoot()?.GetPathTo(node) : "";
    }

    private void OnPressed()
    {
        EditorInterface.Singleton.PopupNodeSelector(Callable.From<NodePath>(OnSelected), _validTypes, Value);
    }

    private void OnSelected(NodePath path)
    {
        // An empty path means the selector was cancelled.
        if (path.IsEmpty)
            return;
        var node = EditorInterface.Singleton.GetEditedSceneRoot()?.GetNodeOrNull(path);
        if (node == null)
            return;
        SetValue(node);
        EmitSignal(SignalName.NodePicked, node);
    }
}
#endif
