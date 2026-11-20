---
id: "wfc-1-5-pinbox-facts-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T13:00:00.000Z"
completedAt: null
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
