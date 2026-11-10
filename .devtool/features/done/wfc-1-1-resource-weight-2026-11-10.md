---
id: "wfc-1-1-resource-weight-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T08:48:59.000Z"
completedAt: "2026-11-10T08:48:59.000Z"
labels: ["engine"]
order: "c1"
---
# WFC 1.1: Weight on WfcTile and in the preview

Export `Weight` on `WfcTile` (range 0 and up, default 1), pass it through `TileSetBinding`, and show each tile's weight and its share of the total in the tile set preview.

Brief: §11.1.

## Done when
- Engine test: a 1.0 `.tres` without the field loads with weight 1, and a saved weight round-trips.
- The preview shows weight and share for the selected tile (manual check).

## Result
- `WfcTile.Weight` is a float, exported with range 0 and up (default 1), and passed through `ToDefinition()`.
- The tile set preview shows a line under the pickers: "Weight W: P% of the total weight, shared by its rotations". Identical copies are counted together. Weight 0 shows "never generated, but it can be placed by hand". The weight is part of the refresh signature and of the key that merges copies in the picker.
- Engine tests, all passing: a 1.0 tile `.tres` loads with weight 1, weights 2.5 and 0 round-trip through a saved tile set, and a weight error reads "Tile 1: Weight …".
- Waiting on: a manual check of the preview line.

## Result
- Manual check passed (2026-11-10): the weight line shows the share, and switches to the weight-0 text after an edit.
