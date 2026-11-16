---
id: "crv-m2-settings-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T14:06:21.000Z"
completedAt: "2026-11-16T14:06:21.000Z"
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

## Result
`Resources/CurveSettings.cs`:
- **Mode** (Replace, Append) and **Shape** (16 kinds, with readable labels such as "S-bend" and "Banked Turn").
- The 19 shape settings, with inspector ranges and degree hints for Angle and Bank.
- **Placement:** Start, Direction and Up.
- `UsedSettings` lists the settings each shape uses. `_ValidateProperty` hides the rest, hides Placement in Append mode, and limits Bend to Left and Right for Helix and Banked Turn.
- `ToParameters()`, `TryFrame()`, and `Check()`. Check reports the shape's own errors, a closed shape in Append mode, and a bad frame in Replace mode only, since in Append mode the curve's end decides the frame.

Engine tests (7):
- the settings shown per shape and per mode
- every shape's settings exist, and the labels match the kinds
- Bend offers only Left and Right for Helix and Banked Turn
- `ToParameters` copies every setting (checked by reflection, so a forgotten field fails)
- `Check`'s messages
- a save and reload with every changed field, and storing null

127 engine and 495 Core tests pass; the editor loads headless without errors.
