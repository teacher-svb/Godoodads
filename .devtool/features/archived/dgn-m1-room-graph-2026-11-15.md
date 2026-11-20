---
id: "dgn-m1-room-graph-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T14:00:10.000Z"
completedAt: "2026-11-15T14:00:10.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "b3"
---
# DGN M1: Room graph with loops

Minimum spanning tree over room centres (Prim's on the complete graph, ties broken by the seed), plus Extra Connections from the remaining shortest edges.

Brief: §4.3.

## Done when
- Unit tests: the graph is connected; 0 gives a tree; 1 adds every remaining edge; deterministic for a seed.

## Results
Branch `godoodungeon-m1`.

- `Core/RoomGraph.cs`: `Build(layout, extraConnections, seed)` fills `layout.Edges`.
  - The weight of every pair of rooms is the squared distance between their centres. A seeded random number breaks ties, so every weight is distinct and the tree is fixed for a seed.
  - Prim's algorithm on the complete graph builds the minimum spanning tree, from room 0. The tree edges come first.
  - Extra Connections: that share of the left-out edges, rounded half away from zero and taken shortest first, added with `IsTreeEdge = false`. 0 gives the tree, 1 every pair.
  - Every edge has A < B, with no duplicates. Fewer than 2 rooms give no edges.
  - Randomness: stream 1 of the layout seed (placement uses stream 0).

Checks: Core tests **284 passed** (7 new in `DungeonRoomGraphTests`):
- Over 200 seeds: n − 1 tree edges, connected, tree edges first, A < B, no duplicates.
- The tree's total length equals Kruskal's over 50 seeds, so it is a minimum spanning tree.
- 0 gives a tree, and 1 gives n(n−1)/2 edges.
- 0.25 gives the rounded share, and every left-out edge is at least as long as the longest extra.
- The same seed gives the same edges.
- Four rooms on a square: the tree never uses a diagonal, and which side is left out varies with the seed (ties are seeded).
- 0 or 1 rooms: no edges.
