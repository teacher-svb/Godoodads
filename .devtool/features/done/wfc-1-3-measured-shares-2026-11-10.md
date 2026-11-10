---
id: "wfc-1-3-measured-shares-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:43:33.000Z"
completedAt: "2026-11-10T13:43:33.000Z"
labels: ["editor", "core"]
order: "e1"
---
# WFC 1.3: Measured tile shares

A Measure button in the preview, with a sample size and seed, runs one background solve (`SolveJob`, cancellable, cleared when the tile set changes). Shows weight share / measured share per tile, flagged above 2x, plus thirds for tiles with a height curve.

Brief: §13.2.

## Done when
- Core test: share counting per tile and per third, and the 2x flag.
- Manual: measure the demo set, change a weight while measuring (cancelled), and an unsolvable sample shows the failure.

## Result
Core `TileShares` (weight share per tile group, measured share, per-third share, the 2x flag, grouping of copies) with 9 Core tests. The preview: a "Tile shares" section with sample size, seed and Measure/Stop; a background `SolveJob` polled in _Process; cancelled when the tile set changes, the preview leaves the tree, or the assembly reloads (`ISerializationListener`). Results sorted by measured share, flagged lines in red, thirds for tiles with a height curve, failure text for an unsolvable sample. The demo set solves the 16x8x16 sample in about 10 ms. Core 208, engine 54 pass. Waiting for the manual editor check.

## Result
Manual check passed (2026-11-10): results list with shares and red flags, works after a reload. Stopping and the failure message are covered by engine tests (`WfcPreviewMeasureTests`: shares shown, Stop cancels a 128x64x128 run, an unsolvable sample shows the failure), because the demo set always solves and the large sample finished too fast to stop by hand. Engine 57 pass.
