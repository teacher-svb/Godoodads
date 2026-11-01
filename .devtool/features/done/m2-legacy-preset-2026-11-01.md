---
id: "m2-legacy-preset-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T00:55:04.000Z"
modified: "2026-11-01T01:02:54.000Z"
completedAt: "2026-11-01T01:02:54.000Z"
labels: ["resources"]
order: "a4"
---
# M2: "Upward faces (legacy)" preset

Brief §4.5: ship a preset that reproduces the reference behaviour. Face normals, alignment 1, V = +Y, slope 0–45° from +Y, area-weighted.

**Files:** `addons/multimesh_scatter/Presets/upward_faces_legacy.tres`

**Test:** an engine test loads the preset and checks that, on the demo cube, only the top face is populated.

Loading and saving presets from the dialog is M5 (§4.7).

## Result (needs a manual check)
`addons/multimesh_scatter/Presets/upward_faces_legacy.tres`, generated with `ResourceSaver` (Face normals, alignment 1, V = +Y, slope 0–45°, area-weighted; only non-default values are stored). Engine test `PresetTests` checks that on a box only the top face is populated and the instances are upright. Loading presets from the dialog comes in M5; for now it can be opened in the inspector to see its values.

## Board check (2026-11-01)
The behaviour (slope 0–45°, upright) was confirmed through the slope and orientation checks, and the engine test `PresetTests` covers the file itself.
