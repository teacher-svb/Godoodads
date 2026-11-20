---
id: "dgn-m0-prototype-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:26:13.000Z"
completedAt: "2026-11-15T13:26:13.000Z"
labels: ["v1.0.0", "m0", "spike", "demo"]
order: "a3"
---
# DGN M0: Hand-built dungeon prototype with the existing WFC tool

Check brief §10 item 5 without new code. In `demo/godoodungeon/prototype/`, build a minimal room tile set (floor, wall, corner, door, with an `outside` edge socket) and corridor tile set (straight, corner, T, end, empty, with a `path` socket) on one MeshLibrary. In one GridMap, generate three rooms with their own bounds, boundary sockets and a placed door, then place straight pieces outside the doors and generate the corridors over the whole footprint with guided paths.

Brief: §4.2, §4.4, §6.1, §10.

## Done when
- Screenshots and findings are on this card: closed rooms, all doors joined, and any seams or double walls.
- Tile set rules that the brief should require are added to §4.2.

## Results (headless, Godot 4.7)

Built by `demo/godoodungeon/prototype/build_prototype.tscn` (`BuildDungeonPrototype.cs`). It uses only the existing WFC code: `TileSetBinding`, `GridMapCells.ReadFixed`/`Diff`/`Apply`, `SolveJob` with boundary sockets, and `PathRequest`. There is no new addon code.

- MeshLibrary with a room tile set (Floor, Crate, Wall, Corner, Door with weight 0) and a corridor tile set (End, Straight, Corner, T, Cross, Empty). Walls stand inside the cell, on its outer edge.
- One GridMap, 24×24×1. Three rooms (6×5, 7×6, 8×7, walls included) with two doors each. Each door is placed and turned to face out, with a straight corridor piece placed outside it.
- One solve per room (bounds = room, boundary `outside-` on the four side faces), then one corridor solve over the whole footprint with guided paths (`path`, wander 0.3). The corridor record is stored, so the WFC dialog regenerates only the corridors.

Result, seed 1–3 for the rooms and 7 for the corridors (+X right, +Z down; `#` wall, `+` corner, `D` door, `.` floor, `c` crate, corridors `= L T e X`):

```
 +####+
 #....#       +#####+
 #....D=L     #.....#
 #....#LT=====D.....#
 +#D##+=      #.....#
   =L==T      #.....#
   LL  =      +##D##+
       =         =
       T=======T=L
       =       =
       =      LL
       LL     =
        =     =
     +##D###+ =
     #..c...# =
     #......# =
     #......D=L
     #....cc#
     #......#
     +######+
```

- Every solve finished first time: 0 backtracks, 0 restarts, 1 route attempt. 5 routes over 40 cells. The 128 room cells were excluded from the corridor solve automatically.
- A walk from room 1's first door reached all 3 rooms and all 6 doors.

## Findings
1. **The room edge socket must be one-sided.** The first run used a symmetric `outside`. Two outward faces could then meet inside a room, and the interiors filled with back-to-back walls and clusters of corners. With `outside+` on the tiles and `outside-` as the boundary, every room came out clean. Brief §4.2 now requires this.
2. **TwoWay means 0° and 180°.** A straight piece needs `FourWay` (identical rotations merge) to run along Z. This is worth a line in the demo tile set comments and the README tile set rules.
3. **Corridors hug walls and each other.** The corridor next to room 1's east wall and the side-by-side pieces at x 6–7 give a double wall. Possible fix in the layout, if the board wants it: a **wall clearance**, a ring of excluded cells around each room except the door stubs. That keeps corridors one cell away from rooms and needs a gap of at least 3.
4. **Wander 0.3 gives some zigzags** (`LL` pairs). This is a setting to tune, not a problem.

## Manual check for the board
1. Open `demo/godoodungeon/prototype/prototype_dungeon.tscn` and look at the walls, the doors, and the double walls along room 1's east side.
2. Optional: select the GridMap, open **GridMap > Generate WFC (GDDS)**, change the seed and Generate. Only the corridors regenerate, and the rooms stay.
3. Decide: are corridors along room walls acceptable, or should the layout get a wall clearance (finding 3)?

## Board check (2026-11-15)
- Rooms: closed, with corners only at the corners and doors where the corridors meet them. Passed.
- Regenerating the corridors with another seed in the WFC dialog changes only the corridors, and the rooms stay. Passed.
- Double walls: the board asked whether a WFC rule can keep room walls and corridors away from each other (empty tile next to them). Answer and decision below.
- Decision (2026-11-15): **Wall Clearance** in the layout (default 1). The ring around each room is fixed to the corridor tile set's empty tile, with a lane of straight pieces outside each door. A socket rule ("Margin" empty tile next to walls) was considered: guided paths lays routes before the solve and only checks path faces, so it would route next to walls and fail. Brief §4.3, §4.4, §6.1 and §10.5, and cards dgn-m1-layout-model, dgn-m1-layout-validation and dgn-m2-corridor-inputs updated.
- Corridors side by side: v1 accepts them; idea card `dgn-idea-route-spacing`.
