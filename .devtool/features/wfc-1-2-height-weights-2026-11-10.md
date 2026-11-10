---
id: "wfc-1-2-height-weights-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: null
labels: ["core", "editor"]
order: "d3"
---
# WFC 1.2: Per-height weights

Optional `HeightCurve` on `WfcTile`. The compiler produces per-layer variant weights; the solver uses the cell's layer for the choice, the remaining-weight order and the generatable set. The preview shows the curve's effect (for example the weight at bottom, middle and top).

Brief: §12.2.

## Done when
- Core tests: frequencies per layer follow the curve, no curve gives the 1.1 result for a seed, a 0 multiplier is never generated at that height but accepted as a fixed cell, negative values and all-zero layers are errors.
- Performance within 10% of the follow-up baseline when no curves are used.
