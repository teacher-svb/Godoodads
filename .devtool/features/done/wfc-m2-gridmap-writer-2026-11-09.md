---
id: "wfc-m2-gridmap-writer-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:39:21.000Z"
completedAt: "2026-11-09T18:39:21.000Z"
labels: ["engine"]
order: "aD"
---
# WFC M2: GridMap writer and undo

Write the result, including Empty tiles as cleared cells, in one `EditorUndoRedoManager` step that restores every cell in the bounds.

Brief: §4.3, §4.7.

## Done when
- An engine test covers writing and restoring cells.
- Undo and redo work in the editor. Move to `review` for this manual check.

## Result
`GridMapCells.Diff` and `Apply` write only free cells that actually change, as flat int records. Generate is one undo step through the plugin. Engine tests pass for writing and restoring. Undo and redo in the editor need the manual check.

## Result
Manual check passed (2026-11-09): undo restores the grid in one step and redo reapplies it. Painted cells stay when generating around them.
