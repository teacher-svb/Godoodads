---
id: "dgn-idea-multi-floor-2026-11-15"
status: "backlog"
priority: "medium"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:15:47.000Z"
completedAt: null
labels: ["v2.0.0", "idea"]
order: "c1"
---
# DGN 2.0: Multiple floors

Rooms at different heights in one dungeon, joined by corridors that climb with stair tiles.

## Design notes
- Layout in 3D: a floor index per room, and a room graph that prefers edges on the same floor.
- Guided paths already climbs when the corridor tile set has stair variants (WFC brief §13.3). The corridor layer range grows to cover every floor.
- Rooms on upper floors need support underneath, or a floor slab of their own.

## Decision (2026-11-15, M4 review)
Not in 1.x for now: ship godoodungeon 1.0.0 first and decide after feedback. Stays in `v2.0.0`.
