---
id: "wfc-m1-directions-grid-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:42:27.000Z"
completedAt: "2026-11-09T16:42:27.000Z"
labels: ["core"]
order: "a0"
---
# WFC M1: Directions and grid

A grid that works with any set of directions: 4 for 2D, 6 for 3D. Includes neighbour lookup and unconstrained edges (no neighbour outside the bounds).

Brief: §4.5, §4.6.

## Done when
- Unit tests cover neighbours and opposite directions in 2D and 3D, including edges and corners.

## Result
Implemented `Directions` and `GridTopology`. A 2D grid uses the X and Z directions. Rotation order is tested against Godot `Basis`. Unit tests pass.
