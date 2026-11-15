---
id: "dgn-m2-room-inputs-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:15:09.000Z"
completedAt: "2026-11-15T16:15:09.000Z"
labels: ["v1.0.0", "m2", "core"]
order: "c2"
---
# DGN M2: Room solve inputs

Per room: grid, `initial` array with door cells fixed to the door variant rotated outward, and `SolveOptions.Boundary` with the room edge socket on the side faces.

Brief: §4.4.

## Done when
- Unit tests with a small synthetic tile set: rooms solve closed for every door side; a door tile with two edge faces is rejected.

## Results
Branch `godoodungeon-m1`.

- `Core/SolveInputs.cs`:
  - `SolveInput(Origin, Grid, Initial, Boundary, Error)`: one WFC solve, with `GridMapCell(cell)`.
  - `SolveInputs.Room(layout, room, roomHeight, tiles, doorTile, edgeSocket)`:
    - The grid is the room footprint × room height, starting at the layout's bottom layer (the bounds origin).
    - The four side faces get the boundary `edge-`. The vertical faces stay open.
    - Every door cell on layer 0 is fixed to the door variant whose `edge+` face points out of its side (layout Y is GridMap Z, through `SolveInputs.Direction`). Other cells are free.
    - An empty edge socket, or a door tile without a fitting rotation, gives `Error` (naming the side and the GridMap cell) instead of an input.
- `DungeonTileRules.DoorVariant` only accepts a variant with exactly one `edge+` face, so a corner (two edge faces) is never a door.

Checks: Core tests **336 passed**, 9 new in `DungeonRoomInputTests`, with the prototype rules in `DungeonTestTiles`. Each test solves with `WfcSolver`:
- Rooms solve closed with a door on each of the 4 sides: every face towards the outside is `outside+`, and none points inwards. The door cell holds the door, facing out.
- A 2-layer room with doors on all sides solves closed, with exactly the 4 placed doors (weight 0).
- 30 seeds with two doors all solve closed.
- The input sits at the room in GridMap cells, with the boundary on the sides only.
- A corner tile as the door is rejected. An empty edge socket gives an error.
