---
id: "dgn-m4-demo-scenes-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:15:47.000Z"
completedAt: "2026-11-15T19:15:47.000Z"
labels: ["v1.0.0", "m4", "demo"]
order: "e3"
---
# DGN M4: Demo scenes

`dungeon_free.tscn` (empty GridMap, target size, 20 rooms) and `dungeon_obstacles.tscn` (hand-placed statues and a pool that rooms and corridors avoid).

Brief: §7.

## Done when
- Both scenes generate at least 19 of 20 seeds (measured, recorded on this card).

## Results
Branch `godoodungeon-m1`.

- `build_demo.tscn` also saves:
  - `demo/godoodungeon/dungeon_free.tscn`: an empty GridMap, 64 × 1 × 64, 20 rooms, seed 1.
  - `dungeon_obstacles.tscn`: the same at seed 2, with hand-placed obstacles: 5 statues and a 6 × 4 pool (29 cells).
  - Both have the demo tile sets and the door tile in the stored `DungeonSettings`, a sun and a sky. Open one, select the GridMap, and press **GridMap → Generate Dungeon (GDDS) → Generate**.
- `tests/godoodungeon/DungeonDemoTests.cs` loads each scene, checks its settings (no errors or warnings), reads the hand-placed cells, and generates seeds 1–20 through the adapter path. It asserts at least 19 solved, and that no generated cell lands on a hand-placed one.

Measured (headless, this machine):

| Scene | Solved | Rooms placed (of 20) | Extra layout attempts | Time per dungeon |
|---|---|---|---|---|
| dungeon_free | **20 of 20** | 19.2 on average | 0 | 16 ms |
| dungeon_obstacles | **20 of 20** | 19.1 on average | 0 | 28 ms |

Core tests 360 passed. Engine tests 101 passed.

## Manual check (board)
Open both scenes and Generate with a few seeds (Randomise Seed helps). In `dungeon_obstacles`, the statues and the pool stay untouched, and rooms and corridors keep clear of them.

## Board check passed (2026-11-15)
The tile set preview shows the pieces as intended with no red face labels. Both demo scenes generate good, dungeon-like layouts over several seeds, and the statues and pool stay untouched.
