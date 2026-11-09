---
id: "wfc-m1-backtracking-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["core"]
order: "a6"
---
# WFC M1: Backtracking and restarts

A trail of domain changes for backtracking, not grid copies. Limits are counted in backtracks and restarts, never in time. Restarts use a seed derived from the original.

Brief: §4.6, §6 item 7.

## Done when
- A tile set that causes contradictions still solves.
- Results stay deterministic with backtracking and restarts.
