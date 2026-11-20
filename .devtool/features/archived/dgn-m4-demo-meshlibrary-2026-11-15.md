---
id: "dgn-m4-demo-meshlibrary-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:15:47.000Z"
completedAt: "2026-11-15T19:15:47.000Z"
labels: ["v1.0.0", "m4", "demo"]
order: "e1"
---
# DGN M4: Script-built dungeon MeshLibrary

A build scene `demo/godoodungeon/tools/build_demo.tscn`, like the WFC demo builder, that makes simple meshes: floor, wall, corner, inner corner, door, pillar and crate props, and corridor straight, corner, T, cross and end. Walls sit inside the cell (§6.1).

Brief: §6.1, §7.

## Done when
- Running the build scene headless recreates the MeshLibrary without errors.

## Results
Branch `godoodungeon-m1`.

- `demo/godoodungeon/tools/build_demo.tscn` (`BuildDungeonDemo.cs`) builds `demo/godoodungeon/assets/dungeon_library.tres` with 18 items made of coloured boxes. Walls sit inside the cell, on its outer edge (§6.1).
  - **Room:** Floor, Floor Cracked, Floor Spawn, Crate, Barrel, Pillar, Wall, Wall Torch (iron sconce), Wall Banner, Corner (with a corner post), Door (archway with a wooden frame).
  - **Corridor:** Corridor (straight), Corridor Torch, Corridor Bend, Corridor T, Corridor Cross.
  - **Obstacles,** in the library but in no tile set, for hand-placing in the obstacles scene: Statue, Pool.
- **Left out on purpose:**
  - *Inner corner*: rooms are rectangles, so it could never be placed.
  - *Corridor end*: the room-to-room router never ends a corridor blind (since `dgn-m3-edge-router`), so it could never be placed either.
- Marker scenes `torch.tscn` (a warm light and a flame by the wall), `corridor_torch.tscn` and `spawn_point.tscn` (a `Marker3D` and a glowing disc) are built by the same scene.
- Running it headless recreates everything without errors: "built 18 items, 11 room tiles, 6 corridor tiles; 0 errors".

## Manual check (board)
Open `dungeon_library.tres` in the inspector, or a GridMap using it, and look over the items.

## Board check passed (2026-11-15)
The tile set preview shows the pieces as intended with no red face labels. Both demo scenes generate good, dungeon-like layouts over several seeds, and the statues and pool stay untouched.
