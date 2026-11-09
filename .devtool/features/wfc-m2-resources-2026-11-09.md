---
id: "wfc-m2-resources-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["engine"]
order: "aA"
---
# WFC M2: Tile set and settings resources

`WfcTile`, `WfcTileSet` and `WfcSettings` with exported properties, readable enum labels and basic validation before a solve.

Brief: §4.1, §4.3, §5.

## Done when
- Resources save and load as `.tres`, and convert to the Core tile set in an engine test.
- The referenced MeshLibrary is never modified.
