---
id: "wfc-m2-gridmap-writer-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["engine"]
order: "aD"
---
# WFC M2: GridMap writer and undo

Write the result, including Empty tiles as cleared cells, in one `EditorUndoRedoManager` step that restores every cell in the bounds.

Brief: §4.3, §4.7.

## Done when
- An engine test covers writing and restoring cells.
- Undo and redo work in the editor. Move to `review` for this manual check.
