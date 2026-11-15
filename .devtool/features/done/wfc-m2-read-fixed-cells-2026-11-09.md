---
id: "wfc-m2-read-fixed-cells-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC Generator"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-09T16:57:41.000Z"
labels: ["v1.0.0", "m2", "adapters"]
order: "aC"
---
# WFC M2: Read fixed cells from GridMap

Map occupied cells in the bounds to variants. Unknown items and unmatched orientations stay in place, impose no constraint and are counted. Support clear bounds first.

Brief: §4.4.

## Done when
- Engine tests cover known cells, unknown items, unmatched orientations and clearing.

## Result
`GridMapCells.ReadFixed` and `AllFree`. Engine tests cover known cells, unknown items, unmatched orientations and cells outside the bounds.
