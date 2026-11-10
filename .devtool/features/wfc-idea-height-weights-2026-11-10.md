---
id: "wfc-idea-height-weights-2026-11-10"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:19:39.000Z"
completedAt: null
labels: ["idea", "core"]
order: "b6"
---
# WFC 1.2: Per-height weights

A weight multiplier per tile as a function of the cell's height in the bounds, for example more rock low down and more ruins high up. Split from `wfc-idea-region-weights`, which keeps region masks for 2.0.

**Size:** S–M. Builds on the 1.1 weights.

## Design notes
- An optional `Curve` (or a few height/multiplier points) on `WfcTile`, sampled at the relative height of the cell.
- Weights then differ per layer. The solver needs a per-layer weight table, for both the choice and the remaining-weight cell order; the byte lookup tables become per layer. Must stay deterministic.
- 1.1 showed that weights also change which cells are filled first, so measure the effect on the demo, not only the frequencies.

## Open questions
- Relative height (0–1 over the bounds) or absolute GridMap Y?
