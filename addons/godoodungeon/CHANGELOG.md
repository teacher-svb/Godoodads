# Changelog

All notable changes to godoodungeon are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.3.0] - 2026-11-24

### Added
- **Several socket labels on one face**, from the shared WFC solver (godoowave 1.6): separate them with commas, such as `wall, window`. A face connects to a face that matches any of its labels. Room Edge Socket and Path Socket stay single names; a face counts as a room edge or a corridor opening when any of its labels has that name. Tile sets with one label per face give the same dungeon for the same settings and seed. Installed next to godoowave, use godoowave 1.6.0 or later: both zips carry the shared solver, and an older godoowave would replace it with one that rejects commas.

## [1.2.1] - 2026-11-21

### Changed
- **A room box's node sits at the centre of its floor**, not at its corner: centred on X and Z, on the bottom layer. The move gizmo is now in the middle of the room, and the node follows when you resize the box. Origin and Size still name the box's first cell and its extent, so saved scenes load with their boxes where they were. Installed next to godoowave, use godoowave 1.5.1 or later, which places pin boxes the same way.

## [1.2.0] - 2026-11-20

### Added
- **Room boxes** (`DungeonRoomBox`): child nodes of the GridMap that fix a room where you put it; the dungeon is laid out and connected around them. **Generated** boxes fix a room's position and size and are filled with the room tile set; **Built** boxes keep the cells you built and get a corridor to every Door tile on their wall ring. Boxes count towards Room Count and are always the first rooms, so their numbers do not change between seeds. Add one with **Room Boxes → Add Room Box** or *Add Child Node*; drag its handles with the box or the GridMap selected. Errors name the box and draw it in red; the status counts the boxes; Preview Layout draws them per mode.
- **The dungeon graph knows the boxes:** `DungeonRoom.Box` and `IsBuilt`, and a box's **Data** is copied to its room on every Generate, so it survives regenerating.
- A room box demo: `demo/godoodungeon/dungeon_rooms.tscn`, with a Built boss room and a Generated entrance; `graph_demo.gd` heads for a room whose Data says `boss`.

### Changed
- godoodungeon now needs the shared box node of godoowave 1.5 (`editor_tools_common/Nodes/`), which its zip contains. Installed next to godoowave, use godoowave 1.5 or later.

## [1.1.0] - 2026-11-20

### Added
- **Dungeon graph** for gameplay: Generate stores a `DungeonGraph` resource in the GridMap's metadata (`godoodads_dungeon_graph`), in the same undo step as the cells. Rooms are its vertices and the corridors, one per room graph connection with its cells from door to door, its edges. It answers which room holds a cell or a global position, the corridors through a cell, neighbours, distances in corridors and in corridor cells, shortest paths, the farthest room and the two rooms farthest apart. Every room and corridor has a `Data` dictionary for your own values. It works at runtime without the plugin, from GDScript and C#. Clear Dungeon removes it, and the status warns when Generate replaces a graph that carried Data. See *Dungeon graph* in the README.
- **Preview Layout numbers the rooms**, with the numbers the status messages use (`DungeonRoom.Number`).
- **Graph demo:** `demo/godoodungeon/dungeon_graph.tscn` with `graph_demo.gd`, which marks a start, a goal and the shortest way between them at runtime.

### Changed
- **Undo and redo of Generate set the dialog's Seed** to the seed of the dungeon now on the map, so Preview Layout shows that dungeon. Until now the preview kept showing the undone dungeon while the dialog was open. Other settings stay as you left them.

## [1.0.1] - 2026-11-16

### Changed
- WFC Generator is now called godoowave (1.4.0, `addons/godoowave/`). The README links to it, and the warning about WFC Generator older than 1.3.3 now says to replace it with godoowave.
- **Door Tile is now called Door, and is stored by its MeshLibrary item** instead of its position in the room tile set. Settings saved by 1.0.0 load with the same door tile and are saved the new way.

### Fixed
- **The door stays the same tile when the room tile set changes.** Deleting or reordering a tile before the door tile made the door point at another tile; with a wall there, Generate sealed every room, put walls at the doors and still reported success.
- **Height curves apply to one-layer grids.** Corridors, and rooms with Room Height 1, ignored the tiles' height curves and used their plain weights. They now use each curve at its bottom (height 0), as taller rooms and the corridor routing already did. Tile sets without height curves give the same dungeon as in 1.0.0 for the same settings and seed; with curves, corridors and one-layer rooms can change. The empty corridor tile is also chosen by its weight at the bottom of its curve.
- **Preview Layout follows the GridMap's cells.** It only recomputed when a setting changed, so painting, erasing or editing cells left it showing a layout Generate would not make. It now checks the cells a few times per second.
- **The GridMap stores the settings a dungeon was generated with.** The dialog stays editable while a generation runs, and a Seed typed or a bounds handle dragged meanwhile ended up in the stored settings, so reopening the dialog no longer reproduced the dungeon. Changes made during a generation now only apply to the next Generate, and the bounds handles are hidden while it runs.
- **Undoing a bounds resize changes the settings that were resized.** After dragging the bounds of one GridMap and opening the dialog for another, Ctrl+Z moved the second GridMap's bounds to the first one's old position.
- **The tile set preview updates when External Sockets change.** After adding `outside-` to the room tile set's External Sockets, as this README advises, the `outside+` faces stayed red and listed as having no partner until a tile was edited or the tile set was selected again.

## [1.0.0] - 2026-11-15

First release.

### Added
- **Generate Dungeon (GDDS)** in the GridMap menu of the 3D viewport toolbar, shared with WFC Generator. It fills a `GridMap` with a dungeon of rooms and corridors, as one undo step, from a room tile set and a corridor tile set (`WfcTileSet`).
- **Layout:** rooms of random size placed apart by a gap; a room graph (minimum spanning tree plus Extra Connections as loops); doors on the side facing each neighbour, one per side at most.
- **Rooms** solved with wave function collapse: the room bounds supply a one-sided edge socket (`outside-`), so every room is closed by walls, corners and its doors.
- **Corridors** routed along the room graph from door to door, keeping Wall Clearance from rooms and keeping apart from each other, then solved with the corridor tile set (straights, bends, T pieces, crosses, empty).
- A connectivity check after every generation, and up to Layout Attempts whole layouts before giving up.
- **Preview Layout** draws the rooms, doors and corridors Generate would make, as the settings change.
- **Bounds** drawn in the viewport with handles on each face; drags snap to cells and undo in one step.
- **Clear Dungeon** removes the last dungeon as one undo step. Cells placed or edited by hand are never changed, and rooms and corridors avoid them.
- Marker scenes on tiles (lights, spawn points), placed in `DungeonMarkers`.
- Status messages for every failure, with red boxes in the viewport where a layout failed; Randomise Seed puts a failed seed back.
- Settings and a record of the written cells are stored on the GridMap, so a dungeon can be reproduced and replaced.
- A warning when WFC Generator older than 1.3.3 is installed next to it.
- Demo: a dungeon MeshLibrary, room and corridor tile sets with torch and spawn markers, `dungeon_free.tscn` and `dungeon_obstacles.tscn`.
