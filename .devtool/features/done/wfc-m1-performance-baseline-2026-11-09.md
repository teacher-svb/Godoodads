---
id: "wfc-m1-performance-baseline-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:45:45.000Z"
completedAt: "2026-11-09T16:45:45.000Z"
labels: ["core", "performance"]
order: "a8"
---
# WFC M1: Performance baseline

Benchmark 100×100×16 using a synthetic tile set of realistic size. Record time, memory, backtracks and restarts, and fix hot spots.

Brief: §8 Performance.

## Done when
- Timings are recorded here, with a recommendation to confirm or change the 30-second target.

## Result
Baseline at 100×16×100 (160,000 cells), seed 7, on the dev machine, using synthetic random tile sets:

| Tiles | Variants | Result | Time | Backtracks | Restarts |
|---|---|---|---|---|---|
| 26 | 91 | Solved | 0.74 s | 5 | 0 |
| 51 | 197 | Solved | 1.54 s | 6 | 0 |
| 13 | 44 | Out of attempts | 4.81 s | 42,000 | 20 |

Allocation stayed around 250 MB or less. That figure is rough because other tests ran in parallel.

**Recommendation:** keep the 30-second target until the demo tileset gives a real number in M3. It is comfortably met here. The risk is reliability rather than speed: sparse, tightly constrained sets can run out of attempts at this size, because chronological backtracking cannot repair a mistake made long before the failure. If the demo set hits this, the fix is better recovery such as solving in overlapping regions, not more limits.
