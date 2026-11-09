---
id: "wfc-m1-propagation-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:45:45.000Z"
completedAt: "2026-11-09T16:45:45.000Z"
labels: ["core"]
order: "a4"
---
# WFC M1: Bitset domains and propagation

Per-cell bitset domains, and constraint propagation that detects contradictions. Includes initial propagation from fixed cells and reporting of conflicting fixed cells.

Brief: §4.4, §4.6.

## Done when
- Unit tests cover propagation on small grids, contradiction detection and conflicting fixed cells.

## Result
Implemented in `WfcSolver`: bitset domains, propagation from every cell at start, and conflicting fixed cell reporting. Tests cover small grids, fixed cells, excluded cells and conflicts. Note that a fixed-cell conflict across a gap reports whichever cell ran out of options, which can be the fixed cell itself.
