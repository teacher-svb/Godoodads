---
id: "wfc-1-2-boundary-sockets-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:36:12.000Z"
completedAt: "2026-11-10T09:36:12.000Z"
labels: ["core", "editor"]
order: "d2"
---
# WFC 1.2: Boundary sockets

Six optional socket fields on `WfcSettings` (a "Boundary" group). The solver narrows edge cells against a virtual fixed neighbour per face before the first choice. Placed cells that conflict with a boundary are reported with coordinates.

Brief: §12.1.

## Done when
- Core tests: an empty boundary changes nothing (same result as 1.1 for a seed), a bottom socket restricts only the bottom layer, conflicts with placed cells are reported, invalid labels are errors.
- Engine test: the settings round-trip.

## Result
- `SolveOptions.Boundary`: one optional `Socket` per direction. The solver builds a mask per face of the variants whose socket matches, and narrows edge cells against it in `Reset`, before the first choice. A cell left without options makes the solve Impossible, with that cell as the failure.
- Placed cells that cannot face a boundary are reported as a `FixedConflict` whose second cell is the position just outside the grid. The dialog says "The placed cell at … cannot face the −Y boundary: 'v' faces 'rock'" and marks the cell red.
- `WfcSettings` has a Boundary group with six fields, Pos X to Neg Z, empty by default. `ParseBoundary` trims labels and reports invalid ones as errors ("Boundary +Y: …"), and Generate stops on them.
- Tests: 7 Core tests (empty boundary gives an identical result, bottom socket restricts only the bottom layer, conflict reported with the outside position, an impossible boundary, one-sided sockets, 2D ignores vertical) and 1 engine test (round-trip and errors).
- The dialog messages are checked manually in the acceptance pass.
