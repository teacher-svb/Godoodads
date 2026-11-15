---
id: "m1-settings-persistence-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "MultiMesh Scatter"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-01T00:43:13.000Z"
labels: ["v1.0.0", "m1", "editor"]
order: "aI"
---
# M1: Persist settings on the node

Store a `ScatterSettings` copy in the `MultiMeshInstance3D` metadata on populate, and restore it when the dialog opens. Include the surface NodePath.

## Result (needs a manual check)
Populate stores a copy of the settings, including `SurfacePath` relative to the target, in metadata `godoodads_scatter_settings` (part of the same undo action). Opening the dialog restores them and re-selects the surface.
**Check:** populate, save the scene, restart the editor and reopen the dialog. Settings and surface should be restored.

## Board check (2026-11-01)
Confirmed: settings persist.
