---
id: "m1-triangle-sampler-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:17:45.000Z"
completedAt: "2026-11-01T00:17:45.000Z"
labels: ["core"]
order: "aC"
---
# M1: Triangle sampler (uniform and area-weighted)

Area-weighted sampling via the alias method (or binary search over cumulative world areas), plus uniform per triangle.
The output for each sample is the triangle index plus barycentrics, so later stages can interpolate normals, UVs and tangents.

**Tests**
- Distribution is proportional to area (chi-squared).
- Points lie inside their triangle.

## Result
`Core/TriangleSampler.cs`: Vose alias method (O(1) per sample), area-weighted or uniform, optional per-triangle weights for later filters. Returns triangle + barycentrics + position.
