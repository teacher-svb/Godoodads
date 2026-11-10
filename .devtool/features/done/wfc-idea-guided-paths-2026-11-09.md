---
id: "wfc-idea-guided-paths-2026-11-09"
status: "done"
priority: "medium"
assignee: null
epic: "WFC 1.3"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: "2026-11-10T13:00:00.000Z"
labels: ["idea", "core"]
order: "b3"
---
# WFC 2.0: Guided connected paths

Guarantee a route between points by placing the path first, then letting WFC fill around it. This reuses fixed cells.

**Size:** M. The guaranteed global version stays a README idea and needs a brief.

## Design notes
- The user draws or picks start and end points. A path is found over the grid (for example A*, with height steps limited to what stairs and ramps allow) and written as fixed path tiles, then the normal solve runs.
- Path tiles need sockets that connect along the path. The demo would need path variants.

## Open questions
- Is the path drawn by hand or generated between points?
- How does a path climb: does it pick stairs and ramps itself?

## Decision (2026-11-10, 1.3 review)
Moved to *WFC 1.3*. The path is generated between two path cells the user places by hand (answers the first open question). The climbing question stays open for planning.

## Result
Split into tickets for 1.3 (2026-11-10, brief §13). Decisions are recorded in the brief.
