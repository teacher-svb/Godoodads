---
id: "wfc-idea-region-weights-2026-11-09"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 2.0"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "core"]
order: "b0"
---
# WFC 2.0: Per-region and per-height weights

Vary tile weights across the bounds, for example more rock low down or more ruins in one area.

**Size:** M. Depends on per-tile weights.

## Design notes
- A weight multiplier per tile as a function of height (a curve per tile) or of region (a painted mask or a weight texture over X and Z).
- The solver needs a per-cell weight lookup during collapse, and must stay deterministic.

## Open questions
- Height curves first, or region masks first?
- How is a region authored: a texture, a second GridMap layer, or boxes?
