---
id: "dgn-idea-room-stamps-2026-11-15"
status: "backlog"
priority: "medium"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:15:47.000Z"
completedAt: null
labels: ["v2.0.0", "idea"]
order: "c2"
---
# DGN 2.0: Room stamps

Hand-built rooms (a GridMap scene or a cell box in another GridMap) placed by the layout as fixed cells, next to generated rooms, in the style of Edgar's room templates.

## Design notes
- A stamp declares its footprint and door cells.
- The layout places stamps first, then generated rooms.
- Rotation of stamps uses the GridMap orientations (rotations only).

## Decision (2026-11-15, M4 review)
Not in 1.x for now: ship godoodungeon 1.0.0 first and decide after feedback. Stays in `v2.0.0`.
