---
id: "wfc-1-5-pinbox-facts-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T23:30:00.000Z"
completedAt: "2026-11-20T23:30:00.000Z"
labels: ["v1.5.0", "spike", "editor"]
order: "d1"
---
# godoowave 1.5: Pin box nodes: verify editor facts

Check the facts of brief §16.6 in the editor before building on them, and record the results here and in the brief:
1. Gizmo handles on a selected `WfcPinBox` child of a GridMap can be dragged (the GridMap editor does not take the input).
2. Keeping the transform in sync with Origin (`NotificationTransformChanged` in a `[Tool]` node, snapping, undo of a move) works; otherwise choose the lock fallback.
3. A 1.4 `WfcSettings` with `PinOrigin`/`PinSize` and a record with pinned bits load and generate without errors.
4. The node survives an assembly reload.

Brief: §16.6.

## Done when
- Each fact has a recorded result (headless where possible, manual for 1, 2 and 4), and the brief says which transform option was chosen.

## Spike (2026-11-20)
A first version to check the facts against, which `wfc-1-5-pinbox-node` finishes:
- `addons/editor_tools_common/Nodes/GridMapBox.cs` (`Godoodads.Common`, `[Tool]`, not global): Origin, Size (default 4 × 1 × 4), Enabled; `NotificationLocalTransformChanged` snaps Origin to the nearest cell and resets rotation and scale; setting Origin moves the node to the box's minimum corner; configuration warning without a GridMap parent.
- `addons/editor_tools_common/Editor/GridMapBoxGizmoPlugin.cs`: outline in the box's colour (faded when disabled) and six face handles through `_SetHandle`/`_CommitHandle`, reusing `BoundsGizmoPlugin.DragFace`; one undo action on Origin and Size per drag.
- `addons/godoowave/Nodes/WfcPinBox.cs` (`[Tool, GlobalClass]`, pin colour) and `Editor/PinBoxGizmoPlugin.cs` (*godoowave pin boxes*), registered by the godoowave plugin.

## Results
3. **Pass (headless).** A `WfcSettings` file with properties the class no longer has loads without an error or warning; the remaining values (Seed, BoundsSize) load, and the resource works as GridMap metadata. Removing `PinOrigin`/`PinSize` is safe. Records with pinned bits: covered by the cells card's decode test.
2. **Pass.** Headless: in a running tree, setting the position snaps Origin at once ((10.7, 0.2, 3.1) → cell (5, 0, 2), node at (10, 0, 4)), rotation and scale reset. Board, in the editor: the move tool moves the box cell by cell with Origin following, Ctrl+Z puts it back, rotate and scale snap back. **Transform sync is kept; no lock fallback.**
1. **Pass (board, editor).** With a pin box child of the hillside GridMap selected, its six handles drag, snap to cells and undo one drag at a time, with the dialog open or closed: the GridMap editor does not take the input.
4. **Pass (board, editor).** After a rebuild and assembly reload the outline and handles still work with no errors, and the box keeps Origin, Size and Enabled through a save and reopen.
