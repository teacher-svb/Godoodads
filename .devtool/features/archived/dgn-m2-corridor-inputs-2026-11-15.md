---
id: "dgn-m2-corridor-inputs-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:22:29.000Z"
completedAt: "2026-11-15T16:22:29.000Z"
labels: ["v1.0.0", "m2", "core"]
order: "c3"
---
# DGN M2: Corridor solve inputs

Corridor grid over the dungeon footprint and layer range, room and occupied cells `Excluded`, the Wall Clearance ring around each room fixed to the empty (no path faces) variant, a lane of straight variants fixed outside each door through that ring, and the `PathRequest`.

Brief: §4.4.

## Done when
- Unit tests with a synthetic tile set: guided paths joins all doors; no path variant appears off the routes; with Wall Clearance 1 no corridor cell touches a room wall, and with 0 corridors may.

## Results
Branch `godoodungeon-m1`.

- `SolveInputs.Corridors(layout, bounds, corridorHeight, wallClearance, tiles, pathSocket, wander, handPlaced)`. The grid is the layout grid × corridor height, at the bounds origin.
  - The Wall Clearance ring around each room is fixed to the empty variant on every corridor layer.
  - A lane of max(clearance, 1) straight variants is fixed outside each door, on layer 0, along the door's axis.
  - Room footprints (all corridor layers) and hand-placed cells are excluded, applied last, so nothing is fixed on top of them.
  - The input carries a `PathRequest` (socket, wander, origin). A corridor tile set without both straights or an empty tile gives `Error`.
- **Finding, fixed in the door placer.** Guided paths never routes through another open face's entry cell (`PathRouter.Route`: "never pass through it"). Two doors facing each other across the 1-cell channel between two clearance rings shared one entry cell and deadlocked: "No route joins the placed path…". With clearance 0 and gap 2, facing lanes touched and closed into a pair with no open face: "has no open path face". New door rule: **a door's lane and entry cell may not overlap another door's.** The door moves one cell along its wall instead. `DoorPlacer.EntryCell` was added. Brief §4.3 and §6.2 are updated.
- **Measured routing failures** (100 layouts each, single attempt):

  | Settings | Before | After |
  |---|---|---|
  | 40×40, 8 rooms, gap 3 | — | 2 |
  | defaults (48×48, 12 rooms, gap 3) | — | 1 |
  | defaults, gap 4 / gap 5 | — | 0 / 0 |
  | clearance 0, gap 2 | 47 | 1 |

  The rest are 1-cell channels blocked by a waiting entry cell. The run's layout attempts (4, brief §4.5) cover them.

Checks: Core tests **343 passed** (7 new in `DungeonCorridorInputTests`; the test helper retries up to 4 layout attempts like the run):
- 25 seeds: every path face meets a path face (or, outside a door, the door). One network reaches every door's lane, with no separate stretch of corridor.
- Lanes and the ring are fixed, rooms are excluded, and the input sits at the origin.
- Clearance 1: no corridor cell (except the lanes) touches a room's ring, over 25 seeds. Clearance 0: corridors do run along room walls.
- Hand-placed cells are excluded. Doors facing each other across a 2-cell gap with clearance 0 are offset and routed. A corridor tile set without straights is an error.
- The door rule tests also assert that no two doors share a lane or entry cell, over all 450 seeds and variants.
