---
id: "m0-demo-scene-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:01:53.000Z"
completedAt: "2026-11-01T00:01:53.000Z"
labels: ["demo"]
order: "a7"
---
# M0: Demo scene and test assets

**Files**
- `demo/multimesh_scatter/demo.tscn`: terrain-like plane, sphere, cube, a CSG shape, and a `MultiMeshInstance3D` with a small instance mesh
- `demo/multimesh_scatter/assets/`: placeholder for masks and normal maps (filled in M3/M4)

**Acceptance**
- The scene opens in Godot 4.7 without errors.


## Result
`demo/multimesh_scatter/demo.tscn` contains a subdivided terrain plane, a sphere, a rotated cube, a non-uniformly scaled cube, a CSG box with a sphere hole, and a `Scatter` MultiMeshInstance3D (prism "blade" mesh). It loads headless without errors.
