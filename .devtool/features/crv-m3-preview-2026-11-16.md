---
id: "crv-m3-preview-2026-11-16"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:30:17.000Z"
modified: "2026-11-16T14:30:17.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor", "ui"]
order: "d2"
---
# CRV M3: Preview overlay

While the dialog is open, draw the shape where Generate would put it (brief §5, board decision: in v1).

- An `ImmediateMesh` overlay, a child of the target without an owner (never saved), drawn on top (no depth test) in a colour distinct from Godot's path gizmo.
- Replace: at Start, Direction and Up. Append: at the end of the current curve, from `CurveWriter.TryEndFrame`.
- Small ticks show the wanted up every few metres, so tilt and bank direction can be judged before generating.
- Redrawn only when the settings, the target's curve or the target's transform change. Invalid settings draw nothing, and the status shows why.
- Removed when the dialog closes or the plugin is disabled.

## Done when
- Manual: the preview follows every setting while you edit it; in Append mode it starts at the curve's end; after Generate it shows the next piece; closing the dialog removes it; saving the scene does not save it.
