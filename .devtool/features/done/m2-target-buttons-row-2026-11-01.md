---
id: "m2-target-buttons-row-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T01:04:50.000Z"
modified: "2026-11-01T01:07:08.000Z"
completedAt: "2026-11-01T01:07:08.000Z"
labels: ["ui", "common"]
order: "a7"
---
# Target direction quick buttons in one row

Board review: the +Y/-Y/+X/+Z buttons in the Orientation group stack in a column; a row would be better.

**Cause:** Godot gives every `[ExportToolButton]` its own full-width inspector row.

**Fix:** a shared `ToolButtonRowInspectorPlugin` in `editor_tools_common`. For a given script class it replaces a list of tool-button properties with one row of buttons, placed where the first button would be. The buttons call the same Callables, so the logic stays on the resource. It also applies to the embedded inspector in `ToolDialog`.

## Result (needs a manual check)
`addons/editor_tools_common/Editor/ToolButtonRowInspectorPlugin.cs`, registered by `MultiMeshScatterPlugin` for the 4 target buttons. The buttons connect to the resource's own Callables (reload-safe). Built, and the plugin was re-enabled through godot-mcp; the log is clean. CONVENTIONS updated.
**Check:** the Orientation group shows +Y, -Y, +X and +Z in one row under Target Direction, and each one sets it.

## Board check (2026-11-01)
Confirmed: looks good.
