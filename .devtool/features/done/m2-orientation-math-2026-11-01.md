---
id: "m2-orientation-math-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T00:55:04.000Z"
modified: "2026-11-01T00:57:39.000Z"
completedAt: "2026-11-01T00:57:39.000Z"
labels: ["core"]
order: "a0"
---
# M2: Orientation blend (slerp N to V, randomness, local up axis)

Brief §4.2. For each instance: `b_i = clamp(b + random(-r, r), 0, 1)`, then `up = slerp(N, V, b_i)`. Then apply, in order: yaw around the instance's own up, tilt, scale, position.

- Write a direction slerp that is stable when N and V are (almost) opposite: rotate around a stable perpendicular axis, never NaN.
- **Mesh up axis** option (+X, -X, +Y, -Y, +Z, -Z, default +Y): that local axis of the instanced mesh is aligned to `up`.
- Default alignment is **1**, so existing results and the reference behaviour (always upright) are unchanged. The brief gives no default.

**Files:** `Core/Orientation.cs`, `Core/ScatterPipeline.cs`

**Tests (§8)**
- Alignment 1: up within 1° of V for several V, including -Y.
- Opposite vectors: N = -V at 0.5 gives orthonormal transforms without NaN.
- Every mesh up axis maps onto `up`.
- Randomness spreads the alignment within its range.

## Result
`Core/OrientationMath.cs` (named to avoid clashing with `Godot.Orientation`): `SlerpDirection` rotates around a fixed perpendicular when N and V are opposite; `MeshAxis` enum. The pipeline computes `b_i`, then up = slerp(N, V, b_i), aligns the mesh axis, then yaw, tilt, scale. The alignment jitter is drawn **last**, so yaw, tilt and scale for a seed match M1. Tests in `OrientationTests`: alignment 1 for 4 V values including -Y, opposite vectors at 0.5, all 6 mesh axes, randomness range, slerp endpoints.
