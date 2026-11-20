---
id: "wfc-1-5-pinbox-node-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T23:55:00.000Z"
completedAt: "2026-11-20T23:55:00.000Z"
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

## Results (2026-11-20)
- Built on the spike of `wfc-1-5-pinbox-facts` (commit c60e488): `GridMapBox` (shared base, `editor_tools_common/Nodes/`), `GridMapBoxGizmoPlugin` (shared, `editor_tools_common/Editor/`), `WfcPinBox` (`godoowave/Nodes/`, pin colour 0.2, 0.75, 1) and `PinBoxGizmoPlugin` (*godoowave pin boxes*). Transform sync kept (facts card).
- New shared API for the cells and dialog cards and godoodungeon room boxes: `GridMapBox.ActiveChildren<T>(map)` (direct children, enabled, every side above 0, child order), `IsActive`, `HasCell`, `CornerOf`, `NearestCell`, `BoxColour`; `GridMapBoxGizmoPlugin.Corners`, `FaceCentre`, and a virtual `Handles(box)` for subclasses.
- `tests/godoowave/WfcPinBoxTests.cs`, 4 tests: Origin/Size/Enabled and position through a saved scene; active boxes (disabled, zero-size and nested ignored, child order), `HasCell`, configuration warning; moving in the tree snaps Origin and resets rotation and scale (cell size 2 × 1.5 × 3); outline corners and handles equal `BoundsGizmoPlugin.Extents`/`HandlePositions`. Engine 137/137, Core 510/510, editor load clean.
- Manual: covered by the board's checks on the facts card (handles with the dialog closed, undo, move tool, rotate/scale, reload, save/reopen), on the same code.
