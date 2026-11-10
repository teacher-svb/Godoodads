---
id: "wfc-1-2-bounds-handles-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:50:45.000Z"
completedAt: "2026-11-10T09:50:45.000Z"
labels: ["editor"]
order: "d4"
---
# WFC 1.2: Bounds handles in the viewport

An `EditorNode3DGizmoPlugin` (or equivalent) for the GridMap while the dialog is open: six face handles on the bounds box, snapped to cells, replacing the current bounds overlay. Drags go through undo and update the dialog's inspector.

Brief: §12.3.

## Done when
- Dragging each face resizes the bounds by whole cells, undo restores them, and the dialog shows the new values (manual check).
- The int16 bounds check still applies.
- Survives an assembly reload (manual check).

## Result
- `BoundsGizmoPlugin` (EditorNode3DGizmoPlugin) adds six face handles per box on the dialog's GridMap while the dialog is open. Handle id = box × 6 + direction. A drag projects the mouse ray onto the face's axis and snaps to the nearest cell boundary, keeping at least one cell. The opposite face stays put.
- The overlay still draws the outlines (orange bounds, blue pin box, red failures), so they show even when the GridMap is not selected. Handles need the GridMap selected.
- A drag updates `WfcSettings` live, and on release adds one undo step ("Resize WFC Bounds") through `SetBox` do/undo. Cancelling puts the box back.
- The headless editor load is clean. Waiting on: the manual check of dragging, undo and an assembly reload.

## Result
- First attempt: dragging gizmo handles did not work, because the GridMap editor takes mouse input in the viewport before gizmo handles do.
- Fix: the plugin enables `SetInputEventForwardingAlwaysEnabled()`, which runs its `_Forward3DGuiInput` before the GridMap editor. A left click within 12 px of a handle, while the GridMap is selected and the dialog open, starts a drag. Motion resizes the box with `BoundsGizmoPlugin.DragFace`, release commits one undo step, and right click or Escape cancels. Other clicks pass through, so painting still works. The gizmo only draws the dots.
- Manual check passed (2026-11-10): dragging both boxes snaps by cells and the inspector follows, undo and cancel work, painting still works, and the handles work after an assembly reload.
