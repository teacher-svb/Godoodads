---
id: "wfc-m1-adjacency-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:42:27.000Z"
completedAt: "2026-11-09T16:42:27.000Z"
labels: ["core"]
order: "a3"
---
# WFC M1: Adjacency table

Build per-variant, per-direction compatibility bitsets from the sockets.

Brief: §4.6.

## Done when
- Unit tests check the table against hand-worked small tile sets, and that it is symmetric: A allows B in a direction exactly when B allows A in the opposite one.

## Result
Implemented per-variant, per-direction compatibility bitsets. Hand-worked sets pass, and symmetry is tested on a set needing more than one bitset word.
