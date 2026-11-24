# Project Godoodads: Godot Editor Tools

## Sub-project 3: godoodungeon (Dungeon Generator)

**Decisions by:** the board (Syndias)
**Status:** M0 (foundations) approved on 2026-11-15, with WFC Generator 1.3.3 released alongside it. WFC Generator has since been renamed godoowave (1.4.0, `addons/godoowave/`, namespace `Godoodads.Wave`; WFC brief §15); this brief keeps the old names and paths where it records M0. M1 (layout core), M2 (solve pipeline), M3 (editor), M4 (demo tile set) and M5 (release) approved on 2026-11-15: version 1.0.0, tagged `godoodungeon-v1.0.0` after merging. Version 1.1.0 (the dungeon graph, §12) planned on 2026-11-20, and 1.2.0 (room boxes, §13) on the same day. Milestones M0–M5 below, tracked in the epic `godoodungeon` with the labels `m0` to `m5`.
**Reference material:** the research summary in §11. The demo tile set is created for this project.

---

### 1. Context

WFC Generator fills a `GridMap` with modules from local socket rules. That gives good terrain and ruins, but not dungeons: socket rules cannot express "a closed room of 5 to 9 cells across with two doors, joined to every other room by a corridor", because WFC has no global structure (WFC brief §6.1).

Dungeon Generator adds that global structure in front of the WFC solver. A **layout** stage decides where the rooms are and where their doors go. Then the WFC solver fills each room with a room tile set. The corridors are routed along the room graph, from each room's door to its neighbour's door, and the WFC solver fills them in with the corridor tile set (card `dgn-m3-edge-router`; M0–M2 used guided paths, see §4.4). This is the hybrid most recent work uses: an abstract layout for the global structure, and WFC for local detail (§11).

The suite's conventions (`CONVENTIONS.md`) apply, and it reuses `addons/editor_tools_common/`.

---

### 2. Goal

Deliver a Godot editor addon that lets a level designer select a `GridMap`, choose dungeon bounds, a room tile set, a corridor tile set and a few layout numbers, and press **Generate** to get closed rooms that are all reachable through corridors. Hand-placed cells inside the bounds stay in place, and rooms and corridors avoid them.

---

### 3. Technical baseline

| Item | Decision |
|---|---|
| Engine | Godot 4.7 or later, .NET build |
| Language | C# |
| Addon | `addons/godoodungeon/`, namespace `Godoodads.Dungeon`, release tag `godoodungeon-v<version>` |
| Naming | The suite's addons move to `godoo<name>` names. The existing ones become `godoowave` and `godooscatter` in a later rename, which is not part of this project. The shared folder and the namespaces above keep their current names until then |
| Solver | The WFC solver, moved into `addons/editor_tools_common/Wfc/` and shared with WFC Generator (decided 2026-11-15) |
| Output in v1 | One `GridMap`: rooms and corridors in the same map (decided 2026-11-15) |
| Floors in v1 | One floor (decided 2026-11-15). Corridors stay on the bottom layer of the bounds (since `dgn-m3-edge-router`) |
| Rooms in v1 | Generated rectangles, filled by WFC (decided 2026-11-15) |
| Usage | Editor-only, with `Core/` kept free of editor and engine objects |
| Target size | 64×64 cells, 20 rooms. Size is freely configurable |
| Testing | Core unit tests without the engine, gdUnit4 for adapters, demo scenes for manual checks |

#### Layout

```
addons/editor_tools_common/
  Editor/        Existing shared editor UI
  Wfc/Core/      WFC solver, moved from addons/wfc_generator/Core (namespace Godoodads.Common.Wfc.Core)
  Wfc/Resources/ WfcTile, WfcTileSet, WfcSettings (namespace Godoodads.Common.Wfc)
  Wfc/Adapters/  GridMap read and write, tile set binding (namespace Godoodads.Common.Wfc.Adapters)
  Wfc/Editor/    Tile set preview and its inspector plugin, bounds gizmo (namespace Godoodads.Common.Wfc.Editor)
addons/wfc_generator/
  Editor/        Unchanged behaviour, now using the shared solver
addons/godoodungeon/
  plugin.cfg, README.md, CHANGELOG.md
  Core/          Room placement, doors, building the solve inputs, connectivity check
  Resources/     DungeonSettings
  Adapters/      Dungeon read and write on a GridMap, generated record
  Editor/        Plugin, dialog, layout overlay, run
demo/godoodungeon/
tests/godoodungeon/
```

---

### 4. Functional requirements

#### 4.1 Shared solver

- WFC Generator's `Core/`, `Resources/` and `Adapters/` move to `addons/editor_tools_common/Wfc/`. Each script keeps its `.uid` file, so files saved in the editor (which reference scripts by uid) keep loading once the editor has scanned the project (§10.1).
- **Legacy stubs:** files saved by scripts outside the editor reference scripts by path only (§10.1), our own demo assets included. For those, `addons/wfc_generator/Resources/` keeps `WfcTile.cs`, `WfcTileSet.cs` and `WfcSettings.cs` as empty `[Tool]` subclasses of the moved classes, in the namespace `Godoodads.WfcGenerator.Legacy`, with **new** `.uid` files and without `[GlobalClass]`. Old files load as the subclass, which the shared code accepts. Extracting 1.3.3 over 1.3.2 overwrites the old scripts and their `.uid` files with the stubs, so no uid is duplicated. The stubs stay until the `godoowave` rename, which has the same problem and resolves it the same way.
- The demo build scenes run again after the move, so the demo assets reference the new paths.
- WFC Generator's behaviour does not change. It ships as **1.3.3** with only this move in its CHANGELOG, and all its tests pass unchanged apart from namespaces.
- The move happens before any WFC 1.4 card starts, so the 2D adapter is built on the shared code.
- Every release zip already contains `editor_tools_common` (`tools/package_addon.sh`), so Dungeon Generator works without WFC Generator installed. Installing both needs WFC Generator 1.3.3 or later. With 1.3.2 or older next to the shared folder, both copies of each resource script carry the same uid, which resolves to the **old** copy, so saved tile sets load as types the shared code does not accept (§10.2). The README says so, and the Dungeon Generator plugin warns when it finds `addons/wfc_generator/plugin.cfg` older than 1.3.3.
- `tests/Core.Tests` compiles `addons/**/Core/**` instead of `addons/*/Core/**`, so the shared Core is tested.

#### 4.2 Tile sets

- Dungeon Generator uses two ordinary `WfcTileSet` resources with the same `MeshLibrary`: a **room tile set** (floors, walls, corners, doors, props) and a **corridor tile set** (path pieces and an empty tile). No new tile set type.
- **Room edge socket** (setting, for example `outside`): the socket name of the faces that point out of a room. It must be **one-sided**. Wall, corner and door tiles carry `outside+` on their outer faces, and the four side faces of each room's bounds get `outside-` as their boundary socket (WFC brief §12.1). Only the boundary can then face an outer face, which forces walls around every room and keeps them out of the interior. With a symmetric `outside`, two outer faces can meet inside a room, and the M0 prototype filled its rooms with back-to-back walls and clusters of corners. A room tile set that uses the edge socket name without a `+` on any tile face is a validation error. The room tile set should list `outside-` in its **External Sockets** (a `WfcTileSet` field added in M4, see the WFC Generator CHANGELOG): the room bounds supply it, and without it the shared tile set check and preview flag every `outside+` face as having no partner. godoodungeon warns when it is missing.
- **Door tile** (setting): a tile of the room tile set with exactly one horizontal face that has the room edge socket. That face is the door's outside. A door tile that does not qualify is a validation error. Weight 0 suits it: the solver then never places doors by itself (WFC brief §11.1), and only the layout does.
- **Path socket** (setting): the corridor tile set's path socket, as in WFC brief §13.3. The corridor tile set needs every piece a corridor can be made of: straights along X and Z, bends, T pieces (all in four rotations) and a cross, plus a variant without path faces, with a weight above 0, for the space between corridors. Missing any of them is a validation error. (T pieces were found missing in `dgn-m2-gridmap-write`; bends and the cross became necessary with the router of `dgn-m3-edge-router`.)

#### 4.3 Layout

The layout is pure Core, runs on a 2D grid of the bounds' X and Z, and is deterministic for a seed.

- **Settings:** room count, minimum and maximum room size (X and Z, walls included), room height in layers (default 1), **Wall Clearance** in cells (default 1; 0 lets corridors run along room walls), the **gap** between rooms in cells (default 3, at least 2 × Wall Clearance + 1 so a corridor fits between the clearance rings of two rooms, and at least 2 so a door's corridor piece always faces a free cell), and a margin to the bounds edge (default 1).
- **Room placement:** rooms are placed one at a time at seeded random positions and sizes inside the bounds. A position is rejected when its footprint plus the gap overlaps another room or an occupied cell (a hand-placed cell, see 4.7). A room that finds no place in a fixed number of attempts is skipped, and the next rooms still try, since their random sizes may be smaller (decided at the M1 review); the status reports the shortfall. Fewer than 2 rooms is a failure.
- **Room graph:** a minimum spanning tree over the room centres (Prim's algorithm on the complete graph, ties broken by the seed), plus **Extra Connections**, 0 to 1 (default 0.15): loops per tree edge, so round(share × (rooms − 1)) of the shortest left-out edges are added (decided at the M1 review; a share of all left-out pairs grew with the square of the room count, 8 loops for 12 rooms).
- **Doors:** for each graph edge, a room that already has a door on a side facing the other room uses it (decided at the M1 review, which halved the loops' door count); otherwise it gets a door on the side that faces the other room best, at the non-corner wall cell closest to it. A side has **at most one door**: edges that leave through the same side share it, and the corridors branch outside (so two doors are never next to each other). Outside every door, the lane through the Wall Clearance (at least one cell) plus one more cell (the entry cell, where routing starts) must be inside the bounds, free of occupied cells and clear of other rooms and their clearance rings, and may not overlap another door's lane or entry cell: guided paths never routes through another open face's entry cell, so two doors sharing one deadlock, and two lanes that meet close into a pair with no open face (found in `dgn-m2-corridor-inputs`). Each room has at least one door; a room that cannot get one is a layout error. **Max Doors Per Room** (default 4) caps new doors: an edge that would need one more door uses the room's door that faces closest to the other room, so no edge is dropped. Guided paths joins every door into one network, so a door per room is all connectivity needs (decided 2026-11-15, card `dgn-m1-doors`).

#### 4.4 Building the solve inputs

The layout becomes ordinary WFC inputs. Core builds them directly as `initial` arrays, `SolveOptions.Boundary` and `SolveOptions.Restrictions`, so no placeholder items are written to the GridMap.

- **Room solve**, one per room: the bounds are the room's footprint × room height, starting at the bounds' bottom layer. The four side faces have the room edge socket as their boundary. The door cells are fixed to the door tile, rotated so that its outside faces out of the room. Other cells are free.
- **Corridor routing** (decided 2026-11-15 after the board saw corridors wrapping around rooms in the M2 review scene; card `dgn-m3-edge-router`): rooms are nodes of the path network. For each graph edge, one corridor runs from the lane outside one room's door to the lane outside the neighbour's door (`DungeonLayout.EdgeDoors`), by A* over the layout grid. Room footprints with their Wall Clearance ring, hand-placed cells and other doors' lanes are blocked. A shared corridor cell is cheap, so corridors that meet merge into T or cross pieces. A cell next to a corridor it does not join costs extra, which keeps corridors apart (measured: 4 side-by-side pairs over 2,813 corridor cells in 50 dungeons). Turns cost a little, and Path Wander adds seeded noise. Guided paths joined each door to the *nearest* corridor instead, so a room's second door got a corridor running round the room into the first.
- **Corridor solve**, one for the whole dungeon, on the bottom layer of the bounds' footprint: room cells and hand-placed cells are `Excluded`. Every routed cell (lanes included) may only use corridor variants with exactly its path faces, and every other cell only variants without path faces (`CellRestrictions`), so the solve draws the planned corridors and nothing else, while weights still choose among the variants that fit.

#### 4.5 Solve run

- Order: layout → rooms (each with a seed derived from the Generate seed and the room index) → corridors. A room that fails is retried with the next derived seed, up to the WFC restart limit. Then the layout is retried with a derived seed, up to **Layout Attempts** (default 4).
- **Connectivity check** after the corridor solve: a flood fill over path faces and room interiors reaches every room. A failed check is a bug, not a user error, and is reported as one.
- The run uses `SolveJob` on a background thread, with the same cancellation and assembly-reload handling as WFC Generator (WFC brief §4.7). GridMap writes happen on the main thread.
- Generate is one undo action over the whole bounds. Animation is not in v1: the result is written once.
- **Seed** with the Randomise Seed checkbox, as in WFC 1.3.1. A failed randomised Generate restores the seed (WFC 1.3.2).

#### 4.6 Writing and regenerating

- Rooms and corridors are written to the selected GridMap. Each item and orientation comes from the tile set binding, as in WFC Generator.
- The generated cells are stored as a generated record in the GridMap's metadata, under a key separate from WFC Generator's, together with `DungeonSettings`, so a dungeon can be reproduced from the scene file.
- **Generate** on a GridMap with a dungeon record first clears the cells of that record, then generates. Cells that changed since then count as hand-placed and stay.
- **Clear Dungeon** removes the recorded cells, as one undo action.
- Marker tiles (WFC brief §12.4) work for both tile sets, so spawn points and lights can come from the room tile set.

#### 4.7 Hand-placed cells

- Occupied cells inside the bounds that are not in the dungeon record are hand-placed. They are never overwritten. The layout treats them as obstacles, and the corridor solve treats them as excluded.

#### 4.8 Failure reporting

- Errors before solving: invalid tile set settings (4.2), a room size that does not fit the bounds, and room count limits.
- Layout shortfalls ("placed 14 of 20 rooms") are warnings, not failures.
- Room and corridor failures report the room index or the corridor that could not be routed, with coordinates, and use WFC Generator's failure overlay.
- Errors and warnings go to the dialog status area and the editor toaster.

---

### 5. UI requirements

- A toolbar entry "Generate Dungeon (GDDS)" appears when a `GridMap` is selected and opens a `ToolDialog`. It is an item in the same **GridMap** menu button as WFC Generator's "Generate WFC (GDDS)". That button is shared: whichever addon is enabled first creates it, and the last one to leave removes it (§10.3).
- The `WfcTileSet` inspector shows the tile set preview once, with either addon or both enabled (§10.4).
- The dialog holds the tile sets, socket and door settings, bounds, layout settings, seed, and limits as exported properties on `DungeonSettings`, with Generate and Clear Dungeon buttons.
- The bounds have viewport handles, as in WFC Generator.
- **Preview Layout** draws the planned rooms, doors and graph edges as an overlay without solving, so the layout numbers can be tuned quickly. The overlay is never saved.
- The status area shows progress per stage, then a result such as: `Placed 18 rooms, 23 doors, 1 loop in 2.1 s. Seed 4821.`

---

### 6. Known pitfalls

1. Corridors next to a room get no socket matching against the room's walls, because the room cells are excluded. In the M0 prototype, a corridor along a room wall gave a thick double wall. Wall Clearance (4.3, 4.4) keeps corridors away from rooms. The corridor router also keeps corridors apart (4.4).
2. A corridor cannot be routed when rooms, clearance rings and hand-placed cells close every way between two doors (for example a wall of hand-placed cells across the bounds). The layout attempts (4.5) try other layouts and then report the corridor. With guided paths (M2), 1–2 of 100 layouts also failed on crowded 1-cell channels; the router has no such case. The acceptance criteria measure reliability.
3. Moving the shared solver breaks files saved without uids, and a mixed install with an old WFC Generator loads the wrong types (§10.1, §10.2). The legacy stubs and the version warning in §4.1 cover both.
4. The corridor solve runs over the whole dungeon footprint. At the target size that is small (4,096 cells per layer), but large bounds with many layers need a check in M5.
5. `SpatialMenuHook` and `TileSetInspectorPlugin` were built for one addon per menu and per inspector. With two, items clash and previews double (§10.3, §10.4). `editor_tools_common` is also replaced by the newest copy when a second addon is installed (root README), so the fixed hook must keep its current API for MultiMesh Scatter 1.2.0.

---

### 7. Milestones

Each milestone ends with a board review. Cards use the epic `godoodungeon` with the labels `v1.0.0` and the milestone, for example `m1`.

| # | Milestone | Content |
|---|---|---|
| M0 | Foundations | §10 checks, hand-built prototype with the existing WFC tool, solver moved to common, WFC Generator 1.3.3, addon skeleton |
| M1 | Layout core | Room placement, room graph with loops, doors, layout validation, determinism, unit tests |
| M2 | Solve pipeline | `DungeonSettings`, room and corridor solve inputs, run order and retries, connectivity check, GridMap write, record, undo |
| M3 | Editor | Dialog, bounds handles, Preview Layout overlay, background run, cancel, failure reporting, Clear Dungeon |
| M4 | Demo tile set | Script-built dungeon MeshLibrary with room and corridor tile sets, demo scenes. **Board check on result quality: decide whether room roles, multi-floor or room stamps move into a later release** |
| M5 | Release | Acceptance pass, performance, README with screenshots, CHANGELOG, version 1.0.0 |

---

### 8. Acceptance criteria

Automated unless noted:

- **Determinism:** same seed, settings and hand-placed cells produce identical output.
- **Connectivity:** every room is reachable from every other room in every completed generation, checked by flood fill over 50 seeds.
- **Closed rooms:** every room's outer faces are walls or doors, and every door leads to a corridor.
- **Layout rules:** rooms never overlap and keep the gap. Doors are never on corners and never face the bounds edge.
- **Hand-placed cells:** they are unchanged after Generate and Clear Dungeon.
- **WFC unchanged:** WFC Generator's tests pass after the move. A WFC scene saved with 1.3.2 loads with 1.3.3 and generates the same result for the same seed.
- **User assets:** the MeshLibrary and both tile sets are unchanged after generation.
- **Undo:** undo restores every cell in the bounds, and redo reapplies the result. (Manual check.)
- **Cancel and reload:** cancelling restores the earlier state, and rebuilding during a run neither crashes nor hangs the editor. (Manual check.)
- **Reliability:** at least 19 of 20 seeds complete on the demo tile sets at the target size.
- **Performance:** 64×64 with 20 rooms completes in under 10 seconds on the board's dev machine. (Manual check, report timings.)

---

### 9. Out of scope for v1

- Multiple floors with rooms at different heights.
- Hand-built room stamps (prefab rooms placed as fixed cells, Edgar style).
- Room roles (start, boss, treasure), mission graphs, lock and key, cyclic generation.
- Non-rectangular rooms and BSP or cellular-automata layouts.
- Animated solve.
- A 2D `TileMapLayer` adapter (it follows once WFC 1.4 exists).
- Runtime (in-game) generation.

Note further ideas as idea cards in the epic `godoodungeon` with the labels `v2.0.0` and `idea` or in the repo's `README.md` backlog rather than building them.

**Decided at the M4 review (2026-11-15):** none of the idea cards (multiple floors, room stamps, room roles and progression) moves into a 1.x release yet. godoodungeon 1.0.0 ships first, and the board decides after feedback.

---

### 10. Assumptions to verify in M0

1. **Checked 2026-11-15, partly true.** Moving a C# script with its `.uid` file keeps files that reference it **by uid** loading, including `WfcSettings` in node metadata, but only after the editor has scanned the project. Before that, a stale uid cache points to the old path, and a missing cache falls back to the text path. Files that reference it **by path only** fail to load for good. `ResourceSaver.Save` outside the editor writes no uids, so every file our demo build scenes made is path-only (5 of the 7 WFC demo files). Stubs at the old paths fix all cases (§4.1). Changing the namespace has no effect on loading. Details are on `dgn-m0-verify-uid-move`.
2. **Checked 2026-11-15.** An old copy next to the moved copy (same uid, same class names, other namespace) gives no error, only "UID duplicate detected" warnings. The uid resolves to the old copy, and the global class list holds both. The editor works, but the shared code gets the old types (§4.1). In the same namespace, the duplicate C# types would not compile, so the namespace change is required.
3. **Checked 2026-11-15: not safely today.** `SpatialMenuHook.FindHost` matches any menu button with the host icon, so the second addon hooks into the first one's fallback button. Both number their items from 7300, so one press runs both callbacks. Disabling the button's creator removes the button and the other addon's item. Disabling the other addon removes the first item with its id, which is the wrong one. **Decision:** the fallback button is shared. It carries metadata with its users and their wanted visibility, items get the first free id, the button is visible when any user wants it, and the last user frees it. The hook's public API stays the same. Card `dgn-m0-shared-menu-inspector`.
4. **Checked 2026-11-15.** Two registered `TileSetInspectorPlugin`s show two previews. **Decision:** the preview, its inspector plugin and the bounds gizmo move to `editor_tools_common/Wfc/Editor/`. Each addon registers its own inspector plugin, and all instances join a list kept as metadata on the editor's base control. `_CanHandle` is true only for the first live instance. A prototype showed one preview with two instances, and still one after the first instance was removed. Card `dgn-m0-shared-menu-inspector`.
5. **Checked 2026-11-15, headless and by the board in the editor.** With the existing solver, a hand-built layout of three rooms, a room tile set with a one-sided edge socket and placed doors, and a corridor tile set with straight pieces placed outside the doors give closed rooms. All 6 doors are joined into one network in a single route attempt, and the room cells are excluded from the corridor solve automatically. A symmetric edge socket fails (see §4.2). Corridors can run right along a room's wall, and two corridors can run side by side. The board accepted the rooms and chose Wall Clearance against walls (4.3), and an idea card for corridors side by side.

---

### 11. Research summary (2026-11-15)

- **Constructive room and corridor generators:** BSP, and random placement with a Delaunay triangulation, a minimum spanning tree, re-added edges for loops and A* corridors (TinyKeep, [Vazgriz](https://vazgriz.com/119/procedurally-generated-dungeons/), with stairs in 3D). godoodungeon follows this recipe: the room graph is the MST plus loops, and since `dgn-m3-edge-router` its own A* routes one corridor per graph edge. Guided paths, used until M2, joined doors nearest first instead, which made corridors wrap around rooms.
- **Graph and room templates:** [Edgar](https://ondrejnepozitek.github.io/Edgar-Unity/docs/introduction/) places hand-made room templates along a level graph. This is the model for the room stamps idea after v1.
- **Mission grammars and cycles:** [Dormans 2010](https://pcgworkshop.com/archive/dormans2010adventures.pdf) and [Unexplored](https://www.boristhebrave.com/2021/04/10/dungeon-generation-in-unexplored/). This is the model for room roles and lock and key after v1.
- **Layout first, then WFC:** [CG-WFC](https://blog.ptidej.net/content/files/2025/11/_ICSE_GAS_Laurent____Graph_WFC_Procedural_Gen-1_compressed.pdf), [Growing Graph WFC](https://dl.acm.org/doi/full/10.1145/3807895.3807922). This is the approach chosen here.
- **Global constraints in WFC:** [DeBroglie path constraints](https://github.com/BorisTheBrave/DeBroglie/blob/master/docs/articles/path_constraints.md). These fix connectivity but not room shapes. Not needed, because the layout gives both.
- Rejected for v1: a two-level WFC (room archetypes on a coarse grid, then tiles). It needs the same fine-level work, plus a second tile set and its own connectivity handling, and gives uniform grid-aligned rooms.

---

### 12. Version 1.1: Dungeon graph

**Decided by the board on 2026-11-20.** godoodungeon 1.1.0 keeps the room graph after Generate, so gameplay code can use it: rooms are the vertices, the corridors joining them the edges. Tracked in the epic `godoodungeon` with the label `v1.1.0`.

Until now the room graph (§4.3) and the routed corridors (§4.4) exist only while a dungeon is generated. The GridMap keeps cells and the generated record, so a game cannot tell which room a cell belongs to, which rooms are neighbours, or how far apart two rooms are.

#### 12.1 What an edge is

- **One edge per room graph edge** (decided 2026-11-20): the tree edges and the Extra Connections of §4.3, each with the corridor routed for it (§4.4). An edge knows its two rooms, the door each room uses, whether it is a tree edge or a loop, its route cells (lanes included) and its **length**: the number of route cells from door to door.
- Corridors that meet share cells (§4.4), so a corridor may pass a T or cross piece of another one. Those junctions are **not** edges: the graph is the designed layout, not every walk the map allows. A shared cell belongs to every edge whose route uses it, and its length counts for each. The README says so, because a player can take the shortcut a junction offers.
- Every edge of a valid dungeon has a route: an edge whose room got no door is a layout error (§4.3), and a corridor that cannot be routed fails the layout attempt. `CorridorRouter` keeps each edge's path (it only kept the path faces until now).

#### 12.2 Data and storage

- New resources in `Resources/`, all `[GlobalClass]` and `[Tool]`, so they load in a game without the plugin enabled (the addon folder must still be in the project, since the C# assembly holds them):
  - `DungeonGraph`: the rooms, the corridors, the bounds origin and size, and the queries of §12.3.
  - `DungeonRoom`: index, footprint (GridMap cells: `Position` of the lowest corner and `Size`, Y being the height in layers), centre cell, door cells with the step out of the room through each (`DoorDirections`), the indices of its corridors, and `Data`.
  - `DungeonCorridor`: index, rooms A and B, door cells, tree edge or loop, route cells, length, and `Data`.
- **Generate stores the graph** in the GridMap's metadata under `godoodads_dungeon_graph`, in the same undo action as the cells and the generated record. The scene file saves it as embedded sub-resources, as it already saves `DungeonSettings`. **Clear Dungeon** removes it; undo and redo restore it. A dungeon generated with 1.0.x has no graph until it is generated again.
- `DungeonGraph.StoredOn(GridMap)` returns the graph, or null when the GridMap has none, and `StoreOn` writes it, as `DungeonSettings` does. GDScript cannot call C# static methods, so it reads `grid_map.get_meta(&"godoodads_dungeon_graph", null)`. GDScript sees the C# member names unchanged (`graph.Rooms`, `graph.ShortestPath(a, b)`), as for every C# script in Godot (decided in `dgn-1-1-graph-resource`).
- **Custom data** (decided 2026-11-20): each room and corridor has a `Data` dictionary (`Godot.Collections.Dictionary`) for gameplay code, such as `{"role": "boss"}` or `{"locked": true}`. The plugin never reads it. Generate replaces the whole graph, so data set in the editor is lost on regenerating, and the dialog says so when the old graph has any.
- **Room numbers:** status messages and the preview say "room 1" for the first room (§4.8). The API is 0-based like Godot's arrays: `Rooms[0]` is room 1. `DungeonRoom.Number` gives the number a person reads.

#### 12.3 Queries

Every public member uses Variant-compatible types (`int`, `Vector3I`, `Vector3`, `PackedInt32Array`, `PackedVector3Array`, `Godot.Collections.Array` of the resources above), so GDScript calls them as easily as C# (decided 2026-11-20). The algorithms live in `Core/` on plain arrays and are tested without the engine; the resource converts.

- **Distances and paths** (decided 2026-11-20):
  - `HopDistances(room)`: per room, the number of corridors on the fewest-corridor way from `room` (breadth-first), −1 when unreachable (cannot happen in a valid dungeon, but the API does not rely on it).
  - `Distances(room)`: per room, the shortest total corridor length from `room` (Dijkstra over corridor lengths). Walking through a room costs nothing in 1.1.
  - `ShortestPath(from, to)` and `ShortestPathCorridors(from, to)`: the rooms and the corridors of that way, by length, ties broken by fewer hops, then lower indices, so the answer is fixed.
  - `FarthestRoom(room)` by length, and `Diameter()`: the two rooms farthest apart (a `Vector2I`, lower index first), for a start and an end.
- **Spatial lookup** (decided 2026-11-20):
  - `RoomAtCell(Vector3I)`: the room whose footprint holds the GridMap cell on X and Z (walls included), or −1. The layer is ignored, since 1.1 has one floor, so any layer above a room finds it (decided in `dgn-1-1-graph-core`).
  - `CorridorsAtCell(Vector3I)`: the corridors whose route holds the cell on X and Z, in index order; more than one at a junction or on a lane shared by two corridors.
  - `RoomAtPosition(GridMap, Vector3)`: the same for a global position, through the GridMap's transform and cell size, so a character's position finds its room.
  - `RoomCentre(GridMap, room)`: the global position of a room's centre, at the height of its bottom layer's cell centres, for spawning.
  - Neighbours: `Neighbours(room)` and `CorridorBetween(a, b)` (−1 when none).
- Not in 1.1: structure queries (dead ends, bridges, articulation rooms, loop membership). They are a few lines on top of the same Core and can follow when a game needs them.

#### 12.4 Editor

- **Preview Layout** labels each room with its number, so a designer can match the graph to the map.
- The status after Generate adds "graph: 12 rooms, 14 corridors (3 loops)".

#### 12.5 Demo and docs

- A demo scene with a small GDScript that reads the graph at runtime: it takes `Diameter()` as start and end, marks both rooms and the rooms on the shortest path between them, and prints the hop and corridor distances. A C# version of the same calls goes in the README.
- README: a "Dungeon graph" section with the data, the junction caveat of §12.1, the API and both examples. CHANGELOG 1.1.0.

#### 12.6 Acceptance

1. After Generate, `DungeonGraph.StoredOn(map)` has as many rooms as the layout and one corridor per room graph edge; every route cell is a corridor cell of the map and every footprint cell a room cell.
2. Saving and reopening the scene gives the same graph; undo of Generate restores the previous graph (or none); Clear Dungeon removes it and its undo brings it back.
3. Every query of §12.3 is called from GDScript in an engine test, and gives the same answer as in C#.
4. Core tests: hop and length distances, paths and diameter on hand-built graphs with loops and ties; `RoomAtCell` and `CorridorsAtCell` on every cell of a generated layout.
5. The same seed and settings give the same graph.
6. A dungeon with a graph from a game export (headless, plugin disabled) loads and answers `RoomAtCell`.

---

### 13. Version 1.2: Room boxes

**Requested by the board on 2026-11-20.** Room boxes are child nodes of the GridMap that fix a room at a place you choose. The dungeon is laid out and connected around them. They combine the fixed-position variant of the room stamps idea card (`dgn-idea-room-stamps`) with the box nodes of godoowave 1.5 (WFC brief §16). Tracked in the epic `godoodungeon` with the label `v1.2.0`, after 1.1.0. Room stamps placed by the layout stay a `v2.0.0` idea.

Decisions (board, 2026-11-20):
- **Mode per box:** **Generated** fixes a room's position and size, and the room is solved with the room tile set like any other. **Built** keeps the cells you built inside the box, and the dungeon only connects corridors to it.
- **Doors of a Built room are the door tiles you place** on its wall ring. Your walls are never changed. A Built box without a door is an error.
- **Room Count includes room boxes:** boxes take places from it, and the layout generates the rest. 12 with one box gives 1 box and 11 generated rooms.

#### 13.1 The room box node

- New node **`DungeonRoomBox`** (`[Tool, GlobalClass]`) in `addons/godoodungeon/Nodes/`, outside `#if TOOLS`. It derives from the box node base that godoowave 1.5 adds to `editor_tools_common` (WFC brief §16.1): Origin, Size, Enabled, the position kept in sync with Origin, and the gizmo with handles. Only enabled direct children of the GridMap count.
- Exports: **Mode** (Generated, Built; default Generated) and **Data** (`Godot.Collections.Dictionary`, copied to the room in the dungeon graph, 13.4).
- **Footprint:** X and Z of the box, walls included, as for generated rooms. Its bottom layer must be the bounds' bottom layer. Size Y is the room's height in layers, so a box may be taller than Room Height.
- The dialog's **Room Boxes** group has an **Add Room Box** button: a Generated box in the middle of the bounds at Max Room Size and Room Height, selected, as one undo step.

#### 13.2 Layout

- **Order:** room boxes first, in child order, then generated rooms. Room 1 is the first box, so the room numbers of boxes stay the same between seeds.
- **Room Count:** generated rooms fill Room Count minus the number of boxes. With more boxes than Room Count, every box is still used and the status warns. Fewer than 2 rooms in all is a failure, as today.
- **Checks before the layout** (errors, with red boxes in the viewport): a box outside the bounds or within the Margin; two boxes closer than the Gap; a box smaller than 3 × 3 (Min and Max Room Size do not apply to boxes); a Generated box that covers occupied cells that are not the last dungeon's (make it Built or clear them).
- Generated rooms keep the Gap from boxes, as from each other. Hand-placed cells outside the boxes stay obstacles (§4.7).
- **Room graph:** boxes are rooms like any other in the spanning tree and the Extra Connections (§4.3).
- **Doors of a Generated box:** placed by the door placer as for generated rooms, with Max Doors Per Room.
- **Doors of a Built box:** the non-corner cells of its wall ring (bottom layer) that hold the Door item. The side is the ring side the cell is on. Max Doors Per Room does not apply. An edge to the room uses its door that faces the other room best. A door that no edge uses gets one more edge, to the nearest other room it does not already join, marked as a loop, so no door opens into nothing. The lane and entry cell rules of §4.3 apply to these doors too, and a blocked one is a layout error naming the box and the door.
- Layout attempts (§4.5) only vary the generated rooms; boxes stay where they are.

#### 13.3 Solve, write and clear

- **Generated box:** solved as a room (§4.4) with the box's footprint and height; its cells go into the dungeon record, and Generate and Clear Dungeon replace them as usual.
- **Built box:** not solved. Its cells are excluded from the corridor solve, as room cells are, and never enter the record, so Generate and Clear Dungeon never change them (they are hand-placed cells, §4.7).
- A disabled box does nothing: a Built box's cells become ordinary hand-placed obstacles.
- Determinism: the same seed, settings, hand-placed cells and boxes give the same dungeon.

#### 13.4 Graph

- `DungeonRoom` (§12.2) gets **`Box`**: the box's node path relative to the GridMap, empty for generated rooms, and **`IsBuilt`**. Its `Data` starts as a copy of the box's Data, so data set on a box survives regenerating (§12.2 says Generate replaces the graph). The Data warning of §12.2 leaves out rooms whose Data came from a box unchanged.

#### 13.5 Editor

- **Handles:** the GridMap selected with the dialog open shows the bounds and every enabled room box with its handles; a selected room box shows its own handles, dialog open or not; as for godoowave pin boxes (WFC brief §16.3). A drag is one undo step on the node.
- **Preview Layout** draws room boxes in their own colour (Generated and Built differ), with their room number, and Built doors.
- Status: "12 rooms (2 room boxes, 1 built)", and errors name the box node ("Room box `BossRoom` (room 2) has no door").

#### 13.6 Demo and docs

- A demo scene with a Built boss room (hand-built, two doors, Data `{"role": "boss"}`) and a Generated entrance room at a bounds edge; the 1.1 graph demo script finds the boss room by its Data.
- README "Room boxes" section; the README limitation "a hand-built room is not connected to the dungeon" changes to point at Built boxes. CHANGELOG 1.2.0.

#### 13.7 Acceptance

1. Over 50 seeds with two boxes (one of each mode) and 10 rooms in all: every dungeon has 10 rooms, rooms 1 and 2 at the boxes, every door of the Built box joined by a corridor, and the Built cells unchanged.
2. Generate, regenerate and Clear Dungeon never change a Built box's cells; a Generated box's cells are replaced.
3. Every check of 13.2 gives its error with the box named.
4. The graph names the box of each box room and carries its Data after regenerating.
5. godoowave 1.5 and godoodungeon 1.2 installed together: pin boxes and room boxes share the box node base and both work.

#### 13.8 Not in 1.2

Room stamps placed by the layout, non-rectangular boxes, rotated boxes, boxes on other floors, and forcing which rooms a box connects to (the room roles idea).

---

### 14. Version 1.3: several socket labels on one face

**Decided by the board on 2026-11-24.** Ships the comma-separated socket lists of the shared WFC solver (WFC brief §17, godoowave 1.6.0) to godoodungeon users. Tracked with the label `v1.3.0`.

- Room and corridor tile sets may list several labels on a face (`wall, window`). The tile rules check each label: a face is a room edge when any label is `<edge>+`, and a corridor opening when any label has the Path Socket name. A list may not hold `<edge>-`, as single labels may not.
- Room Edge Socket and Path Socket stay single names.
- Tile sets with one label per face give the same dungeon for the same settings and seed as 1.2.1.
- Installed next to godoowave, godoowave must be 1.6.0 or later, because both zips carry `editor_tools_common`.

#### 14.1 Version 1.3.1: several path sockets and a Doorway Socket

**Decided by the board on 2026-11-24.** Corridors built from several kinds of piece, of which only some may stand right outside a door: `corridor` and `pitfall` pieces, without a pitfall at the door. Tracked with the label `v1.3.1`.

- **Path Socket** takes a comma-separated list (`corridor, pitfall`, spaces optional). A face is a corridor opening when any of its labels has one of the names. The corridor router is unchanged; the cell restrictions, the tile set checks and the connectivity check use the list.
- **Doorway Socket** (`DungeonSettings.DoorwaySocket`, a comma-separated list, empty by default): the corridor cell outside a door must have one of these names on its face towards the door. The cell restrictions then group variants by path face mask and door face mask. Empty adds no restriction.
- Doorway names must be Path Socket names (error). A listed name no corridor tile uses is a warning; no tile with any of the names is an error, as is a side from which no piece can face a door with a doorway name.
- One Path Socket name and an empty Doorway Socket give the same restriction classes in the same order, so the same dungeon for the same seed as 1.3.0.
- Only godoodungeon changes, so `editor_tools_common` stays as in godoowave 1.6.0.
