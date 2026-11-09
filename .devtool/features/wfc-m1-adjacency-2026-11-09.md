---
id: "wfc-m1-adjacency-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["core"]
order: "a3"
---
# WFC M1: Adjacency table

Build per-variant, per-direction compatibility bitsets from the sockets.

Brief: §4.6.

## Done when
- Unit tests check the table against hand-worked small tile sets, and that it is symmetric: A allows B in a direction exactly when B allows A in the opposite one.
