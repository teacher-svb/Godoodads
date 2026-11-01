#if TOOLS
using Godot;

namespace Godoodads.Common;

public static class UndoHelpers
{
    /// <summary>
    /// Commits one undoable action that sets several properties on <paramref name="target"/>.
    /// Old values are captured before the action runs. Metadata can be set through
    /// "metadata/&lt;name&gt;" property paths.
    /// </summary>
    public static void SetProperties(EditorUndoRedoManager undoRedo, string actionName, GodotObject target,
        params (StringName Property, Variant Value)[] changes)
    {
        undoRedo.CreateAction(actionName, UndoRedo.MergeMode.Disable, target);
        foreach (var (property, value) in changes)
        {
            undoRedo.AddDoProperty(target, property, value);
            undoRedo.AddUndoProperty(target, property, target.Get(property));
        }
        undoRedo.CommitAction();
    }
}
#endif
