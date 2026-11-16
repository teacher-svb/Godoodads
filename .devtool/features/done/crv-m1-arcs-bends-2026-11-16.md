---
id: "crv-m1-arcs-bends-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T13:03:54.000Z"
completedAt: "2026-11-16T13:03:54.000Z"
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

## Result
- **Arc:** exact arcs from `Arcs.Add`. Angle must be above 0 and at most 360°, with Bend choosing the side. A negative angle first started backwards; it added nothing over Bend, so it is now an error. 360° gives a closed curve. Circle and Semicircle share the code.
- **S-bend:** always two equal opposite arcs, each of angle 2·atan(Offset/Length) and radius Length/(2·sin θ). That works for any offset, so the arc–straight–arc variant is not needed. In a vertical S-bend the wanted up turns with the arcs and ends level.
- **Ellipse:** fitted from (a·sin φ, c·(1 − cos φ)) and closed. The wanted up turns with the tangent's angle (unwrapped), so a vertical ellipse keeps it across the track.

| Case | Points | Deviation |
|---|---|---|
| Arc r 5 90° / r 12 30° Up / r 2 250° Left / r 3 360° | 2 / 2 / 4 / 4 | 0.014 / 0.001 / 0.009 / 0.014 % |
| S-bend 10×3 / 40×1 Left / 4×10 Up | 3 / 3 / 5 | 0.002 / 0.000 / 0.003 % |
| Ellipse 6×3 / 1×9 Down / 20×0.5 Left | 8 | 0.043 / 0.016 / 0.004 % |

Further checks: the S-bend ends exactly Offset to the side and parallel to the start; an ellipse with equal radii matches the circle, closes, and has its wanted up pointing down at the top; impossible settings give messages. 463 Core tests pass.
