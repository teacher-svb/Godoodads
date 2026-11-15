---
id: "wfc-m1-collapse-loop-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC Generator"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-09T16:45:45.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "a5"
---
# WFC M1: Cell selection and collapse

Main solve loop: pick the cell with the fewest options, breaking ties by seed, collapse to a uniformly random remaining variant, then propagate. Deterministic from the seed.

Brief: §4.6, §8.

## Done when
- Tests confirm determinism and validity (every adjacent pair matches) in 2D and 3D.
- Fixed cells are unchanged after a solve.

## Result
Picks the cell with the fewest options, ties by seed, then a uniformly random remaining variant. Determinism and all-neighbours-match are tested in 2D and 3D.
