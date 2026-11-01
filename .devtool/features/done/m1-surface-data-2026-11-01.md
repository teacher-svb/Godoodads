---
id: "m1-surface-data-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:17:45.000Z"
completedAt: "2026-11-01T00:17:45.000Z"
labels: ["core"]
order: "a9"
---
# M1: Surface data extraction

Read every surface of the target mesh through `SurfaceGetArrays`: positions, normals, UV, UV2, tangents and indices (indexed and non-indexed).
CSG: use `CsgShape3D.BakeStaticMesh()`. Transform to world space and store world-space triangle areas.

**Files**
- `Core/SurfaceData.cs`, `Editor/SurfaceSource.cs` (node to mesh)

**Tests**
- Plane, cube, multi-surface mesh and non-uniform scale (areas are correct).

## Result
`Core/SurfaceData.cs` (all surfaces, indexed and non-indexed, world-space positions, normals (inverse-transpose), tangents, UV/UV2, face normals and areas) and `Adapters/SurfaceSource.cs` (MeshInstance3D, root CSG via `BakeStaticMesh`). Tests: `SurfaceDataTests` (Core) and `AdapterTests` (engine).
New folder `Adapters/` holds engine glue that is not editor-only (CONVENTIONS updated).
