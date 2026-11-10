---
id: "wfc-1-3-socket-editing-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: null
labels: ["editor"]
order: "e0"
---
# WFC 1.3: Socket editing in the preview

Click a face label in the 3D tile set preview to edit the socket in place, with suggestions from the labels used in the tile set (including one-sided partners). Edits go to the tile's own face, also for rotated variants, through undo. Faces no variant can face are drawn in red.

Brief: §13.1.

## Done when
- Engine tests: a rotated-variant edit lands on the right local face; suggestions include partners; unmatched faces are found.
- Manual: edit, undo and red labels in the editor, also after an assembly reload.
