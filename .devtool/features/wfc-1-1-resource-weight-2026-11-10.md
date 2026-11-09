---
id: "wfc-1-1-resource-weight-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-09T23:03:56.000Z"
completedAt: null
labels: ["engine"]
order: "c1"
---
# WFC 1.1: Weight on WfcTile and in the preview

Export `Weight` on `WfcTile` (range 0 and up, default 1), pass it through `TileSetBinding`, and show each tile's weight and its share of the total in the tile set preview.

Brief: §11.1.

## Done when
- Engine test: a 1.0 `.tres` without the field loads with weight 1, and a saved weight round-trips.
- The preview shows weight and share for the selected tile (manual check).
