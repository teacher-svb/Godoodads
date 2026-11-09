---
id: "wfc-m3-demo-acceptance-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M3 Demo tileset"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:51:14.000Z"
completedAt: "2026-11-09T18:51:14.000Z"
labels: ["demo", "performance"]
order: "aL"
---
# WFC M3: Reliability and performance on the demo

Run 20 seeds at 100×100×16 and record completion rate and timings.

Brief: §8 Performance, Reliability.

## Done when
- At least 19 of 20 seeds complete and timings meet the target, or the shortfall is reported here for the board.


## From M2
If a full-size solve is slow enough, check in the editor that **Stop** leaves the GridMap unchanged and that rebuilding during a solve neither crashes nor hangs. Then close `wfc-m2-solve-runner`.

## Result
Headless, 100×16×100, 20 seeds each, default limits:

| Setup | Solved | Time (min / avg / max) | Backtracks | Restarts |
|---|---|---|---|---|
| Rock ground layer | 20 of 20 | 3.84 / 3.87 / 3.90 s | 0 | 0 |
| No fixed cells | 20 of 20 | 4.48 / 5.28 / 6.26 s | 0 | 0 |

Meets the 30-second and 19-of-20 targets. Earlier socket designs with unbounded bridges ran out of attempts on 3 of 4 seeds, so reliability depends strongly on keeping requirements local. Stop and rebuild during a solve are checked on `wfc-m2-solve-runner`.
