---
id: "dgn-m1-layout-model-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:26:13.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b1"
---
# DGN M1: Layout model and settings

Core types for the layout: room rectangles, doors (cell, side), the room graph, `LayoutSettings` (room count, size range, height, wall clearance, gap, margin, extra connections, max doors), a 2D occupancy grid, and seed derivation shared with the solver's restart seeds.

Brief: §4.3.

## Done when
- Unit tests cover rectangle overlap with gap, occupancy marking and seed derivation.
