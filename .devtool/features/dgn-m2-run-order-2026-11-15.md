---
id: "dgn-m2-run-order-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "core"]
order: "c4"
---
# DGN M2: Dungeon run: order, seeds and retries

A `DungeonJob` that runs layout, rooms and corridors on a background thread with derived seeds, room retries and Layout Attempts, honours cancellation, and returns one combined result with the cells, stats and any failure.

Brief: §4.5.

## Done when
- Unit tests: the same seed gives the same cells; a forced room failure retries; cancellation stops within one solve.
