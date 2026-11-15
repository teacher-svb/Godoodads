---
id: "dgn-m3-bounds-handles-2026-11-15"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor"]
order: "d2"
---
# DGN M3: Bounds handles in the viewport

Bounds box with snapped handles while the dialog is open, reusing the shared `BoundsGizmoPlugin` in `editor_tools_common/Wfc/Editor/`. Drags go through undo.

Brief: §5.

## Done when
- Manual check: dragging updates the dialog and undoes in one step.
