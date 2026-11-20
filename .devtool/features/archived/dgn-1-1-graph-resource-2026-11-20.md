---
id: "dgn-1-1-graph-resource-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T16:00:00.000Z"
completedAt: "2026-11-20T16:00:00.000Z"
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

## Results (2026-11-20)
- `Resources/DungeonGraph.cs`, `DungeonRoom.cs`, `DungeonCorridor.cs`: `[Tool, GlobalClass]` resources, one per file as Godot needs. Exported data only (cells as `Array<Vector3I>`, corridor indices as `PackedInt32Array`, `Data` as `Dictionary`); `DungeonGraph.From(DungeonGraphData)` builds them, and `CoreGraph` (C# only) rebuilds the Core graph lazily after a scene load, finding each corridor's door by its cell.
- **Decision:** `StoredOn` / `StoreOn` instead of `Of`, as `DungeonSettings`. GDScript cannot call C# statics, so it uses `get_meta(&"godoodads_dungeon_graph", null)`; member names stay PascalCase in GDScript. `Diameter()` returns a `Vector2I`. Brief §12.2 and §12.3 updated.
- `RoomAtPosition` / `RoomCentre` use the global transform, or the local one when the map is not in a tree.
- `tests/godoodungeon/DungeonGraphResourceTests.cs`: 5 tests. Resource matches Core; scene save and reload keeps rooms, routes, tree flags and `Data`, and the rebuilt graph answers every query and cell lookup like the original; every query through `Call()`/`Get()`; a GDScript reads the graph from GridMap metadata, calls all queries and sets `Data` (checked that a wrong expectation fails); positions on a moved, rotated, scaled GridMap with cell size (2, 3, 1.5) and `CellCenterY` off.
- Engine suite 132/132, Core 510/510; headless editor load clean.
