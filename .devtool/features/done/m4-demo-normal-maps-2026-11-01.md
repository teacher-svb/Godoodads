---
id: "m4-demo-normal-maps-2026-11-01"
status: "done"
priority: "low"
assignee: "claude"
epic: "MultiMesh Scatter"
dueDate: null
created: "2026-11-01T01:22:52.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-01T01:28:03.000Z"
labels: ["v1.0.0", "m4", "demo"]
order: "a3"
---
# M4: Demo normal maps

`demo/multimesh_scatter/assets/`:
- `normal_neutral.png` (128, 128, 255)
- `normal_tilt20_x.png` (constant 20° tilt toward +U)
- `normal_bumps.png` (soft bumps, OpenGL convention)

Imported through godot-mcp.

## Result
Generated with Godot `Image` (256 px RGB8): `normal_neutral.png` (128, 128, 255), `normal_tilt20_x.png` (constant 20° toward +U), `normal_bumps.png` (4×4 sine bumps from a height field, OpenGL Y+). Imported via godot-mcp (0 errors, 0 warnings).
