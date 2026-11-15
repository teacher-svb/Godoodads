---
id: "dgn-m1-room-graph-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b3"
---
# DGN M1: Room graph with loops

Minimum spanning tree over room centres (Prim's on the complete graph, ties broken by the seed), plus Extra Connections from the remaining shortest edges.

Brief: §4.3.

## Done when
- Unit tests: the graph is connected; 0 gives a tree; 1 adds every remaining edge; deterministic for a seed.
