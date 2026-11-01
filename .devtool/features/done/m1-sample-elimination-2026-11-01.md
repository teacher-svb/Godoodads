---
id: "m1-sample-elimination-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:17:45.000Z"
completedAt: "2026-11-01T00:17:45.000Z"
labels: ["core"]
order: "aD"
---
# M1: Spacing via sample elimination

Use Yuksel 2015, "Sample Elimination for Generating Poisson Disk Sample Sets", in place of dart throwing.
- Generate about k×Amount candidates (k around 5, configurable).
- Build a spatial hash grid and per-sample weights. Use a max-heap to repeatedly remove the most crowded sample until Amount remain.
- Existing instances (siblings and append mode) are fixed points that contribute weight but are never removed.
- Min distance 0 means no spacing (skip the stage).

**Tests**
- Exactly Amount kept.
- Spacing on a plane is better than random.
- Fixed points are respected.
- Deterministic.

## Result
`Core/SampleElimination.cs`: Yuksel weight function (alpha 8, weight limiting beta 0.65 / gamma 1.5), spatial hash, CSR neighbour lists, indexed max-heap. Min distance is a **hard** limit: conflicting candidates get top heap priority, and the shortfall is reported as `RemovedForSpacing`. Fixed points add weight and conflicts but are never removed. Tests: exact count, better spread than random, hard limit, fixed points, determinism.
