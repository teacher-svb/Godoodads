---
id: "wfc-m2-orientation-adapter-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:57:41.000Z"
completedAt: "2026-11-09T16:57:41.000Z"
labels: ["engine"]
order: "aB"
---
# WFC M2: GridMap orientation mapping

Map Core rotations to GridMap orientation indices, and back for reading existing cells.

Brief: §6 item 4, §8 Rotation.

## Done when
- An engine test compares each Core socket direction with the actual `Basis` for every rotation.

## Result
`GridMapOrientation` maps quarter turns to indices 0, 16, 10 and 22. An engine test compares every Core direction with the actual GridMap basis for each rotation.
