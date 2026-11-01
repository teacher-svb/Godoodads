---
id: "m0-plugin-skeleton-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:12:36.000Z"
completedAt: "2026-11-01T00:12:36.000Z"
labels: ["editor"]
order: "a6"
---
# M0: multimesh_scatter plugin skeleton

**Files**
- `addons/multimesh_scatter/plugin.cfg`, `Editor/MultiMeshScatterPlugin.cs` (inside `#if TOOLS`)
- `Core/ScatterSettings.cs`: placeholder resource with a few exported groups

**Acceptance**
- Enabling the plugin adds "Scatter Surface…" to the MultiMesh menu. It opens the dialog showing `ScatterSettings` and a surface node picker.
- Disabling the plugin cleans everything up.

## Result (needs a manual check)
The plugin is enabled in `project.godot`. `ScatterSettings` lives in `addons/multimesh_scatter/Resources/` (not `Core/`, because a Resource is an engine object and Core must stay engine-free). Populate currently shows "not implemented yet (M1)".
