---
id: "dgn-m2-connectivity-check-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "core"]
order: "c5"
---
# DGN M2: Connectivity check

Flood fill over path faces and room interiors after the corridor solve. A failure is reported as an internal error with the unreachable rooms.

Brief: §4.5, §8.

## Done when
- Unit tests: a connected result passes; a result with a corridor removed fails and names the rooms.
