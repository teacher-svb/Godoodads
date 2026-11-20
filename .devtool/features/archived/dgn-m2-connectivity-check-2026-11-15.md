---
id: "dgn-m2-connectivity-check-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:32:07.000Z"
completedAt: "2026-11-15T16:32:07.000Z"
labels: ["v1.0.0", "m2", "core"]
order: "c5"
---
# DGN M2: Connectivity check

Flood fill over path faces and room interiors after the corridor solve. A failure is reported as an internal error with the unreachable rooms.

Brief: §4.5, §8.

## Done when
- Unit tests: a connected result passes; a result with a corridor removed fails and names the rooms.

## Results
Branch `godoodungeon-m1`.

- `Core/DungeonConnectivity.cs`: `UnreachableRooms(layout, corridorInput, corridorCells, corridorTiles, pathSocket)` walks from room 1.
  - Each room counts as one open space.
  - A door leads to the corridor cell outside it when that cell has a path face towards the door.
  - Corridor cells lead to each other where both have path faces towards each other. All active directions count, so climbing corridors are included.
  - `Describe` gives "Internal error: … room(s) cannot be reached from room 1: room n at (x, y, z), …. Nothing was written. Please report this with the seed and settings."
- `DungeonJob` runs the check after a solved corridor pass. Unreachable rooms give the new status **`InternalError`**, with the message in `Errors` and the rooms in `UnreachableRooms`. The writer (next card) writes nothing for it.

Checks: Core tests **353 passed**, 3 new in `DungeonConnectivityTests`:
- 30 generated dungeons are connected, both in the job result and by a direct check.
- Replacing the corridor piece outside a single-door room with the empty tile names exactly that room, with the full message.
- Clearing every corridor cell except the door lanes leaves every room except room 1 unreachable.
