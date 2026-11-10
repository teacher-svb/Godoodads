---
id: "wfc-1-3-path-router-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:52:17.000Z"
completedAt: "2026-11-10T13:52:17.000Z"
labels: ["core"]
order: "e2"
---
# WFC 1.3: Guided paths: router and solver restrictions

Core `PathRouter`: path faces from the path socket, endpoint groups, free faces, a network joined nearest first, seeded A* over the face transitions the tile set offers (including vertical ones), and Path Wander. Its result becomes per-cell restrictions in `SolveOptions` (route cells: exactly their path faces; neighbours: no path face towards the route).

Brief: §13.3.

## Done when
- Core tests: two ends give one valid connected path; three give a branch through a T; climbing only when stairs variants exist; wander 0 is a shortest route; same seed, same route; every error case from §13.3 with coordinates; no path socket gives the 1.2 result.
- Solver tests: every route cell has exactly its path faces, and no other cell shows a path face towards the route.

## Result
Core `PathRouter` and `CellRestrictions` in `SolveOptions` (the solver narrows limited cells in Reset). Path faces by socket name, groups of placed path cells, open faces (faces out of the bounds count as connected), groups joined nearest first, then every remaining open face routed to another one or into a route (junction). A* over (cell, entry face) states with a box estimate and deepest-first ties; a step needs a variant with that pair of path faces in that layer, so climbing comes from the tile set. Off the route no path variants (brief §13.3 updated). 12 Core tests, including exact path faces per free cell after solving. Timing at 100x16x100 with three ends and climbing: routing 38 ms at wander 0, 0.25-0.45 s at 0.3-1; solving 75 ms. Note for the docs: a straight needs Quarter Turns (Half Turn gives no straight along Z).
