---
id: "wfc-m5-animated-solve-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M5 Animation and release"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:44:10.000Z"
completedAt: "2026-11-09T22:44:10.000Z"
labels: ["editor"]
order: "aR"
---
# WFC M5: Animated solve

Apply solver events in per-frame batches, including backtracks, with a speed control and an off option. Animation never changes the result.

Brief: §4.7, §6 item 5.

## Done when
- Output is identical with animation on and off.
- The editor stays responsive at target size (manual check).

## From M0
Headless timing could not measure real GridMap mesh rebuilds. Measure frame times here with a real renderer to pick the batch size.

## Result
Solver changes are queued from the thread (`SolveEvent`) and written to the GridMap at Cells Per Frame, default 1,000, with 0 meaning off. Measured with a real renderer, writing 160,000 cells: 250/frame 16.7 ms, 1,000/frame 18.6 ms average (26.5 ms max), 4,000/frame 35.5 ms, 16,000/frame 73.9 ms. The preview remembers the original cells and is restored before the single undoable commit, on Stop, on rebuild, on failure and when the dialog closes. Core test: recorded events replay to the result. Needs the manual check.

## Result
Manual check passed (2026-11-09): at 100×16×100 the cells fill in gradually and the editor stays responsive. Stop removes every preview cell. One undo restores the grid. Cells Per Frame 0 gives the same result at once.
