---
id: "wfc-m5-animated-solve-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M5 Animation and release"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:27:25.000Z"
completedAt: null
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
