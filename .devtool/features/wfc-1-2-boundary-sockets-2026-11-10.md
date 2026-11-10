---
id: "wfc-1-2-boundary-sockets-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: null
labels: ["core", "editor"]
order: "d2"
---
# WFC 1.2: Boundary sockets

Six optional socket fields on `WfcSettings` (a "Boundary" group). The solver narrows edge cells against a virtual fixed neighbour per face before the first choice. Placed cells that conflict with a boundary are reported with coordinates.

Brief: §12.1.

## Done when
- Core tests: an empty boundary changes nothing (same result as 1.1 for a seed), a bottom socket restricts only the bottom layer, conflicts with placed cells are reported, invalid labels are errors.
- Engine test: the settings round-trip.
