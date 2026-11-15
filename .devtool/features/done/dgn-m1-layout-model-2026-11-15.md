---
id: "dgn-m1-layout-model-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:38:44.000Z"
completedAt: "2026-11-15T13:38:44.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "b1"
---
# DGN M1: Layout model and settings

Core types for the layout: room rectangles, doors (cell, side), the room graph, `LayoutSettings` (room count, size range, height, wall clearance, gap, margin, extra connections, max doors), a 2D occupancy grid, and seed derivation shared with the solver's restart seeds.

Brief: §4.3.

## Done when
- Unit tests cover rectangle overlap with gap, occupancy marking and seed derivation.

## Results
Branch `godoodungeon-m1`.

- `addons/godoodungeon/Core/` (`Godoodads.Dungeon.Core`). The layout grid is the bounds seen from above, with Y as the GridMap's Z.
  - `RoomRect`: position and size with walls. `Overlaps(other, gap)` counts the free cells needed between rooms on either axis. Also `Grow`, `Ring(width)` (for Wall Clearance), `IsOnEdge`, `IsCorner`, `IsInside(bounds, margin)` and `Cells`.
  - `DoorSide` (±X, ±Y) with `Offset`. `Door(room, cell, side)` with `Outside`. `RoomEdge(a, b, isTreeEdge)`. `DungeonLayout` holds the rooms, edges, doors and warnings.
  - `LayoutSettings`: bounds, room count, room size range, room height, Wall Clearance (1), gap (3), margin (1), Extra Connections (0.15), Max Doors Per Room (4), attempts per room (50). `MinimumGap` = 2 × clearance + 1.
  - `OccupancyGrid`: rooms and hand-placed cells. Cells outside the grid count as taken.
  - `LayoutSeeds`: a seed per layout attempt, per room solve and per corridor solve, each from its own stream.
- Shared Core: `WfcRandom` (SplitMix64) is now public, with `Derive(seed, stream)` (the SplitMix64 finaliser over seed and stream). This is additive: the solver's restart streams and WFC results do not change.

Checks: Core tests **270 passed** (11 new in `DungeonLayoutModelTests`: gap counting on both axes and diagonally, edges, corners and rings, margin, occupancy and clipping, distinct and stable seeds across 8 attempts × 200 rooms, default gap against clearance). Engine tests 85 passed. Build: 0 warnings.
