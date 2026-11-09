---
id: "wfc-m2-bounds-gizmo-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:39:21.000Z"
completedAt: "2026-11-09T18:39:21.000Z"
labels: ["editor"]
order: "aG"
---
# WFC M2: Bounds drawn in viewport

Draw the target bounds in the 3D viewport while the dialog is open. The overlay is never saved into the scene.

Brief: §5.

## Done when
- Bounds follow the dialog values and the GridMap transform.
- The saved scene contains no overlay nodes.

## Result
Orange box drawn while the dialog is open, as a MeshInstance3D child with no owner so it is not saved. It follows the bounds and cell size. Needs the manual check.

## Result
Manual check passed (2026-11-09): the box follows the bounds and disappears on close. After saving and reopening the scene, terrain remains and no overlay is left.
