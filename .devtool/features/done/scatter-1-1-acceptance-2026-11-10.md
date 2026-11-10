---
id: "scatter-1-1-acceptance-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T18:20:00.000Z"
completedAt: "2026-11-10T18:20:00.000Z"
labels: ["acceptance"]
order: "g6"
---
# Scatter 1.1: 1.1 acceptance pass

Run the 1.0 acceptance criteria again (brief §8) plus the GridMap and projection checks; same result as 1.0.1 for mesh surfaces.

Brief: §8, §10.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8 and §10, checked 2026-11-10:

| Criterion | Result | Evidence |
|---|---|---|
| Same result for mesh surfaces | Pass | Bit-exact instance hashes equal to the 1.0.1 tag for 8 runs (two seeds, with and without min distance, identity and a rotated, mirrored, scaled transform), compared in a worktree |
| 1.0 criteria (§8) | Pass | Unchanged pipeline stages; all Core and engine tests pass, including the 1.0 suites and the shipped presets |
| GridMap placement | Pass | Positions match `GridMap.GetMeshes()` within 2e-6 for rotated, scaled, uncentred cells and a moved GridMap |
| Covered faces | Pass | Engine tests (shared faces, post on block); demo test: no instance inside a block |
| Item filter and cell box | Pass | Engine tests; manual |
| Projected masks | Pass | Core tests; manual (UV and Top Down on the demo) |
| Dialog | Pass | GridMap group shown only for GridMaps, header included (manual and engine test); status line; undo; after a reload |
| User assets | Pass | MeshLibrary unchanged (engine test) |
| Performance | Pass | 100 x 100 GridMap, 25,175 cells: read 0.21 s (257,588 covered triangles left out, 44,512 kept); 50,000 instances in 0.08 s, or 2.0 s with min distance 0.1 |
| Package | Pass | multimesh_scatter-1.1.0.zip, smoke test loaded cleanly |

Core 231, engine 70 pass, 0 warnings. No failures.
