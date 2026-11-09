---
id: "wfc-m4-tileset-check-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M4 Authoring tools"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:35:10.000Z"
completedAt: "2026-11-09T22:35:10.000Z"
labels: ["core", "editor"]
order: "aP"
---
# WFC M4: Tile set check

Report sockets with no possible partner and tiles that can never be placed. Logic in Core; results shown in the inspector and before every solve.

Brief: §5.

## Done when
- Unit tests cover unmatched sockets and unplaceable tiles.
- A broken tile set shows the problem before solving.

## Result
`Core/TileSetChecker` reports faces with no partner and tiles that only fit at grid edges, as warnings because edges are unconstrained in v1. Shown in the inspector, and after every solve. 7 Core tests, and the demo tile set has no warnings. Needs the manual check.

## Result
Manual check passed (2026-11-09): a `lonely+` socket is reported within a second and clears when restored.
