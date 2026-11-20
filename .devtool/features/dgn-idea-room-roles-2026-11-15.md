---
id: "dgn-idea-room-roles-2026-11-15"
status: "backlog"
priority: "medium"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-20T19:00:00.000Z"
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

## Check against 1.1 and 1.2 (2026-11-20)
- **Covered:** graph distances and per-room `Data` (1.1); designer-tagged roles on fixed rooms, since a room box's Data (such as `{"role": "boss"}`) is copied into the graph on every Generate (1.2; the demo script finds the boss that way). The design note's concern is solved: corridors are routed per graph edge since M3 (`CorridorRouter`), and 1.1 records each edge's route.
- **Left for 2.0:** the generator assigning roles (start, boss, treasure by graph distance), a tile set or markers per role, lock and key, mission graphs and cyclic generation.
- **Caveat for lock and key:** corridors that meet share cells, so a player can bypass a locked corridor at a junction (1.1 README). Lock and key needs corridors that do not merge, or locks on doors instead of corridors.
