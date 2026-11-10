---
id: "scatter-1-1-gridmap-ui-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T17:45:00.000Z"
completedAt: "2026-11-10T17:45:00.000Z"
labels: ["editor", "resources"]
order: "g2"
---
# Scatter 1.1: GridMap options in the dialog

The node picker accepts GridMap. A GridMap group in `ScatterSettings` (items, Include/Exclude, cell box), shown only for GridMap surfaces where possible. Presets keep working.

Brief: §10.1.

## Done when
- Engine test: settings round-trip; old presets load with defaults.
- Manual: pick a GridMap, filter items, set a box, undo, and after an assembly reload.

## Result
- `ScatterSettings` *GridMap* group (Items, Item Mode Include/Exclude, Box Origin, Box Size) and `ToGridMapSelection()`. A non-saved `SurfaceIsGridMap` flag hides the group (storage-only) unless the surface is a GridMap.
- The plugin: the node picker accepts GridMap; the flag is set on pick, on opening the dialog and on loading a preset; GridMap surfaces are read with the selection, and the status area reports the cells read and the covered triangles left out.
- Engine tests (`GridMapSettingsTests`): round-trip, old presets get defaults, the group is hidden unless the flag is set, and the flag is not saved. Engine 69 pass.
- Waiting for the manual editor check, together with the demo.

Manual check passed (2026-11-10): the demo shows grass on open tops and ramps with path gaps and none on paving or covered faces; the dialog shows the GridMap group for Tiles, reports cells and covered triangles, the cell box, Projection UV/Top Down and undo work; the group, header included, is hidden for mesh surfaces; works after a reload.
