---
id: "wfc-1-3-path-router-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: null
labels: ["core"]
order: "e2"
---
# WFC 1.3: Guided paths: router and solver restrictions

Core `PathRouter`: path faces from the path socket, endpoint groups, free faces, a network joined nearest first, seeded A* over the face transitions the tile set offers (including vertical ones), and Path Wander. Its result becomes per-cell restrictions in `SolveOptions` (route cells: exactly their path faces; neighbours: no path face towards the route).

Brief: §13.3.

## Done when
- Core tests: two ends give one valid connected path; three give a branch through a T; climbing only when stairs variants exist; wander 0 is a shortest route; same seed, same route; every error case from §13.3 with coordinates; no path socket gives the 1.2 result.
- Solver tests: every route cell has exactly its path faces, and no other cell shows a path face towards the route.
