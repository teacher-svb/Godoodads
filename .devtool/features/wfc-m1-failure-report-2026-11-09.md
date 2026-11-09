---
id: "wfc-m1-failure-report-2026-11-09"
status: "backlog"
priority: "medium"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["core"]
order: "a7"
---
# WFC M1: Failure report and solver events

A failure result with the failing cell, constraining neighbours and sockets, separating "fixed cells make this impossible" from "ran out of attempts". Add a progress and event stream with cell changes for later animation, plus a cancellation check.

Brief: §4.7, §4.8.

## Done when
- Unit tests cover both failure kinds and cancellation.
- Event replay reproduces the final grid, including after backtracks.
