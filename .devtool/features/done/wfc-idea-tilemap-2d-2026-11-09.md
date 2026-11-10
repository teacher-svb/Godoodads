---
id: "wfc-idea-tilemap-2d-2026-11-09"
status: "done"
priority: "medium"
assignee: null
epic: "WFC 1.3"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: "2026-11-10T13:00:00.000Z"
labels: ["idea", "2d"]
order: "b4"
---
# WFC 2.0: 2D TileMapLayer adapter

Generate into a `TileMapLayer`, reusing the solver, which already supports 2D.

**Size:** L

## Design notes
- A 2D tile resource maps a TileSet source, atlas coordinates and an alternative tile to sockets on four faces.
- `TileMapLayer` supports flips and transposes, so mirrored variants become possible. This is new for the variant compiler, which only rotates today.
- Needs a reader, writer, undo, a dialog entry for `TileMapLayer`, a 2D preview and a demo.

## Open questions
- Should mirrored variants be generated automatically, like rotations?
- Use Godot's terrain sets to suggest sockets, or keep sockets fully manual?

## Decision (2026-11-10, 1.3 review)
Moved to *WFC 1.3*. Re-sized to L: a reader, writer, undo, dialog entry, 2D preview, demo and mirrored variants are all new.

## Result
Split into tickets for 1.3 (2026-11-10, brief §13). Decisions are recorded in the brief.
