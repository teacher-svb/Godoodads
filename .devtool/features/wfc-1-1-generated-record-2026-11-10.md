---
id: "wfc-1-1-generated-record-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-09T23:03:56.000Z"
completedAt: null
labels: ["engine"]
order: "c3"
---
# WFC 1.1: Record what Generate wrote

After a successful solve, store the cells the generation wrote (position, item, orientation, non-empty cells only) on the GridMap as compressed metadata, changed in the same undo action as the cells.

Brief: §11.2.

## Done when
- Engine tests: the record round-trips, undo restores the previous record, and the scene file grows by well under 1 MB for a 100×16×100 generation (report the size).
