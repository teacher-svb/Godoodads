---
id: "dgn-1-1-graph-resource-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T10:00:00.000Z"
completedAt: null
labels: ["v1.1.0", "resources", "adapters"]
order: "g3"
---
# godoodungeon 1.1: DungeonGraph resources and GDScript API

`DungeonGraph`, `DungeonRoom`, `DungeonCorridor` as `[GlobalClass]` `[Tool]` resources in `Resources/`, built from the Core graph. `Data` dictionary per room and corridor, `DungeonRoom.Number`. Queries of §12.3 with Variant-compatible types only; `RoomAtPosition` / `RoomCentre` through the GridMap transform. `DungeonGraph.Of(GridMap)`.

Brief: §12.2, §12.3.

## Done when
- Engine tests: every query called through `Call()` (as GDScript does) matches the C# result; a GDScript test script calls them too.
- Save a scene with the graph in GridMap metadata, reload: same rooms, corridors, routes and `Data`.
- `RoomAtPosition` correct for a moved, rotated and scaled GridMap with a non-default cell size.
