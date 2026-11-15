---
id: "wfc-idea-socket-editing-2026-11-09"
status: "done"
priority: "low"
assignee: null
epic: "WFC Generator"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-10T13:00:00.000Z"
labels: ["v1.3.0", "idea", "editor"]
order: "b5"
---
# WFC 2.0: In-viewport socket editing

Edit sockets directly in the tile set preview instead of the six text fields.

**Size:** M

## Design notes
- Click a face label in the preview to edit it in place, with suggestions from the labels already used in the tile set, so typos create fewer unmatched sockets.
- Optionally show matching faces of other tiles side by side.
- Builds on the M4 `TileSetPreview`. All edits must go through undo.

## Decision (2026-11-10, 1.3 review)
Moved to *WFC 1.3*.

## Result
Split into tickets for 1.3 (2026-11-10, brief §13). Decisions are recorded in the brief.
