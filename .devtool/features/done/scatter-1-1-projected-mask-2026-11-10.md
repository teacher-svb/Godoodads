---
id: "scatter-1-1-projected-mask-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T17:45:00.000Z"
completedAt: "2026-11-10T17:45:00.000Z"
labels: ["core", "adapters"]
order: "g3"
---
# Scatter 1.1: Projected masks

Mask → Projection: UV or Top Down. Top Down maps world X/Z over the surface's top-down area (or the cell box) to mask UV.

Brief: §10.2.

## Done when
- Core tests: corner and centre mapping, +X right and +Z down; UV mode unchanged.
- Manual: one mask over a GridMap.

## Result
- `MaskProjection` (UV, Top Down) on `ScatterSettings` under Placement Mask; with Top Down the UV set is hidden and a surface without UVs is fine.
- `MaskFilter` takes an optional top-down area: the sample's world X/Z mapped over it, +X right and +Z down. The plugin uses the GridMap cell box (its eight corners in world space) when one is set, otherwise the surface's extent seen from above.
- Core tests (`MaskProjectionTests`): corners, centre and direction mapping; the area from points; on a quad whose UVs all sit on the black pixel, Top Down accepts the white half and rejects the black half, while UV mode rejects both. Core 231, engine 69 pass.
- Waiting for the manual check with the demo.

Manual check passed (2026-11-10): the demo shows grass on open tops and ramps with path gaps and none on paving or covered faces; the dialog shows the GridMap group for Tiles, reports cells and covered triangles, the cell box, Projection UV/Top Down and undo work; the group, header included, is hidden for mesh surfaces; works after a reload.
