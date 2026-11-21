---
id: "dgn-1-2-1-box-pivot-centre-2026-11-21"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-21T10:00:00.000Z"
modified: "2026-11-21T10:40:00.000Z"
completedAt: null
labels: ["v1.2.1", "common", "editor", "bug"]
order: "h1"
---
# godoodungeon 1.2.1: Room box node sits at its floor centre, not its corner

A `DungeonRoomBox` node sits at the box's minimum corner, so its move gizmo and position are off to one side of the room. Put the node at the **centre of the box's floor**: centred on X and Z, on the bottom face on Y. The placement lives in the shared `GridMapBox` base (`addons/editor_tools_common/Nodes/GridMapBox.cs`), so `WfcPinBox` gets the same pivot (decided 2026-11-21: both boxes, floor centre). Ships as godoodungeon 1.2.1 and godoowave 1.5.1.

Brief: WFC brief §16.1 (node position follows Origin), godoodungeon brief §13.1.

## Plan
- `GridMapBox`: add `PivotOf(map, origin, size)` = `CornerOf(origin)` + half the box's X and Z extent (sides below 1 count as 1, like the gizmo). `PlaceAtOrigin` puts the node there; the `Size` setter re-places it too, so the pivot follows a resize.
- Moving the node: Origin = `NearestCell` of position minus that half extent, so a move still snaps Origin to the nearest cell and keeps Size.
- `GridMapBoxGizmoPlugin.Corners`: relative to the pivot instead of the corner. Outline and handles stay where they are in the map.
- Origin and Size stay the box's minimum cell and extent: no change to `HasCell`, layout, solving, saved scenes or the dungeon graph. Saved scenes re-place the node on load, so no migration.
- Tests: update `WfcPinBoxTests` (placement, round-trip, move snapping, gizmo match) to the floor centre; add a move test with an odd and an even size, and a resize test that the node follows. A room box placement test in `DungeonRoomBoxTests`.
- Docs: WFC brief §16.1 wording, both CHANGELOGs (1.2.1, 1.5.1), version in both `plugin.cfg`, READMEs if they mention the corner.

## Done when
- Core and engine tests pass; headless editor load shows no errors.
- Manual (editor): a room box and a pin box show their move gizmo at the floor centre; moving snaps to cells; handle drag and undo keep outline and handles in place; an existing scene (`demo/godoodungeon/dungeon_rooms.tscn`) opens with its boxes where they were.

## Results (2026-11-21)
- `GridMapBox.PivotOf(map, origin, size)` = `CornerOf(origin)` + half the X and Z extent (sides below 1 count as 1). `PlaceAtOrigin` uses it, the `Size` setter now re-places the node, and a move subtracts the half extent before `NearestCell`. `GridMapBoxGizmoPlugin.Corners` is relative to the pivot, so outline and handles stay put in the map. `HasCell`, layout, solving and the graph are unchanged (they use Origin and Size only).
- Tests: `WfcPinBoxTests` updated (round-trip, move snapping, gizmo match) plus `The_node_sits_at_the_floor_centre_for_odd_and_even_sizes_and_follows_a_resize`; `DungeonRoomBoxTests` round-trip checks the floor centre. Core 519/519, engine 154/154, headless editor load without errors.
- Docs: WFC brief §16.1, both CHANGELOGs (godoodungeon 1.2.1, godoowave 1.5.1), both `plugin.cfg`, both READMEs, root README versions.
- Open: the manual editor check under *Done when* (also after an assembly reload), then the release review.
