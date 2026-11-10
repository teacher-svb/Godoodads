---
id: "wfc-idea-bounds-handles-2026-11-10"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:19:39.000Z"
completedAt: null
labels: ["idea", "editor"]
order: "b7"
---
# WFC 1.2: Bounds handles in the viewport

Drag the bounds box in the 3D viewport instead of typing Origin and Size. Regenerate makes trying a sub-area cheap, so editing bounds by hand matters more.

**Size:** M

## Design notes
- An `EditorNode3DGizmoPlugin` for the GridMap while the dialog is open, with six face handles snapped to cells. It replaces the current bounds overlay.
- Handle drags go through undo and update `WfcSettings` in the dialog.
- Keep the int16 bounds check.
