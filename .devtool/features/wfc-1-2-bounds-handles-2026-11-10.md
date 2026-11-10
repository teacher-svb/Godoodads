---
id: "wfc-1-2-bounds-handles-2026-11-10"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: null
labels: ["editor"]
order: "d4"
---
# WFC 1.2: Bounds handles in the viewport

An `EditorNode3DGizmoPlugin` (or equivalent) for the GridMap while the dialog is open: six face handles on the bounds box, snapped to cells, replacing the current bounds overlay. Drags go through undo and update the dialog's inspector.

Brief: §12.3.

## Done when
- Dragging each face resizes the bounds by whole cells, undo restores them, and the dialog shows the new values (manual check).
- The int16 bounds check still applies.
- Survives an assembly reload (manual check).
