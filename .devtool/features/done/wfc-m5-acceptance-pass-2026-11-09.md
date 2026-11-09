---
id: "wfc-m5-acceptance-pass-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M5 Animation and release"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:44:10.000Z"
completedAt: "2026-11-09T22:44:10.000Z"
labels: ["testing"]
order: "aU"
---
# WFC M5: Full acceptance pass

Go through every item of brief §8, automated and manual, and record the results here.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8, checked 2026-11-09:

| Criterion | Result | Evidence |
|---|---|---|
| Determinism | Pass | Core seed tests; replayed events equal the result; animated and instant runs match (manual) |
| Validity, 2D and 3D | Pass | Core tests on every pair of neighbours |
| Vertical rules | Pass | Rotation-specific adjacency test |
| Rotation | Pass | Engine test against GridMap basis for every orientation |
| Fixed cells | Pass | Core and engine tests, manual M2 |
| Unsatisfiable input | Pass | Core tests report coordinates; no writes (manual M2, M5) |
| Contradiction recovery | Pass | Contradiction-prone set solves; brute-force agreement on 3,000 small sets |
| Unknown cells | Pass | Engine test, counted in the status |
| User assets | Pass | Engine test: MeshLibrary unchanged |
| Undo | Pass | Manual M2 and M5 |
| Cancel | Pass | Manual M3 and M5, including during animation |
| Reload during a solve | **Not run** | Skipped by the board in M3. The same hook passed in M0 |
| Performance | Pass | 100×16×100 demo: 3.87 s with ground, 5.23 s without (target 30 s) |
| Reliability | Pass | 20 of 20 seeds for both demo setups |

Totals: 155 Core tests and 34 engine tests pass, the build has 0 warnings, and the headless editor load is clean.
