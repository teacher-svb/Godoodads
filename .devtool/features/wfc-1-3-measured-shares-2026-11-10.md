---
id: "wfc-1-3-measured-shares-2026-11-10"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: null
labels: ["editor", "core"]
order: "e1"
---
# WFC 1.3: Measured tile shares

A Measure button in the preview, with a sample size and seed, runs one background solve (`SolveJob`, cancellable, cleared when the tile set changes). Shows weight share / measured share per tile, flagged above 2x, plus thirds for tiles with a height curve.

Brief: §13.2.

## Done when
- Core test: share counting per tile and per third, and the 2x flag.
- Manual: measure the demo set, change a weight while measuring (cancelled), and an unsolvable sample shows the failure.
