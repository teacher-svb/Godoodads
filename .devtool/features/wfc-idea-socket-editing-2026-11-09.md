---
id: "wfc-idea-socket-editing-2026-11-09"
status: "backlog"
priority: "low"
assignee: null
epic: "WFC 2.0"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "authoring"]
order: "b5"
---
# WFC 2.0: In-viewport socket editing

Edit sockets directly in the tile set preview instead of the six text fields.

**Size:** M

## Design notes
- Click a face label in the preview to edit it in place, with suggestions from the labels already used in the tile set, so typos create fewer unmatched sockets.
- Optionally show matching faces of other tiles side by side.
- Builds on the M4 `TileSetPreview`. All edits must go through undo.
