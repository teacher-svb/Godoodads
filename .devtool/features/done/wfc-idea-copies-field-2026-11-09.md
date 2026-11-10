---
id: "wfc-idea-copies-field-2026-11-09"
status: "done"
priority: "low"
assignee: null
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-10T09:02:19.000Z"
completedAt: "2026-11-10T09:02:19.000Z"
labels: ["idea", "authoring"]
order: "b1"
---
# WFC 1.1: Copies field on tiles

A `Copies` count on `WfcTile` instead of repeating entries in the list, which keeps big tile sets editable.

**Size:** XS

## Design notes
- Only worth doing if per-tile weights do not ship in the same release, because weights replace copies. Decide when 1.1 is planned.
- The preview already lists identical copies once.

## Decision (2026-11-10)
Dropped from 1.1, because weights replace copies. Close as superseded when 1.1 ships (see the docs ticket).

## Result
- Closed as superseded by 1.1 (2026-11-10): weights replace copies, and a tile of weight 3 behaves like three copies of weight 1.
