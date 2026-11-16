---
id: "crv-m3-readme-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:30:17.000Z"
modified: "2026-11-16T14:33:18.000Z"
completedAt: "2026-11-16T14:33:18.000Z"
labels: ["v1.0.0", "m3", "docs"]
order: "d3"
---
# CRV M3: README usage

Write `addons/godoocurves/README.md` for users: install, usage (select a Path3D, Generate Curve (GDDS), Replace and Append, building a track piece by piece), every shape with its settings, placement, tilt, and the pitfalls (brief §6):
- Generate gives the path its own curve (a shared or saved curve is never changed).
- Use `PathFollow3D` rotation mode **XYZ** with Tilt Enabled to follow banks, loops and corkscrews; Oriented ignores the curve's up and flips in loops.
- Parabolic and Helix leave at their slope; closed shapes cannot be appended.

## Done when
- README covers the above; the root README entry is current.

## Result
`addons/godoocurves/README.md` covers install, usage (Replace, Append, building a track), every shape with its settings and notes, following the tilt with `PathFollow3D` rotation mode XYZ (not Oriented), and the pitfalls (its own curve, local space, unsaved preview). The root README shows godoocurves as in development (M3).
