---
id: "wfc-idea-measured-shares-2026-11-10"
status: "backlog"
priority: "low"
assignee: null
epic: "WFC 2.0"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:19:39.000Z"
completedAt: null
labels: ["idea", "editor"]
order: "b11"
---
# WFC 2.0: Measured tile shares in the preview

Show each tile's real share next to its weight share in the tile set preview. In 1.1, weights also change which cells are filled first, so the real share can differ a lot from the weight share (the plateau problem on `wfc-1-1-demo-weights`).

**Size:** S–M

## Design notes
- Run a small background solve (for example 16×8×16, a fixed seed) whenever the tile set changes, with the same `SolveJob` and cancellation as Generate.
- Show "weight share / measured share" per tile, and flag big gaps.
