---
id: "dgn-1-0-1-door-tile-by-item-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T08:53:11.000Z"
modified: "2026-11-16T10:34:32.000Z"
completedAt: null
labels: ["v1.0.1", "resources", "adapters", "bug"]
order: "d4"
---
# godoodungeon 1.0.1: Door Tile points at another tile after the room tile set changes

Review finding 1 (MINOR). `DoorTile` (`DungeonSettings.cs:79`, drop-down in `_ValidateProperty` 209-216) is stored as a position in `RoomTileSet.Tiles`. Deleting or reordering an earlier tile makes the stored settings point at a different tile. `DungeonTileRules.Check` only catches this when the new tile does not have exactly one `outside+` face. A wall tile has exactly one, so it passes with only the "weight above 0" warning. The connectivity check does not catch it either: it counts a room as reached once a corridor reaches its door cell, whatever tile stands there.

- Room tile set `[floor, door, wall, …]`, Door Tile = door (index 1). Delete `floor`: index 1 is now `wall`. Generate puts walls at every door. Every room is sealed, the corridors end at blank walls, and the run reports success.

## Done when
- The door is stored by its MeshLibrary item id (unique per tile, `TileSetBinding` enforces it) and looked up as a tile index when Generate builds the request (`DungeonGridMap.Request`, `DungeonTileSets.Check`). The drop-down still shows tile names.
- Settings saved by 1.0.0 (index only) still load and map to the same tile once, then save the item id.
- Engine test: delete a tile before the door tile; Generate still places the door tile at every door.
- Manual: the case above in the editor.

## Result
- `DungeonSettings.DoorTile` (position) is replaced by **`Door`** (MeshLibrary item, `NoDoor` = -1 = `WfcTile.EmptyItem`; board decision 2026-11-16 on the label). The drop-down lists the room tiles' names with their items as values; empty tiles are left out, since several tiles may share the empty item and `TileSetBinding` does not map it.
- 1.0.0 files: `DoorTile` is now a private, non-exported property (see the review below), which maps the position to that tile's item as soon as the room tile set is known (in either property order). It is not in the property list, so a re-save writes `Door` only. A position that pointed at an empty tile, or out of range, loads as no door, and Generate reports "Choose the Door tile from the room tile set."
- `DungeonTileSets.Check` looks up the tile index (`DoorTile`) with `TileForItem`; `DungeonGridMap.Request` passes that index to the Core, which is unchanged. A door item no longer in the room tile set gives the same "Choose the Door tile" error.
- Messages say "Door tile" instead of "Door Tile". README and CHANGELOG updated. The demo scenes (`dungeon_free`, `dungeon_obstacles`, `prototype_m2_dungeon`) now say `Door = <item>` (item and position were the same there), and the two demo build scripts set `Door` by item.
- Engine tests: `DungeonSettingsTests.The_door_is_a_drop_down_of_the_room_tiles_by_item`, `A_door_tile_saved_by_1_0_0_loads_as_that_tiles_item_and_saves_as_door` (both property orders, -1, out of range, re-save), `DungeonGridMapTests.Deleting_a_tile_before_the_door_tile_keeps_the_door` (Generate puts item 3 at every door after a tile in front was deleted). 374 Core and 112 engine tests pass; headless editor load clean.

## Manual check
1. Open `demo/godoodungeon/dungeon_free.tscn`, open the dialog: Door shows "Door".
2. Room tile set `[floor, door, wall, …]` case: in a copy of the room tile set, move the Door tile to the top (or delete a tile in front of it). Generate: doors are still door tiles.
3. Open a scene saved with 1.0.0 (e.g. `git show godoodungeon-v1.0.0:demo/godoodungeon/dungeon_free.tscn > /tmp/old.tscn`, copied into the project): Door shows "Door"; save it, and the file has `Door = 10` and no `DoorTile`.

## Review (stern-reviewer, 2026-11-16)
- MAJOR, fixed: the first version read `DoorTile` in `_Set`, which only runs when a file is parsed. An assembly reload restores members by name through the generated `RestoreGodotObjectData` and never calls `_Set`. So settings that the 1.0.0 class had loaded (the README's update path: reopen the project on the stale DLL, then Build) came back with Door = none, and saving the scene lost the door for good. `DoorTile` is now a private non-exported property declared after `Door`: the loader and the reload both set it by name (checked in the generated serialization code), and the -1 its getter saves for a 1.0.1 instance leaves `Door` alone. Test: `Set("DoorTile", 4)` maps to the item; `Set("DoorTile", -1)` after `Door = 3` keeps 3.

## Manual check (added)
4. Stale-assembly update: with godoodungeon 1.0.0 built, open `dungeon_free.tscn` (its GridMap stores 1.0.0 settings), close Godot, put these sources in place without building, reopen the project, then Build and focus Godot. Open the dialog: Door shows "Door". Save: the GridMap's settings say `Door = 10`.
