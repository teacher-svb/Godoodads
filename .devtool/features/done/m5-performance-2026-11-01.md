---
id: "m5-performance-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T01:36:45.000Z"
modified: "2026-11-01T01:39:22.000Z"
completedAt: "2026-11-01T01:39:22.000Z"
labels: ["test"]
order: "a5"
---
# M5: Performance check (brief §8)

Brief §8: 50,000 instances on a 100,000-triangle mesh in under 3 s without minimum distance and under 6 s with it, on the board's dev machine. Report timings.

The Core benchmark (65 ms / 1.8 s) does not cover surface extraction or the MultiMesh write. Add an engine benchmark of the full path on a real 100k-triangle mesh, run it on this machine, and record the numbers.

## Result
Engine benchmark `tests/multimesh_scatter/PerformanceTests.cs` (full editor path: `SurfaceSource`, pipeline, local transforms, `MultiMeshWriter`) on a 100,352-triangle PlaneMesh, smooth normals, alignment 0.5, tilt 10°. On this machine (the board's dev machine), Debug build, headless:

| Case | Extract | Pipeline | Write | Total | Budget |
|---|---|---|---|---|---|
| No min distance | 25 ms | 90 ms | 28 ms | **143 ms** | 3 s |
| Min distance 0.05 (250k candidates) | 42 ms | 2051 ms | 31 ms | **2.1 s** | 6 s |

Runs with the engine tests and asserts the budgets. Not included: undo registration and the inspector refresh in the editor (both O(1) resource swaps).
