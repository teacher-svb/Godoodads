---
id: "dgn-1-2-room-box-node-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T14:00:00.000Z"
modified: "2026-11-20T16:30:00.000Z"
completedAt: "2026-11-20T16:30:00.000Z"
labels: ["v1.2.0", "resources", "editor"]
order: "h1"
---
# godoodungeon 1.2: Room boxes: DungeonRoomBox node

`DungeonRoomBox` (`[Tool, GlobalClass]`, `addons/godoodungeon/Nodes/`, outside `#if TOOLS`) on the shared `GridMapBox` base and gizmo from godoowave 1.5 (`wfc-1-5-pinbox-node`, which must be done first). Exports Mode (Generated, Built) and Data; colour per mode.

Brief: §13.1; WFC brief §16.1, §16.5.

## Done when
- Engine tests: Mode, Data, Origin and Size round-trip in a saved scene; only enabled direct children are collected, in child order.
- Manual: select a room box with the dialog closed, drag its handles, undo; reload the assembly and repeat.

## Board check (2026-11-20)
Passed in the editor: Add Room Box (placement, selection, undo); box handles with the box selected (dialog open or closed) and with the GridMap selected, with undo; preview colour, number and redraw; Generate keeps the box as room 1; a Built box keeps its cells and gets a corridor to its door with the status count; a Built box without a door gives an error naming it and turns red; Clear Dungeon keeps Built cells; handles work after an assembly reload.
