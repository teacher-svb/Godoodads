---
id: "dgn-idea-room-roles-2026-11-15"
status: "backlog"
priority: "medium"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-20T10:00:00.000Z"
completedAt: null
labels: ["v2.0.0", "idea"]
order: "c3"
---
# DGN 2.0: Room roles and progression

Start, boss and treasure rooms, each with its own room tile set or markers, and optionally lock and key along the room graph (mission graphs, cyclic generation).

## Design notes
- Roles pick rooms by graph distance from the start room.
- Lock and key needs the door graph to stay meaningful after guided paths joins corridors nearest first, so it may need routes per graph edge instead.

## Decision (2026-11-15, M4 review)
Not in 1.x for now: ship godoodungeon 1.0.0 first and decide after feedback. Stays in `v2.0.0`.

## Note (2026-11-20)
The dungeon graph of godoodungeon 1.1.0 (brief §12) gives the graph distances roles need: `HopDistances`, `Distances`, `Diameter`. Its `Data` per room lets games assign roles themselves in the meantime.
