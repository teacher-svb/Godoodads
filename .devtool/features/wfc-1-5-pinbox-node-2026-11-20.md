---
id: "wfc-1-5-pinbox-node-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T13:00:00.000Z"
completedAt: null
labels: ["v1.5.0", "resources", "editor"]
order: "d3"
---
# godoowave 1.5: Pin box nodes: WfcPinBox node and its gizmo

Shared base first: `GridMapBox` in `addons/editor_tools_common/Nodes/` (Origin, Size, Enabled, transform sync, configuration warning; not a `[GlobalClass]`) and one gizmo plugin for any `GridMapBox`, colour from the subclass. godoodungeon 1.2 room boxes (`DungeonRoomBox`) derive from it too, so keep its API small and stable.

Then `WfcPinBox` (`[Tool, GlobalClass]` `Node3D`, `addons/godoowave/Nodes/`, outside `#if TOOLS`) with Origin, Size and Enabled; position kept in sync with Origin (or locked, per the facts card); configuration warning when the parent is not a GridMap. A gizmo plugin (*godoowave pin boxes*) draws every pin box outline and, when one is selected, its six face handles, snapped to cells, through `_SetHandle`/`_CommitHandle` with undo.

Brief: §16.1, §16.3, §16.5; dungeon brief §13.1.

## Done when
- Engine tests: Origin/Size/Enabled round-trip in a saved scene; handle positions and face drags match `BoundsGizmoPlugin`; a disabled or zero-size box is ignored.
- Manual: select a pin box with the dialog closed, drag each face, undo, move it with the move tool; rebuild and repeat after the assembly reload.
