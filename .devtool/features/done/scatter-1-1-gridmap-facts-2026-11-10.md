---
id: "scatter-1-1-gridmap-facts-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T16:20:00.000Z"
completedAt: "2026-11-10T16:20:00.000Z"
labels: ["adapters"]
order: "g0"
---
# Scatter 1.1: Verify GridMap facts

Check on Godot 4.7, with the editor running (godot MCP), as in M0: how a cell's item mesh ends up in world space (`MapToLocal`, `GetCellItemBasis`, `MeshLibrary.GetItemMeshTransform`, `CellScale`, cell centring); whether `ToolDialog`'s embedded inspector can hide a property group depending on the picked node; how the node picker filters supported nodes. Record results here and in brief §10.

Brief: §10.1.

## Done when
- Each fact has a recorded result, with an engine test where headless can show it.

## Result
Checked on Godot 4.7, 2026-11-10:

1. **Cell transform.** In GridMap space a cell's mesh is `Transform3D(GetCellItemBasis(cell).Scaled(CellScale), MapToLocal(cell)) * GetItemMeshTransform(item)`. An engine probe compared this with `GridMap.GetMeshes()` for three centring/scale setups (all centred, scale 1; X and Z not centred, scale 1.7; Y not centred, scale 0.6), four orientations (0, 16, 10, 7), a non-uniform cell size and a rotated, scaled, offset item mesh transform: every cell matched. The adapter computes it per cell (it needs the cell coordinate for covered faces); `GetMeshes` is the reference in its test.
2. **Cell bounds.** A cell spans `cell * CellSize` to `(cell + 1) * CellSize` regardless of the centring flags, which only move `MapToLocal`. A covered face is a triangle whose three vertices lie on one of those planes (with a small tolerance), with an occupied neighbour on that side.
3. **Hiding the GridMap group.** `ScatterSettings._ValidateProperty` already hides options by making them storage-only (mask options without a mask). A non-stored flag set by the plugin when a surface is picked, plus `NotifyPropertyListChanged`, hides the GridMap group the same way. Whether Godot also drops the empty group header is checked in the editor with the UI card.
4. **Node picker.** `NodePickerRow` takes a list of type names; adding `"GridMap"` enables it. `SurfaceSource.IsSupported` needs the same.
