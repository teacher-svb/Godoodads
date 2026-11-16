---
id: "crv-m1-frame-segments-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T12:50:52.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b1"
---
# CRV M1: Frames, points and cubic segments

The Core building blocks every shape uses (brief §4.1, §4.2, §4.4). Pure C# with GodotSharp value types, in `addons/godoocurves/Core/`, namespace `Godoodads.Curves.Core`.

- `ShapeFrame`: start, forward, up, side = forward × up. Built from Start, Direction and Up: Up is made perpendicular to Direction; a zero or parallel Direction/Up is an error with a message. Maps local (forward, up, side) coordinates to path space.
- `Bend` (Left, Right, Up, Down): the in-plane direction a flat shape turns to, as a frame axis.
- `CurvePoint`: position, in-handle, out-handle (relative, as `Curve3D`), and the **wanted up** (§4.4). `ShapeCurve`: points, closed flag, length estimate, sampling.
- Exact circular arcs: pieces of at most 90°, handle length 4/3·tan(θ/4)·r, around any axis, with an optional linear drift along the axis (for Loop's Side Shift).
- Adaptive Hermite fitting of a parametric curve (position and derivative): doubles the segments until it stays within a tolerance of the exact curve. Used by Wave, Spiral, Ellipse and others.

## Done when
- Core tests: frame building and its errors, local-to-path mapping, a quarter arc's handles are 0.5523·r, arcs of 30° to 360° stay within 1e-4·r of the circle, the fitter meets its tolerance on a sine and a helix with the fewest segments it reports, closed curves and lengths.
- All Core tests pass.
