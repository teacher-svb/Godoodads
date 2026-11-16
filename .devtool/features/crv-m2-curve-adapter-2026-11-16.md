---
id: "crv-m2-curve-adapter-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:23:07.000Z"
completedAt: null
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
