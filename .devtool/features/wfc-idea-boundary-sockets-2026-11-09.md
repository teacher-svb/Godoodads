---
id: "wfc-idea-boundary-sockets-2026-11-09"
status: "backlog"
priority: "low"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-10T09:19:39.000Z"
completedAt: null
labels: ["idea", "core"]
order: "b3"
---
# WFC 1.1: Boundary sockets

Optionally give each of the six bounds faces a socket, such as ground below or air above and around, instead of leaving edges unconstrained.

**Size:** S

## Design notes
- In the solver, constrain edge cells as if a fixed virtual neighbour with that socket sat outside. No grid change is needed.
- Out of v1 by board decision (M3), because the demo managed with sockets and a ground layer. Only pick this up if users ask for it.

## Decision (2026-11-10)
Not in 1.1. Moved to the *WFC 2.0* epic.

## Decision (2026-11-10, 1.2 planning)
Moved to *WFC 1.2*. A bottom socket such as `rock` would replace the painted ground layer the demo needs, and it removes the "edges are unconstrained" limitation.
