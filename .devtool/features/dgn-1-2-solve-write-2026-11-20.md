---
id: "dgn-1-2-solve-write-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T14:00:00.000Z"
modified: "2026-11-20T14:00:00.000Z"
completedAt: null
labels: ["v1.2.0", "core", "adapters"]
order: "h3"
---
# godoodungeon 1.2: Room boxes: solve, record, clear and graph

Generated boxes are solved as rooms with their own footprint and height and recorded; Built boxes are not solved, are excluded from the corridor solve and never enter the record, so Generate and Clear Dungeon keep them. Reading Built door cells from the GridMap (Door item on the wall ring). `DungeonRoom.Box` and `IsBuilt`, Data copied from the box; the Data warning skips unchanged box Data.

Brief: §13.3, §13.4.

## Done when
- Engine tests: acceptance §13.7 items 1, 2 and 4 on a test GridMap; a disabled Built box becomes an obstacle; graph queries still pass.
