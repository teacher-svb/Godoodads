---
id: "m2-normal-source-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T00:55:04.000Z"
modified: "2026-11-01T00:57:39.000Z"
completedAt: "2026-11-01T00:57:39.000Z"
labels: ["core"]
order: "a1"
---
# M2: Normal source (face / smooth)

Brief §4.4, items 1–2 (the normal map is M4). This decides which `N` the orientation uses: the flat face normal, or vertex normals interpolated with the sample's barycentrics. Smooth falls back to the face normal when the surface has no normals; the dialog warns about it.

**Tests (§8)**
- Alignment 0 on a sphere with smooth normals: every up within 1° of the radial direction.
- Face normals on a cube give the face directions.

## Result
`NormalSource` Face/Smooth via `OrientationMath.SurfaceNormal`. Tests: alignment 0 on a 32x64 UV sphere gives every up within 1° of radial (2000 instances); cube face normals; Smooth falls back without vertex normals.
