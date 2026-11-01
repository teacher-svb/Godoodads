#if TOOLS
using Godot;

namespace Godoodads.Common;

/// <summary>Reports a message both in a tool dialog's status area and through the editor toaster.</summary>
public static class Notify
{
    public static void Report(ToolDialog dialog, string message, ToolDialog.Severity severity)
    {
        dialog?.AddStatus(message, severity);
        var toast = severity switch
        {
            ToolDialog.Severity.Error => EditorToaster.Severity.Error,
            ToolDialog.Severity.Warning => EditorToaster.Severity.Warning,
            _ => EditorToaster.Severity.Info,
        };
        EditorInterface.Singleton.GetEditorToaster().PushToast(message, toast);
    }
}
#endif
