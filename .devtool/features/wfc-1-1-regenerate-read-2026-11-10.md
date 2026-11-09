---
id: "wfc-1-1-regenerate-read-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-09T23:03:56.000Z"
completedAt: null
labels: ["engine"]
order: "c4"
---
# WFC 1.1: Free previously generated cells on Generate

When reading fixed cells, a cell that matches the stored record (same item and orientation) is free again. A cell that differs is hand-placed and stays fixed. Clear Bounds First is unchanged.

Brief: §11.2.

## Done when
- Engine tests: an unchanged generated cell is freed, an edited one is kept, a hand-placed one is kept, cells outside the bounds are untouched.
- Generating twice with different seeds gives the second result without undoing (manual check).
