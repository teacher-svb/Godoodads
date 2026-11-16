---
id: "crv-m1-arcs-bends-2026-11-16"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T12:50:52.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b3"
---
# CRV M1: Arc, S-bend and Ellipse

The arcs-and-bends group of brief §4.1.

- Arc: Radius, Angle (any, up to 360°), Bend; exact. Circle and Semicircle become its presets.
- S-bend: Length, Offset, Bend: two opposite arcs (or arc–straight–arc when the offset is small for the length), ends parallel to the start.
- Ellipse: Radius Forward, Radius Side, Bend; closed, starting along Direction; fitted within tolerance.

## Done when
- Core tests: accuracy within 0.1 %, start tangent along Direction, S-bend's end parallel to the start and offset by exactly Offset, Ellipse closes and equals a circle when both radii match.
