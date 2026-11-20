---
id: "crv-m3-preview-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:30:17.000Z"
modified: "2026-11-16T14:38:18.000Z"
completedAt: "2026-11-16T14:38:18.000Z"
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

## Result
- **Overlay:** an `ImmediateMesh` child of the target with no owner, drawn without depth test. The shape is pink and the wanted up is shown as yellow ticks at each point, 6 % of the shape's size long.
- **Placement:** the preview uses the same `PlaceShape` as Generate, so in Append mode it starts at the curve's end.
- **Redraw:** only when the preview key changes: every stored setting, plus the curve's instance, point count, closed flag and last point's position, in-handle and tilt. Invalid settings clear the drawing, and the reason appears in the status once, not every frame.
- **Removal:** when the dialog closes, the target leaves the scene, or the plugin exits.

## Manual check
1. With the dialog open, change Shape, sizes, Bend and Start/Direction/Up: the preview follows at once.
2. In Append mode the preview starts at the curve's end. After Generate it shows the next piece from the new end.
3. Invalid settings (for example Direction = Up) clear the preview, and the status says why.
4. Close the dialog: the preview disappears. Save the scene: it is not saved.

## Board check (2026-11-16)
All manual checks passed.
