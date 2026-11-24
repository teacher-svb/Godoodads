# Changelog

All notable changes to godoodungeon are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- **Several socket labels on one face**, from the shared WFC solver (godoowave 1.5): separate them with commas, such as `wall, window`. A face connects to a face that matches any of its labels. Room Edge Socket and Path Socket stay single names; a face counts as a room edge or a corridor opening when any of its labels has that name.

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
