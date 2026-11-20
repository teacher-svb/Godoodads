---
id: "dgn-1-1-graph-core-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T14:00:00.000Z"
completedAt: "2026-11-20T14:00:00.000Z"
labels: ["v1.1.0", "core"]
order: "g2"
---
# godoodungeon 1.1: Graph core and per-edge routes

`CorridorRouter` keeps each edge's route cells (lanes included) in `CorridorPlan`, and `DungeonResult` carries them. A Core graph type built from `DungeonLayout` + routes: rooms with footprints and doors, edges with doors, tree/loop flag, route and length. Algorithms on plain arrays: hop distances (BFS), length distances (Dijkstra), shortest path by length (ties: fewer hops, then lower indices), farthest room, diameter, neighbours, corridor between, room at cell, corridors at cell.

Brief: §12.1, §12.3.

## Done when
- Core tests on hand-built graphs with loops and equal lengths: distances, paths, farthest, diameter, fixed tie order.
- Core tests on generated layouts: every footprint cell maps to its room, every route cell to its corridors, junction cells to all of them; one edge per room graph edge, none without route.
- Same seed and settings give the same graph; corridor output (faces) unchanged, existing tests pass.

## Results (2026-11-20)
- `CorridorPlan.Routes`: per edge, the layout cells from the cell outside door A, through lane A, the A* path and lane B, to the cell outside door B. Faces and all existing tests unchanged.
- `Core/DungeonGraphData.cs`: `GraphRoom`, `GraphDoor`, `GraphCorridor` in GridMap cells (rooms and corridors on the bounds' bottom layer), built by `DungeonGraphData.Build(layout, plan, roomHeight)`; `DungeonResult.Graph` set on every solved run. Public constructor takes plain lists, so the resource can rebuild it after loading.
- Queries: `HopDistances`, `Distances`, `ShortestPath`, `ShortestPathCorridors`, `FarthestRoom`, `Diameter`, `Neighbours`, `CorridorBetween`, `RoomAtCell`, `CorridorsAtCell`. Dijkstra keys (length, hops); rooms leave the queue by key then index, and a way is only replaced by a strictly better one, so ties are fixed.
- **Decision:** cell lookups use X and Z only (one floor in 1.1), so a character's cell on any layer finds its room. Brief §12.3 updated.
- `tests/Core.Tests/DungeonGraphTests.cs`: 9 tests (hand-built chain with loops, tie square, split graph, unknown rooms; 6 generated dungeons checked cell by cell, junctions seen; same seed same graph). Core suite 510/510; Godot project builds without warnings.
