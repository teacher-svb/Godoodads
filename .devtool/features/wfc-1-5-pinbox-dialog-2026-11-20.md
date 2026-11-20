---
id: "wfc-1-5-pinbox-dialog-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T13:00:00.000Z"
completedAt: null
labels: ["v1.5.0", "editor", "ui"]
order: "d4"
---
# godoowave 1.5: Pin box nodes: dialog, handles and status

Remove the Pin Box group (Origin, Size, Place Around Bounds, Pin Generated Cells, Unpin) and the `PinRequested` wiring. Add a Pin Boxes group with **Add Pin Box** (one undo step, owned by the scene). With the GridMap selected and the dialog open, the overlay draws every enabled pin box child and `IBoundsBoxes` gives their handles after the bounds; a drag is one undo step on the node. Generate and Clear collect the enabled direct children and report the cells they kept. `BoundsGizmoPlugin`/`IBoundsBoxes` keep their API for godoodungeon.

Brief: §16.1–§16.3, §16.5.

## Done when
- Engine tests: Add Pin Box placement and undo; box list from children (order, disabled, nested ignored); status counts.
- Manual: several boxes on the hillside demo, drag them with the GridMap selected, Generate and Clear Bounds keep their cells and leave their empty cells empty, delete a box and regenerate.
