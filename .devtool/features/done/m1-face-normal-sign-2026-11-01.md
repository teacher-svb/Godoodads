---
id: "m1-face-normal-sign-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:17:45.000Z"
completedAt: "2026-11-01T00:17:45.000Z"
labels: ["core", "test"]
order: "aA"
---
# M1: Verify face normal winding sign

Do not assume the reference's negation. Prove the correct sign with tests on a plane, a cube and a sphere: the face normal must point outward and match the vertex normals.

## Result
Proven by gdUnit4 `AdapterTests.Face_normals_point_outward_on_primitive_meshes` (PlaneMesh, BoxMesh, SphereMesh). Godot front faces wind clockwise, so the outward normal is `(c - a) x (b - a)`; the reference's negation was right. A Core test also covers mirrored transforms (handedness flip).
