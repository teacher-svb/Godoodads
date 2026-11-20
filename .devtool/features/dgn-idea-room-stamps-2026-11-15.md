---
id: "dgn-idea-room-stamps-2026-11-15"
status: "backlog"
priority: "medium"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-20T14:10:00.000Z"
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

## Variant: fixed-position rooms (added 2026-11-20)
**Planned as godoodungeon 1.2 room boxes** (brief §13, cards `dgn-1-2-*`), decided 2026-11-20: `DungeonRoomBox` child nodes with Mode Generated or Built, Built doors from placed door tiles, Room Count includes boxes. The notes below were the first sketch; the brief is authoritative. This card keeps the layout-placed stamps for v2.0.0.

The designer places a room at a fixed spot in the GridMap and the dungeon is generated around it, like pinned tiles in godoowave. Today such cells are hand-placed obstacles (brief §4.7): kept, avoided, but never connected ("a hand-built room is not connected to the dungeon", README limitations).
- A fixed room is a `RoomRect` at the designer's position, entered before the random rooms, so the Gap and placement checks apply to the random rooms only.
- Its door cells are declared by hand (or found from door tiles in its cells); the door lane and entry cell rules of brief §4.3 still apply and are a layout error if blocked.
- It is skipped by the room solve (its cells are kept as placed), or solved with all its cells pinned.
- It is a node in the room graph, so `CorridorRouter` joins its doors like any other room's.
- Open: how the designer marks a room as fixed (a box in `DungeonSettings`, a marker node, or a cell box read from the GridMap), and whether its cells belong to the dungeon record (Clear Dungeon keeps them either way).
