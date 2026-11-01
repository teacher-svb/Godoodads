---
id: "m0-common-helpers-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:12:36.000Z"
completedAt: "2026-11-01T00:12:36.000Z"
labels: ["editor", "common"]
order: "a5"
---
# M0: Node picker row, toast and undo helpers

Small shared helpers:
- **Node picker row**: label plus button that opens `EditorInterface.PopupNodeSelector(callback, validTypes)`.
- **Notify**: wraps `EditorInterface.GetEditorToaster().PushToast` and also writes to the dialog status area.
- **Undo helper**: a thin wrapper over `EditorUndoRedoManager` for "swap property value" actions.

**Files**
- `addons/editor_tools_common/Editor/NodePickerRow.cs`, `Notify.cs`, `UndoHelpers.cs`

**Acceptance**
- Used by the plugin skeleton. The node picker greys out and blocks invalid types.

## Result (needs a manual check)
`NodePickerRow.cs` (wraps `EditorInterface.PopupNodeSelector`), `Notify.cs` and `UndoHelpers.cs`.
**Check in the editor:** the Surface picker only allows MeshInstance3D and CSGShape3D nodes.
