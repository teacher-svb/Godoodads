---
id: "crv-m2-settings-2026-11-16"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:23:07.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "resources"]
order: "c4"
---
# CRV M2: CurveSettings

`Resources/CurveSettings` (brief §4.2, §5): the exported settings the dialog shows.

- Mode (Replace, Append) and Shape (all 16).
- The shape's settings, with ranges from brief §4.1. `_ValidateProperty` hides the settings the chosen shape does not use, and hides Start, Direction and Up in Append mode.
- `ToParameters()` returns the Core `ShapeParameters`, `TryFrame()` the `ShapeFrame`, and `Check()` returns errors (a closed shape in Append mode, a bad frame, a bad shape setting).
- Readable enum labels.

## Done when
- Engine tests: the hidden properties per shape and mode; `ToParameters` copies every setting; settings survive a scene save and reload (already tested in M0) with all fields.
