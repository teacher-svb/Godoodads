---
id: "wfc-1-5-pinbox-dialog-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T02:00:00.000Z"
completedAt: "2026-11-20T02:00:00.000Z"
labels: ["v1.5.0", "editor", "ui"]
order: "d4"
---
# godoowave 1.5: Pin box nodes: dialog, handles and status

Remove the Pin Box group (Origin, Size, Place Around Bounds, Pin Generated Cells, Unpin) and the `PinRequested` wiring. Add a Pin Boxes group with **Add Pin Box** (one undo step, owned by the scene). With the GridMap selected and the dialog open, the overlay draws every enabled pin box child and `IBoundsBoxes` gives their handles after the bounds; a drag is one undo step on the node. Generate and Clear collect the enabled direct children and report the cells they kept. `BoundsGizmoPlugin`/`IBoundsBoxes` keep their API for godoodungeon.

Brief: §16.1–§16.3, §16.5.

## Done when
- Engine tests: Add Pin Box placement and undo; box list from children (order, disabled, nested ignored); status counts.
- Manual: several boxes on the hillside demo, drag them with the GridMap selected, Generate and Clear Bounds keep their cells and leave their empty cells empty, delete a box and regenerate.

## Results (2026-11-20)
- The old Pin Box group, `PinRequested` wiring and the pin box outline went in `wfc-1-5-pinbox-cells` (to keep the build green); the status line "Kept N cells in M pin boxes as they were." too, for Generate and Clear Bounds.
- `WfcSettings`: **Pin Boxes** group with **Add Pin Box** (`AddPinBoxRequested`). The plugin adds a `WfcPinBox` named PinBox (PinBox2, … when taken) to the target GridMap, owned by the edited scene, at `WfcPinBox.CentredIn(bounds)` (centred, half X and Z, full height), as one undo step, selects it and says what it did.
- Handles with the GridMap selected and the dialog open: `IBoundsBoxes` box 0 is the bounds, boxes 1–n the enabled pin box children in child order (`GridMapBox.ActiveChildren`). A drag moves the node captured at mouse press and commits one undo step on that node's Origin and Size ("Resize WFC Pin Box"); cancel restores it. The overlay's redraw key includes the pin boxes, so the GridMap's handles follow undo. Outlines come from the pin box gizmo, which draws every pin box.
- `BoundsGizmoPlugin`/`IBoundsBoxes` unchanged (godoodungeon unaffected).
- Tests: `CentredIn` (even, odd and 1-cell bounds); Add Pin Box do/undo pair on a plain `UndoRedo` (added, owned, undo removes, redo restores, saved with the scene). Engine 141/141, Core 511/511, editor load clean.

## Manual check (board)
Build, focus Godot, re-enable godoowave. Hillside demo, select the GridMap, open the dialog:
1. Pin Boxes → Add Pin Box twice: two boxes (PinBox, PinBox2) centred in the bounds, the second selected, one Ctrl+Z each.
2. Select the GridMap again: the bounds and both pin boxes have handles; drag a pin box face, Ctrl+Z undoes only that drag.
3. Generate: the status says "Kept N cells in 2 pin boxes as they were."; cells in the boxes are unchanged, empty cells in them stay empty.
4. Clear Bounds with Keep Placed Cells off and on: pin box cells stay.
5. Delete one box (or untick Enabled), Generate: its cells are replaced.

## Board check (2026-11-20)
Passed: Add Pin Box (placement, selection, undo), pin box handles with the GridMap selected and their undo, Generate keeps box cells and leaves empty ones empty with the status line, Clear Bounds in both modes keeps them, a removed or disabled box frees its cells.
