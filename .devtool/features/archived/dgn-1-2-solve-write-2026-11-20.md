---
id: "dgn-1-2-solve-write-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T14:00:00.000Z"
modified: "2026-11-20T15:00:00.000Z"
completedAt: "2026-11-20T15:00:00.000Z"
labels: ["v1.2.0", "core", "adapters"]
order: "h3"
---
# godoodungeon 1.2: Room boxes: solve, record, clear and graph

Generated boxes are solved as rooms with their own footprint and height and recorded; Built boxes are not solved, are excluded from the corridor solve and never enter the record, so Generate and Clear Dungeon keep them. Reading Built door cells from the GridMap (Door item on the wall ring). `DungeonRoom.Box` and `IsBuilt`, Data copied from the box; the Data warning skips unchanged box Data.

Brief: §13.3, §13.4.

## Done when
- Engine tests: acceptance §13.7 items 1, 2 and 4 on a test GridMap; a disabled Built box becomes an obstacle; graph queries still pass.

## Results (2026-11-20)
- `DungeonRequest.RoomBoxes`; `DungeonJob` and `DungeonPreview` pass them to the layout and count them for the 2-room minimum. Built rooms are skipped by the room solves; Generated boxes solve at their own height (`layout.Height(room)`); failure messages name the box.
- Built cells need no new code to stay: they are not in the record, so they are hand-placed cells (§4.7): the corridor router and solve avoid them, Write and Clear Dungeon never touch them, and a disabled Built box is an ordinary obstacle.
- `DungeonGridMap.ReadRoomBoxes(map, boundsOrigin, doorItem)`: enabled direct children in child order → `RoomBoxSpec` (layout rect, layer, height, mode, Built door cells = Door item on the box's bottom layer). The plugin passes them for Generate and Preview Layout.
- Graph: `GraphRoom.Box`/`IsBuilt`, `DungeonRoom.Box` (NodePath, the box's name) and `IsBuilt`, rebuilt after load; box heights. `DungeonGridMap.Graph(result, map)` copies each box's Data (deep copy). `EntriesWithData(graph, map)` leaves out rooms whose Data still equals their box's.
- `tests/godoodungeon/DungeonRoomBoxSolveTests.cs`, 5 tests: 50 seeds regenerating over each other with a Generated and a Built box and 6 rooms (rooms 1 and 2 at the boxes, both Built doors joined, 5 room solves, nothing written into the Built room, its cells unchanged; §13.7 item 1); regenerate and Clear Dungeon keep the Built room and replace the Generated box (item 2); graph Box, IsBuilt, Data after regenerating, Data copy independent, warning skips box Data, Core graph rebuild keeps them (item 4); disabled Built box is an obstacle; reading boxes from the children. Engine 149/149, Core 519/519, editor load clean.
