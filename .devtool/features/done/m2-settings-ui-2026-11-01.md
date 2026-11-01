---
id: "m2-settings-ui-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T00:55:04.000Z"
modified: "2026-11-01T01:02:54.000Z"
completedAt: "2026-11-01T01:02:54.000Z"
labels: ["ui", "editor"]
order: "a3"
---
# M2: Orientation and slope settings in the dialog

`ScatterSettings` additions:
- **Normals:** normal source.
- **Orientation:** target vector with quick buttons (+Y, -Y, +X, +Z as `[ExportToolButton]`s), alignment and alignment randomness (sliders 0–1), mesh up axis.
- **Slope:** min and max angle, reference (target vector or custom), and a custom direction (hidden unless Custom).

Dialog warning: Smooth selected but the surface has no normals. The status line mentions the slope coverage.

## Result (needs a manual check)
New settings:
- **Surface > Slope Filter:** min and max degrees, reference (Target Direction or Custom), custom direction (hidden unless Custom).
- **Normals:** normal source.
- **Orientation:** target direction, quick buttons +Y/-Y/+X/+Z, alignment (default 1, so behaviour is unchanged), alignment randomness, mesh up axis.

The dialog errors when min > max, warns when Smooth falls back to face normals, and the status line adds "slope filter kept N% of the surface" (with a hint when it is 0%). Built in the editor through godot-mcp.

## Board check (2026-11-01)
Confirmed: the slope filter, normal source and orientation options work. The editor log shows no new warnings or errors during the check (godot-mcp, since seq 4).
