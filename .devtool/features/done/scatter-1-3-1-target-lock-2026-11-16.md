---
id: "scatter-1-3-1-target-lock-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godooscatter"
dueDate: null
created: "2026-11-16T09:00:00.000Z"
modified: "2026-11-16T09:22:16.000Z"
completedAt: "2026-11-16T09:22:16.000Z"
labels: ["v1.3.1", "editor", "bug"]
order: "b4"
---
# Scatter 1.3.1: Populate writes to the current selection, not the dialog's target

Review finding 2 (MAJOR). The dialog is non-exclusive, but `_Edit` overwrites `_target` on every selection change and Populate writes to whatever `_target` is then.

- Open the dialog for Grass, select Rocks, Populate: Rocks gets the grass scatter and settings.
- Select the surface MeshInstance3D, Populate: "The target MultiMeshInstance3D is no longer in the scene."

## Done when
- `_Edit` sets `_selected`; `OpenDialog` locks `_target = _selected`, as godoowave does. The validity guard stays on the locked target.
- Manual: both cases above in the editor.

## Result
`_Edit` now sets `_selected`; `OpenDialog` locks `_target = _selected` (godoowave's pattern). `OnPopulatePressed` and `FixedPoints` use the locked `_target`, with the existing `IsInstanceValid`/`IsInsideTree` guard. Build and headless editor load clean; selection changes cannot be driven headless.

## Manual check
1. Open the dialog for one MultiMeshInstance3D, select another in the Scene dock, press Populate: the first one changes, the status still names it.
2. With the dialog open, select the surface MeshInstance3D and press Populate: it populates, no "no longer in the scene" error.
