---
id: "wfc-m0-verify-gridmap-api-2026-11-09"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["spike"]
order: "a1"
---
# WFC M0: Verify GridMap assumptions

Check brief §10 items 1, 2, 3 and 5 on Godot 4.7: no per-item custom data on `MeshLibrary`, orientations are rotations only, four Y rotations map to orientation indices, and the cost of `SetCellItem` at per-frame batch sizes.

## Done when
- Findings are recorded on this card, with the four Y orientation indices and measured write costs.
- The brief is corrected if any assumption is wrong.
