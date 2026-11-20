---
id: "dgn-1-1-preview-labels-2026-11-20"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T18:30:00.000Z"
completedAt: null
labels: ["v1.1.0", "editor", "ui"]
order: "g5"
---
# godoodungeon 1.1: Room numbers in Preview Layout

Preview Layout labels each room with its number (1-based, as in status messages), readable from above and at an angle, hidden with the preview.

Brief: §12.4.

## Done when
- Manual (review): labels on the demo dungeon match `Rooms[n].Number`; survive assembly reload; gone when the preview is off.

## Results (2026-11-20)
- `GodoodungeonPlugin.UpdateRoomLabels`: one `Label3D` per preview room, centred on the top of its box, showing the room number (index + 1, as status messages and `DungeonRoom.Number`). Billboard, no depth test, room colour with outline, pixel size scaled to the cell size (font 64 × 0.025 × cell size, so about 1.6 cells tall). Children of the owner-less overlay, rebuilt only when the overlay redraws, so they are never saved and go away with Preview Layout or the dialog.
- Builds without warnings; headless editor load clean. Not visible headless: needs the manual check below.

## Manual check (board)
Build, focus Godot, then open the dialog on `demo/godoodungeon/dungeon_graph.tscn`'s GridMap and turn on Preview Layout:
1. Each room box shows its number, readable zoomed out to the whole dungeon and when orbiting.
2. With the scene's seed (1), the numbers match the labels `graph_demo.gd` shows when the scene runs (room 3 start, room 12 goal).
3. Change Seed: labels follow the new layout. Turn Preview Layout off or close the dialog: labels gone. Save: no labels in the .tscn.
4. Rebuild with the dialog open (assembly reload): no errors, labels come back.
