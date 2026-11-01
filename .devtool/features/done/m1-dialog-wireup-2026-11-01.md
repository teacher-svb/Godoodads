---
id: "m1-dialog-wireup-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:43:13.000Z"
completedAt: "2026-11-01T00:43:13.000Z"
labels: ["editor", "ui"]
order: "aJ"
---
# M1: Wire dialog to pipeline, status and errors

Populate runs the pipeline and shows a status line (`Placed N / M … Seed S.`). Errors and warnings stay in the dialog and also go to the toaster.
Up-front checks: no surface, MultiMesh has no mesh.

## Result (needs a manual check)
Populate validates the target and surface, builds `SurfaceData`, gathers fixed points (existing instances in Append mode, plus siblings if Avoid Siblings is on, excluding the surface node itself), runs the pipeline, writes and commits the undo action. Status line: `Placed N / M (k by spacing). Seed S. T ms.`, with hints when short. Errors stay in the dialog and also go to the toaster.
Engine test `SceneTests.Demo_scene_terrain_end_to_end` covers the non-UI path on the demo scene, and `Csg_root_shapes_are_baked_and_children_rejected` covers CSG.

## Board check (2026-11-01)
Confirmed: the dialog opens after a reload and Populate fills the terrain.
