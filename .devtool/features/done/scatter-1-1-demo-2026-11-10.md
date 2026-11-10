---
id: "scatter-1-1-demo-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T17:45:00.000Z"
completedAt: "2026-11-10T17:45:00.000Z"
labels: ["demo"]
order: "g4"
---
# Scatter 1.1: GridMap demo

A tool scene builds a small tile library and a GridMap scene; grass scattered on open tops and slopes with a projected path mask.

Brief: §10.3.

## Done when
- The builder makes the assets; an engine test scatters on the demo GridMap with no instance inside a covered face.

## Result
- `DemoBuilder` builds `assets/gridmap_tiles.tres` (Block, Paving, Ramp as a 2 m prism rising to +X) and `demo_gridmap.tscn`: a 12 x 12 ground layer with a paving strip, a plateau with a row of ramps, and three loose blocks; 6000 grass blades with Slope Max 50 degrees, Paving excluded, and `mask_paths.png` projected Top Down and inverted (the mask is white on the paths). The other demos rebuild unchanged (only property order differed, so they were restored).
- Engine test (`GridMapDemoTests`): 6000 instances, none inside a block, none on paving, some on ramps (read from the MultiMesh buffer). Engine 70 pass.

Manual check passed (2026-11-10): the demo shows grass on open tops and ramps with path gaps and none on paving or covered faces; the dialog shows the GridMap group for Tiles, reports cells and covered triangles, the cell box, Projection UV/Top Down and undo work; the group, header included, is hidden for mesh surfaces; works after a reload.
