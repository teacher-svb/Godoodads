---
id: "wfc-1-1-acceptance-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T09:05:46.000Z"
completedAt: "2026-11-10T09:05:46.000Z"
labels: ["testing"]
order: "c7"
---
# WFC 1.1: 1.1 acceptance pass

Run the 1.0 acceptance criteria again (brief §8) plus the 1.1 checks: weighted frequencies, weight 0, regenerate, hand edits kept. Record results here.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8 and §11, checked 2026-11-10:

| Criterion | Result | Evidence |
|---|---|---|
| Determinism | Pass | Core seed tests, `SameSeedGivesTheSameResult`, and replay tests (SolveJob unchanged) |
| Validity, 2D and 3D | Pass | Core neighbour-pair tests; the demo reliability run checks every solve |
| Vertical rules | Pass | Rotation-specific adjacency test |
| Rotation | Pass | Engine basis test |
| Fixed cells | Pass | Core and engine tests, including weight-0 fixed cells |
| Unsatisfiable input | Pass | Core tests |
| Contradiction recovery | Pass | Contradiction-prone set and brute-force comparison |
| Unknown cells | Pass | Engine test |
| User assets | Pass | Engine test: MeshLibrary unchanged |
| Undo | Pass | Manual, during the regenerate check (undo, then Generate) |
| Cancel | Pass | Manual: Stop at 250 cells per frame, 100×16×100 restores the GridMap |
| Reload during a solve | **Pass** | Manual: rebuild during an animated solve, then focus. Not run for 1.0 |
| Performance | Pass | 100×16×100 demo: 1.65 s with ground, 2.34 s without (1.0: 3.87 s and 5.23 s) |
| Reliability | Pass | 20 of 20 seeds for both setups, 0 backtracks |
| Weighted frequencies (§11.1) | Pass | 3:1 within 0.03 over 4,096 cells; rotations share weight; weight equals copies |
| Weight 0 (§11.1) | Pass | Never generated, accepted as fixed, errors for negative and all-zero; status line (manual) |
| Regenerate (§11.2) | Pass | Engine tests, and manual: a second Generate replaces the first |
| Hand edits kept (§11.2) | Pass | Engine tests, and manual with the edited-cells message |
| Record size (§11.2) | Pass | +26 KB scene growth at 100×16×100 (limit 1 MB) |

Totals: 172 Core tests (plus 5 performance tests) and 42 engine tests pass, both builds have 0 warnings, the headless editor load is clean, and the packaged 1.1.0 zip passes the smoke test.
Known: the synthetic Core performance sets are +18% and +14% slower than 1.0 (see `wfc-1-1-demo-weights`). Undoing a first generation leaves an empty settings metadata entry; this behaviour is from 1.0 and harmless. Neither has its own card; the board can decide.
