---
id: "crv-m2-curve-adapter-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T14:03:15.000Z"
completedAt: "2026-11-16T14:03:15.000Z"
labels: ["v1.0.0", "m2", "adapters", "test"]
order: "c1"
---
# CRV M2: Curve3D writer, tilt and Append

`Adapters/CurveWriter` (brief §4.3, §4.4): turns a `ShapeCurve` in path space into a `Curve3D`, and appends one to an existing curve.

- **Replace:** a new `Curve3D` with the points, handles and closed flag, up vectors enabled.
- **Tilt:** after writing the points, read Godot's untilted up at each point (`sample_baked_up_vector(offset, false)`), set tilt to the signed angle around the tangent to the point's wanted up (projected across the tangent), unwrapped so neighbours differ by less than 180°. Where the wanted up is nearly along the tangent, keep the previous tilt.
- **Append:** read the end frame of the existing curve (last point, tangent from its in-handle reversed or the chord, up from `sample_baked_up_vector(length, true)`); a duplicate of the curve gets the shape's points, the first merged into the last (its in-handle kept). The original curve is never changed. An empty curve, or one without a direction, uses the given frame.
- Closed shapes cannot be appended.

## Done when
- gdUnit tests: points and handles written exactly; closed flag; the M0 probes as tests (loop with Side Shift: exit up +Y within 0.5°; corkscrew faces its axis within 0.5°); a banked turn reaches its bank; Append: join position exact, direction within 0.01°, up carried over; the original `Curve3D` unchanged after Replace and Append.

## Result
`Adapters/CurveWriter.cs`:
- **`Create`:** writes a new `Curve3D` with the points, handles, closed flag and up vectors.
- **`Append`:** works on a duplicate. The join point keeps its in-handle and tilt and takes the shape's out-handle.
  - Closed shapes, and curves that are already closed, give an error.
  - An empty curve starts from the default frame, with a note.
  - A curve with a single point continues along +X from that point, with a note.
- **`TryEndFrame`:** reads the end frame from the reversed in-handle (or the chord) and `sample_baked_up_vector(length, true)`.
- **Tilt (brief §4.4):** at each point, the signed angle around the tangent from Godot's untilted up to the wanted up, both projected across the tangent, unwrapped against the previous tilt. Where the wanted up is nearly along the tangent, the previous tilt is kept. Point offsets come from summed segment lengths rather than `get_closest_offset`, which can pick the wrong pass where the curve crosses itself.

gdUnit tests (`tests/godoocurves/CurveWriterTests.cs`, 8):
- Points, handles and the closed flag are written exactly.
- A loop with a 1 m Side Shift ends within 0.5° of +Y (without tilt it is more than 5° off), and its up faces down at the top.
- Corkscrew: 0.000° from its axis at the points and at most 1.5° between them (Godot interpolates tilt linearly); it ends upright.
- Banked Turn: 30° at the middle within 0.5°, leaning into the turn, upright at both ends.
- Append: join position and in-handle unchanged, direction continuous within 0.01°, the original curve unchanged (positions and handles), and the end up with tilt carried over.
- Closed shapes and closed curves are refused; an empty curve and a single point fall back as described.

122 engine tests pass.
