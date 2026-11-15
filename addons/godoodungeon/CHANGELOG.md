# Changelog

All notable changes to godoodungeon are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

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
