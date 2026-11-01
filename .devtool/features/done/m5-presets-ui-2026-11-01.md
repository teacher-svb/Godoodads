---
id: "m5-presets-ui-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T01:36:45.000Z"
modified: "2026-11-01T02:10:16.000Z"
completedAt: "2026-11-01T02:10:16.000Z"
labels: ["ui", "editor"]
order: "a0"
---
# M5: Load and save presets in the dialog

Brief §4.7. A **Preset** row in the dialog header:
- a dropdown of the shipped presets (`addons/multimesh_scatter/Presets/*.tres`, labelled by `resource_name`);
- **Load…** and **Save…** buttons with an `EditorFileDialog` for `.tres` anywhere in the project.

Loading copies the preset's values into the current settings but keeps the chosen surface. Saving writes a copy without the surface path. Both are reload-safe (method-name connections, no lambdas).

## Result (needs a manual check)
Shared `Common/PresetRow` (dropdown of `Presets/*.tres` by `resource_name`, Load… and Save… with `EditorFileDialog`, signals `PresetLoaded` and `SaveRequested`). The plugin keeps the surface on load, writes a copy without `SurfacePath` on save (named after the file if unnamed), and refreshes the filesystem and the list. Built and plugin re-enabled through godot-mcp; the log is clean.
**Check:** pick each shipped preset from the dropdown; save a preset to `res://my_preset.tres`; load it back with Load….

## Board check (2026-11-01)
Accepted with the M5 review (v1.0.0).
