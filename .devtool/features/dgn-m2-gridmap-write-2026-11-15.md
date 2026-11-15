---
id: "dgn-m2-gridmap-write-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "adapters"]
order: "c6"
---
# DGN M2: GridMap read, write, record and undo

Read the hand-placed cells, clear the cells of the previous dungeon record, write the result through the tile set bindings, store the record under its own metadata key, and place markers, all as one undo action.

Brief: §4.6, §4.7.

## Done when
- Engine tests: hand-placed cells are unchanged; regenerate replaces only recorded cells; undo restores the bounds (asserted on cells); the MeshLibrary and tile sets are unchanged.
