---
id: "wfc-m4-valid-neighbours-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M4 Authoring tools"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:35:10.000Z"
completedAt: "2026-11-09T22:35:10.000Z"
labels: ["editor"]
order: "aO"
---
# WFC M4: Valid neighbours list

Per-face "show valid neighbours" list, using the Core adjacency table.

Brief: §5.

## Done when
- The list matches the adjacency table on the demo tile set.

## Result
Per-face neighbour list from the Core adjacency table. It exposed that the demo walls used half-turn rotation and could only run along X. Fixed by using quarter turns, and the rotation option now reads "Half Turn (2 rotations)" and "Quarter Turns (4 rotations)". Needs the manual check.

## Result
Manual check passed (2026-11-09): six face lines. For Stairs at 0°, `+X (landing)` lists Stairs and Plinth, and walls appear under the open faces.
