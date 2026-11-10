---
id: "wfc-1-3-acceptance-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T14:20:27.000Z"
completedAt: "2026-11-10T14:20:27.000Z"
labels: ["acceptance"]
order: "eC"
---
# WFC 1.3: 1.3 acceptance pass

Run the 1.0 acceptance criteria again (brief §8) plus the 1.3 checks: socket editing, measured shares, paths, performance unchanged without paths.

Brief: §8, §13.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8 and §13, checked 2026-11-10:

| Criterion | Result | Evidence |
|---|---|---|
| Determinism | Pass | Same seed gives identical results to the 1.2.0 tag without paths (10 of 10 solves over two tile sets, compared in a worktree); route seed tests |
| Validity, 2D and 3D | Pass | Core neighbour-pair tests, including every path solve |
| Vertical rules | Pass | Rotation-specific adjacency test; path stairs use `path@0` |
| Rotation | Pass | Engine basis test; socket edits on rotated variants (engine test and manual) |
| Fixed cells | Pass | Core and engine tests; placed and pinned path cells as endpoints |
| Unsatisfiable input | Pass | Path errors with GridMap coordinates and red cells (engine test and manual) |
| Contradiction recovery | Pass | Unchanged solver code; route retries when the cells around a route cannot fit |
| Unknown cells | Pass | Engine test |
| User assets | Pass | Engine test: MeshLibrary unchanged; the base demo tile set and its scenes unchanged |
| Undo | Pass | Manual: socket edits, generation with paths |
| Cancel | Pass | Manual (1.2 code path); measuring stops (engine test) |
| Reload during a solve | Pass | Manual: preview editing and measuring, and paths, after reloads |
| Performance | Pass | 100x16x100 demo: 0.71 s with the rock boundary, 0.83 s without (1.2: 0.69 s and 0.81 s), 20 of 20 seeds; paths demo at the same size with three ends: 1.50 s mean, 20 of 20, first route used every time |
| Socket editing (§13.1) | Pass | Core and engine tests, manual |
| Measured shares (§13.2) | Pass | Core and engine tests, manual |
| Guided paths (§13.3) | Pass | 12 Core router tests, 3 engine path tests, demo test 20 of 20 seeds climbing, manual |
| Package | Pass | wfc_generator-1.3.0.zip, smoke test loaded cleanly |

Core 220, engine 61 pass. No failures.
