---
id: "wfc-idea-marker-tiles-2026-11-09"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 2.0"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "editor"]
order: "b2"
---
# WFC 2.0: Marker tiles that place scenes

Tiles that also place a scene, such as a door, spawn point or light, as a node at the cell.

**Size:** M

## Design notes
- Add an optional `PackedScene` on `WfcTile`. After a successful solve, instance it at the cell's transform under a container node next to the GridMap, inside the same undo action.
- Regenerating must remove the markers previously placed in the bounds, which needs them tagged, for example with metadata.

## Open questions
- Where do marker nodes live: a child of the GridMap, or a sibling container?
- Should markers also be allowed on empty tiles (a spawn point in the air above ground)?
