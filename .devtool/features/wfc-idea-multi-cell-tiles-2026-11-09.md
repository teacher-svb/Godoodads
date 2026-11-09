---
id: "wfc-idea-multi-cell-tiles-2026-11-09"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 2.0"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "core"]
order: "b1"
---
# WFC 2.0: Multi-cell tiles

Modules larger than one cell, such as a 2×1×2 house or a tall tower.

**Size:** M

## Design notes
- Split each large module into one part per cell at compile time. Internal faces get unique generated socket labels, so the parts can only appear together. The solver itself does not change.
- The writer places the item only in the anchor cell and leaves the other cells empty, but those cells must still count as occupied when reading fixed cells.
- The preview has to show the whole module.

## Open questions
- How does the user author the footprint: a size field on `WfcTile`, with outer sockets per cell face?
- How do occupied-but-empty cells survive a reload of the scene?
