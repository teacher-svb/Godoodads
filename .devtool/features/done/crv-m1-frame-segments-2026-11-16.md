---
id: "crv-m1-frame-segments-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T12:54:17.000Z"
completedAt: "2026-11-16T12:54:17.000Z"
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
- Core tests: frame building and its errors, local-to-path mapping, a quarter arc's handles are 0.5523·r, arcs of 30° to 360° stay within 3e-4·r of the circle (a cubic quarter arc's best error is 2.7e-4·r), the fitter meets its tolerance on a sine and a helix with the fewest segments it reports, closed curves and lengths.
- All Core tests pass.

## Result
- `Core/ShapeFrame.cs`: `ShapeFrame` (TryCreate with messages for a zero Direction and a zero or parallel Up; `ToPath`, `ToPathDirection`; Side = forward × up, so heading −Z with +Y up has its right at +X as in Godot) and `Bend` with its local direction.
- `Core/ShapeCurve.cs`: `CurvePoint` (position, in, out, wanted up), `ShapeCurve` (segments, sampling, start/end tangent, length, `ToPath`), `Bezier`, and `CurveBuilder`, which merges a piece that starts where the last ended (keeps the in-handle, takes the new out-handle and up), closes a closed curve onto its first point, and drops the outer handles of an open one.
- `Core/Segments.cs`: `Arcs.Add` (pieces ≤ 90°, handle 4/3·tan(θ/4)·r, any axis, signed angle, linear drift along the axis, wanted up per point) and `Fitter.Add` (Hermite segments from the derivative, the fewest that keep the parametric error within the tolerance: doubling, then a binary search).
- 18 Core tests in `tests/Core.Tests/CurveSegmentTests.cs`, all passing.
