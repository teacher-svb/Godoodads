# Project Godoodads: Godot Editor Tools

## Sub-project 3: godoodungeon (Dungeon Generator)

**Decisions by:** the board (Syndias)
**Status:** M0 (foundations) approved on 2026-11-15, with WFC Generator 1.3.3 released alongside it. M1 (layout core) is next. Milestones M0–M5 below, tracked in the epic `godoodungeon` with the labels `m0` to `m5`.
**Reference material:** the research summary in §11. The demo tile set is created for this project.

---

### 1. Context

WFC Generator fills a `GridMap` with modules from local socket rules. That gives good terrain and ruins, but not dungeons: socket rules cannot express "a closed room of 5 to 9 cells across with two doors, joined to every other room by a corridor", because WFC has no global structure (WFC brief §6.1).

Dungeon Generator adds that global structure in front of the WFC solver. A **layout** stage decides where the rooms are and where their doors go. Then the WFC solver fills each room with a room tile set, and fills the space between the rooms with corridors using guided paths (WFC brief §13.3), which already joins every path end into one connected network. This is the hybrid most recent work uses: an abstract layout for the global structure, and WFC for local detail (§11).

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
| Floors in v1 | One floor. Corridors may climb inside their layer range if the corridor tile set has stairs (decided 2026-11-15) |
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
- **Room edge socket** (setting, for example `outside`): the socket name of the faces that point out of a room. It must be **one-sided**. Wall, corner and door tiles carry `outside+` on their outer faces, and the four side faces of each room's bounds get `outside-` as their boundary socket (WFC brief §12.1). Only the boundary can then face an outer face, which forces walls around every room and keeps them out of the interior. With a symmetric `outside`, two outer faces can meet inside a room, and the M0 prototype filled its rooms with back-to-back walls and clusters of corners. A room tile set that uses the edge socket name without a `+` on any tile face is a validation error.
- **Door tile** (setting): a tile of the room tile set with exactly one horizontal face that has the room edge socket. That face is the door's outside. A door tile that does not qualify is a validation error. Weight 0 suits it: the solver then never places doors by itself (WFC brief §11.1), and only the layout does.
- **Path socket** (setting): the corridor tile set's path socket, as in WFC brief §13.3. The corridor tile set needs a straight path variant (path faces on two opposite sides) and a variant without path faces for the space between corridors. Missing either is a validation error.

#### 4.3 Layout

The layout is pure Core, runs on a 2D grid of the bounds' X and Z, and is deterministic for a seed.

- **Settings:** room count, minimum and maximum room size (X and Z, walls included), room height in layers (default 1), **Wall Clearance** in cells (default 1; 0 lets corridors run along room walls), the **gap** between rooms in cells (default 3, at least 2 × Wall Clearance + 1 so a corridor fits between the clearance rings of two rooms), and a margin to the bounds edge (default 1).
- **Room placement:** rooms are placed one at a time at seeded random positions and sizes inside the bounds. A position is rejected when its footprint plus the gap overlaps another room or an occupied cell (a hand-placed cell, see 4.7). After a fixed number of failed attempts per room, the layout stops with fewer rooms and the status says so. Fewer than 2 rooms is a failure.
- **Room graph:** a minimum spanning tree over the room centres (Prim's algorithm on the complete graph, ties broken by the seed), plus **Extra Connections**, 0 to 1 (default 0.15): that share of the remaining shortest edges is added, which gives loops.
- **Doors:** for each graph edge, each of the two rooms gets a door on the side that faces the other room. A door is never on a corner cell, never next to another door on the same side, and never on a side that faces the bounds edge or an occupied cell with less than one free cell in between. Each room has at least one door. **Max Doors Per Room** (default 4) caps the count, and edges over the cap are dropped as long as the graph stays connected.

#### 4.4 Building the solve inputs

The layout becomes ordinary WFC inputs. Core builds them directly as `initial` arrays, `SolveOptions.Boundary` and a `PathRequest`, so no placeholder items are written to the GridMap.

- **Room solve**, one per room: the bounds are the room's footprint × room height, starting at the bounds' bottom layer. The four side faces have the room edge socket as their boundary. The door cells are fixed to the door tile, rotated so that its outside faces out of the room. Other cells are free.
- **Corridor solve**, one for the whole dungeon: the bounds are the dungeon bounds' footprint × the corridor layer range (default the bottom layer only). Room cells and occupied cells are `Excluded`. The ring of Wall Clearance cells around each room is fixed to the corridor tile set's variant without path faces (its empty tile), so routes never touch a room wall (decided 2026-11-15, M0 review: a socket rule cannot do this reliably, because guided paths lays routes before the solve and only checks path faces). Outside each door, a lane of Wall Clearance cells (at least one) is fixed to the corridor straight variant that runs through the door. Its face towards the excluded door counts as connected (WFC brief §13.3), so guided paths only routes its outward face. Guided paths then joins all doors into one network, and every cell off the routes gets a variant without path faces.

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
- Room and corridor failures report the room index or the path error from guided paths, with coordinates, and use WFC Generator's failure overlay.
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

1. Corridors next to a room get no socket matching against the room's walls, because the room cells are excluded. In the M0 prototype, a corridor along a room wall gave a thick double wall. Wall Clearance (4.3, 4.4) keeps corridors away from rooms. Two corridors can still run side by side; route spacing is the idea card `dgn-idea-route-spacing`.
2. Guided paths can fail when doors face each other across a narrow gap, or when routes crowd each other. Its 8 route attempts and the layout attempts (4.5) cover most cases. The acceptance criteria measure reliability.
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

---

### 10. Assumptions to verify in M0

1. **Checked 2026-11-15, partly true.** Moving a C# script with its `.uid` file keeps files that reference it **by uid** loading, including `WfcSettings` in node metadata, but only after the editor has scanned the project. Before that, a stale uid cache points to the old path, and a missing cache falls back to the text path. Files that reference it **by path only** fail to load for good. `ResourceSaver.Save` outside the editor writes no uids, so every file our demo build scenes made is path-only (5 of the 7 WFC demo files). Stubs at the old paths fix all cases (§4.1). Changing the namespace has no effect on loading. Details are on `dgn-m0-verify-uid-move`.
2. **Checked 2026-11-15.** An old copy next to the moved copy (same uid, same class names, other namespace) gives no error, only "UID duplicate detected" warnings. The uid resolves to the old copy, and the global class list holds both. The editor works, but the shared code gets the old types (§4.1). In the same namespace, the duplicate C# types would not compile, so the namespace change is required.
3. **Checked 2026-11-15: not safely today.** `SpatialMenuHook.FindHost` matches any menu button with the host icon, so the second addon hooks into the first one's fallback button. Both number their items from 7300, so one press runs both callbacks. Disabling the button's creator removes the button and the other addon's item. Disabling the other addon removes the first item with its id, which is the wrong one. **Decision:** the fallback button is shared. It carries metadata with its users and their wanted visibility, items get the first free id, the button is visible when any user wants it, and the last user frees it. The hook's public API stays the same. Card `dgn-m0-shared-menu-inspector`.
4. **Checked 2026-11-15.** Two registered `TileSetInspectorPlugin`s show two previews. **Decision:** the preview, its inspector plugin and the bounds gizmo move to `editor_tools_common/Wfc/Editor/`. Each addon registers its own inspector plugin, and all instances join a list kept as metadata on the editor's base control. `_CanHandle` is true only for the first live instance. A prototype showed one preview with two instances, and still one after the first instance was removed. Card `dgn-m0-shared-menu-inspector`.
5. **Checked 2026-11-15, headless and by the board in the editor.** With the existing solver, a hand-built layout of three rooms, a room tile set with a one-sided edge socket and placed doors, and a corridor tile set with straight pieces placed outside the doors give closed rooms. All 6 doors are joined into one network in a single route attempt, and the room cells are excluded from the corridor solve automatically. A symmetric edge socket fails (see §4.2). Corridors can run right along a room's wall, and two corridors can run side by side. The board accepted the rooms and chose Wall Clearance against walls (4.3), and an idea card for corridors side by side.

---

### 11. Research summary (2026-11-15)

- **Constructive room and corridor generators:** BSP, and random placement with a Delaunay triangulation, a minimum spanning tree, re-added edges for loops and A* corridors (TinyKeep, [Vazgriz](https://vazgriz.com/119/procedurally-generated-dungeons/), with stairs in 3D). Guided paths already joins doors nearest first, which acts like the MST and A* steps, so v1 only needs the room graph to decide door sides.
- **Graph and room templates:** [Edgar](https://ondrejnepozitek.github.io/Edgar-Unity/docs/introduction/) places hand-made room templates along a level graph. This is the model for the room stamps idea after v1.
- **Mission grammars and cycles:** [Dormans 2010](https://pcgworkshop.com/archive/dormans2010adventures.pdf) and [Unexplored](https://www.boristhebrave.com/2021/04/10/dungeon-generation-in-unexplored/). This is the model for room roles and lock and key after v1.
- **Layout first, then WFC:** [CG-WFC](https://blog.ptidej.net/content/files/2025/11/_ICSE_GAS_Laurent____Graph_WFC_Procedural_Gen-1_compressed.pdf), [Growing Graph WFC](https://dl.acm.org/doi/full/10.1145/3807895.3807922). This is the approach chosen here.
- **Global constraints in WFC:** [DeBroglie path constraints](https://github.com/BorisTheBrave/DeBroglie/blob/master/docs/articles/path_constraints.md). These fix connectivity but not room shapes. Not needed, because the layout gives both.
- Rejected for v1: a two-level WFC (room archetypes on a coarse grid, then tiles). It needs the same fine-level work, plus a second tile set and its own connectivity handling, and gives uniform grid-aligned rooms.
