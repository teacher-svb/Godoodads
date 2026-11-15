---
id: "dgn-m1-room-placement-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b2"
---
# DGN M1: Room placement

Seeded room placement with sizes in range, gap, margin and occupied cells as obstacles, a fixed number of attempts per room, and a shortfall warning.

Brief: §4.3, §4.7.

## Done when
- Unit tests: no overlaps or gap violations over 200 seeds; occupied cells are avoided; the same seed gives the same rooms; a crowded setup reports the shortfall.
