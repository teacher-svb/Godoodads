# Project Godoodads: Godot Editor Tools

## Sub-project 2: godoowave (WFC Generator)

*Called WFC Generator up to 1.3.4, in `addons/wfc_generator/`; renamed to godoowave in 1.4.0 (§15). Sections before §15 use the old name where they record history.*

**Decisions by:** the board (Syndias)
**Status:** 1.0.0 complete (2026-11-09). Milestones M0–M5 were done; the board decided against tile weights and boundary sockets for v1 (M3 review). 1.1.0 (§11) adds per-tile weights, ordering by remaining weight, and regenerate. 1.2.0 (§12) adds boundary sockets, height curves, viewport handles, pinning and marker tiles. 1.4.0 (§15) is the rename to godoowave. 1.5.0 (§16) replaces the pin box with pin box nodes, 1.6.0 (§17) adds several socket labels on one face, and the 2D adapter (§14) follows in 1.7.
**Reference material:** none. The demo tileset is created for this project.

---

### 1. Context

godoowave is an editor tool that fills a `GridMap` with modules using wave function collapse (WFC): each cell starts with every tile as an option, and the solver repeatedly picks a cell, collapses it to one tile and removes the options its neighbours can no longer use.

It follows the suite's conventions (`CONVENTIONS.md`) and reuses `addons/editor_tools_common/`. The priority use case is true 3D generation, including along the vertical axis. The solver is built for 2D and 3D alike, but 1.0 ships with 3D output only.

---

### 2. Goal

Deliver a Godot editor addon that lets a level designer define which modules may connect using sockets, select a `GridMap`, choose bounds, and press **Generate** to watch the bounds fill with a valid arrangement. Cells the designer placed by hand stay in place and the solve works around them.

---

### 3. Technical baseline

| Item | Decision |
|---|---|
| Engine | Godot 4.7 or later, .NET build |
| Language | C# |
| Addon | `addons/godoowave/`, namespace `Godoodads.Wave` |
| Output in v1 | `GridMap` only |
| Authoring | Socket labels per tile face |
| Usage | Editor-only, with `Core/` kept free of editor and engine objects |
| Target size | 100×100 cells across X and Z, 16 layers on Y (160,000 cells). Size is freely configurable |
| Testing | Core unit tests without the engine, gdUnit4 for adapters, demo scenes for manual checks |

#### Layout

```
addons/godoowave/
  plugin.cfg, README.md, CHANGELOG.md
  Core/        Grid, directions, sockets, variants, solver. No engine objects
  Resources/   WfcTile, WfcTileSet, WfcSettings
  Adapters/    GridMap read and write, MeshLibrary lookup
  Editor/      Plugin, dialog, socket preview, solve runner, failure overlay
demo/godoowave/
tests/godoowave/
```

Since 1.3.3, `Core/`, `Resources/` and `Adapters/` live in `addons/editor_tools_common/Wfc/`, shared with godoodungeon (dungeon brief §4.1). `addons/wfc_generator/Resources/` kept empty legacy subclasses for files that name the old script paths, until 1.4.3 (§15).

---

### 4. Functional requirements

#### 4.1 Tile set

- `WfcTileSet` is a `Resource` that references a `MeshLibrary` and lists `WfcTile` entries. The rules live in this resource, because a `MeshLibrary` is the user's asset and must not be modified.
- Each `WfcTile` has a MeshLibrary item (or **Empty**, see 4.3), six socket labels (+X, -X, +Y, -Y, +Z, -Z) and a **rotation mode**: None, 2-way or 4-way around Y.
- Rotated variants are generated automatically. Variants that are identical after rotation are merged.
- Rotation is around Y only in v1. Mirrored variants need their own MeshLibrary item, because GridMap orientations are rotations.

#### 4.2 Socket matching

Two facing sockets connect when their labels match under these rules:

| Face | Written as | Connects to |
|---|---|---|
| Horizontal, symmetric | `path` | `path` |
| Horizontal, asymmetric | `path+` | `path-`, and the reverse |
| Vertical, any rotation | `stair` | `stair` |
| Vertical, rotation-specific | `stair@0` to `stair@3` | The same label with the same rotation index, after the tile's rotation is applied |

An empty label is a validation error. The label that means "open air" is an ordinary socket label chosen by the tileset author.

#### 4.3 Air

Empty space is a normal tile whose item is **Empty**. Writing it clears the cell (`GridMap.InvalidCellItem`).

#### 4.4 Fixed cells

- Every occupied cell inside the bounds is fixed. Its item and orientation are mapped to a variant, and the solve starts by propagating from it.
- Empty cells cannot be fixed, because an empty cell means "free to fill".
- **Clear bounds first** is an option on the dialog. Regenerating a region means choosing bounds and enabling it.
- Occupied cells whose item is not in the tile set, or whose orientation has no matching variant, stay in place and impose no constraint on their neighbours. The status area reports how many there are.
- Fixed cells that already conflict are reported before solving, with their coordinates.

#### 4.5 Boundaries

Edges are unconstrained in v1: a cell at the boundary has no constraint in the outward direction. Tilesets keep ground low and air high through their sockets (for example, air accepts only air above it) or through a hand-placed ground layer.

#### 4.6 Solver

```
Read tile set and fixed cells
  > Build variants and the adjacency table
  > Validate
  > Propagate from fixed cells
  > Loop: pick the cell with the fewest options (ties broken by seed)
          > collapse to a uniformly random remaining variant
          > propagate
  > On contradiction: backtrack, up to a limit, then restart with a seed derived from the original
  > Write to GridMap with undo
```

- Core supports any set of grid directions. 2D (4 directions) and 3D (6 directions) use the same solver code.
- Domains are bitsets. Backtracking uses a trail of changes, not copies of the grid.
- Limits are counted in backtracks and restarts, never in time, so that results stay deterministic.
- There are no weights in v1. Every remaining variant is equally likely.
- **Failure** ends the solve with a report (4.8). Cells placed so far are discarded and the GridMap returns to its earlier state.

#### 4.7 Solve run

- The solve runs on a background thread. GridMap writes happen on the main thread.
- **Animated solve** applies progress in per-frame batches, with a speed control and an off option. Animation never changes the result.
- **Cancel** stops the solve and restores the earlier state.
- Generate is one undo step: undo restores every cell in the bounds, redo reapplies the result.
- A running solve is cancelled before an assembly reload, when the GridMap is removed, and when the scene closes.
- Settings are stored as a `WfcSettings` resource in node metadata, as godooscatter does, so a generation can be reproduced from the scene file.

#### 4.8 Failure reporting

- Report the failing cell's coordinates, the neighbours constraining it, and the sockets involved.
- Highlight the failing cell in the 3D viewport with an overlay that is never saved into the scene.
- Distinguish "the fixed cells make this impossible" from "the solver ran out of attempts" wherever the solver can prove the former.

---

### 5. UI requirements

- A toolbar entry appears when a `GridMap` is selected and opens a `ToolDialog`, using `SpatialMenuHook`, which falls back to the addon's own button because no built-in GridMap menu exists (see section 10).
- The dialog holds tile set, bounds (origin and size), seed with a randomise button, clear bounds first, animation speed, and backtrack and restart limits. Options are exported properties on `WfcSettings`.
- The status area shows progress, then a result such as: `Solved 160,000 cells in 12.4 s (3 backtracks, 0 restarts). Seed 4821.`
- Bounds are drawn in the viewport while the dialog is open.
- **Socket preview:** an `EditorInspectorPlugin` for `WfcTileSet` shows the selected tile in a 3D preview with labelled faces and a "show valid neighbours" list per face.
- **Tile set check:** reports sockets with no possible partner and tiles that can never be placed. It runs in the inspector and before every solve.

---

### 6. Known pitfalls

1. WFC only enforces local rules. It cannot guarantee reachability or that structures are supported.
2. Without weights, a tile with four rotations is about four times as likely as a tile with one, so results lean towards tiles with many rotations. Section 7 includes a board check for this.
3. Unconstrained edges cut structures off at the boundary. This is accepted for v1.
4. Rotating sockets is easy to get backwards relative to GridMap's orientation. An engine test must compare the Core rotation with the actual `Basis` for every orientation used.
5. A GridMap write can trigger a mesh rebuild, so animation must batch writes.
6. A background thread that outlives an assembly reload can crash or hang the editor. See 4.7.
7. Backtracking at 160,000 cells can use a lot of memory if it stores grid copies. See 4.6.

---

### 7. Milestones

Each milestone ends with a board review. Cards use the epic `godoowave` (`WFC Generator` until 1.3.4) with the labels `v1.0.0` and the milestone, for example `m1`.

| # | Milestone | Content |
|---|---|---|
| M0 | Foundations | Addon skeleton, plugin load, verifying the section 10 assumptions |
| M1 | Core solver | Directions, sockets, variants, adjacency, propagation, backtracking, restarts, determinism, 2D and 3D tests, performance baseline |
| M2 | GridMap pipeline | Resources, adapters, fixed cells, dialog, background solve, cancel, undo, settings persistence |
| M3 | Demo tileset | Script-built stacked hillside ruin MeshLibrary and socket set, demo scenes. **Board check on result quality: decide whether weights or boundary sockets move into v1** |
| M4 | Authoring tools | Socket preview, valid neighbours, tile set check |
| M5 | Animation and release | Animated solve, failure overlay, addon README with screenshots, CHANGELOG, version 1.0.0 |

The demo tileset covers ground, walls, floors, stairs, ramps, bridges and air, using simple generated meshes. It includes one scene with no fixed cells and one with a hand-placed ground layer.

---

### 8. Acceptance criteria

Automated unless noted:

- **Determinism:** same seed, settings and fixed cells produce identical output, with animation on or off.
- **Validity:** in every completed solve, every pair of adjacent cells has matching sockets, in 2D and in 3D.
- **Vertical rules:** a rotation-specific vertical socket only connects at the matching rotation.
- **Rotation:** for every tile rotation, each Core socket direction matches the direction given by the GridMap orientation's `Basis`.
- **Fixed cells:** every fixed cell is unchanged after a solve.
- **Unsatisfiable input:** conflicting fixed cells are reported with their coordinates and no writes happen.
- **Contradiction recovery:** a tile set that causes contradictions still solves through backtracking.
- **Unknown cells:** an occupied cell with an unknown item is left alone and counted in the status.
- **User assets:** the MeshLibrary is unchanged after generation.
- **Undo:** undo restores every cell in the bounds; redo reapplies the result. (Manual check.)
- **Cancel:** cancelling restores the earlier state. (Manual check.)
- **Reload:** rebuilding during a solve neither crashes nor hangs the editor. (Manual check.)
- **Performance:** 100×100×16 with the demo tileset and animation off completes in under 30 seconds on the board's dev machine. (Manual check, report timings. Target to be confirmed after M1.)
- **Reliability:** at least 19 of 20 seeds complete on the demo tileset at target size.

---

### 9. Out of scope for v1

- Weights, including per-region and per-height weights.
- Runtime (in-game) generation.
- Streaming, chunks and infinite grids.
- Connected-path and reachability constraints.
- A 2D `TileMapLayer` adapter.
- A full in-viewport socket editor.
- Configurable boundary sockets.
- Multi-cell tiles.
- Marker tiles that instantiate scenes (doors, spawn points, lights).
- The overlapping WFC model and learning rules from examples.

Note further ideas in the repo's `README.md` backlog rather than building them.

---

### 10. Assumptions verified in M0

Checked on Godot 4.7. Details are on the M0 cards.

1. `MeshLibrary` has no per-item custom data, so `WfcTileSet` holds the rules. Confirmed.
2. GridMap's 24 orthogonal orientations are rotations only. Confirmed. `GetOrthogonalIndexFromBasis` silently returns 0 for a mirrored basis, so never pass one.
3. Y rotations of 0, 90, 180 and 270 degrees are orientation indices 0, 16, 10 and 22. Confirmed.
4. There is no built-in GridMap menu in the 3D toolbar, so the addon adds its own button.
5. The cost of `SetCellItem` calls is negligible (160,000 in 47 ms), but real mesh rebuild cost needs a real renderer. It is measured in M5.
6. Stopping a background thread before an assembly reload uses `ISerializationListener.OnBeforeSerialize`. Confirmed in the editor.

---

### 11. Release 1.1

Decided by the board on 2026-11-10. Tracked under the label `v1.1.0`.

#### 11.1 Per-tile weights

- `WfcTile` gets a **Weight** (default 1). A tile's weight is **shared by its rotations**: each of its variants gets weight ÷ variant count, so a 4-way ramp with weight 1 is as likely as grass with weight 1.
- **Weight 0** means the solver never chooses the tile, but cells you place yourself may still use it. A negative weight, or a tile set where every tile has weight 0, is a validation error.
- ~~The solver still collapses the cell with the fewest remaining options.~~ **Changed on 2026-11-10:** the solver collapses the cell with the **least remaining weight** (the sum of its options' weights, bucketed to about 1%). With equal weights this is the fewest-options order. Weights decide both the cell order and the variant chosen there, so a tile of weight 3 acts like three copies of weight 1. Reason: without the air copies, the fewest-options order turned the demo into tall rock plateaus (rock 87k cells instead of 16.5k at 100×16×100), because cells at the edge of the air were resolved before the terrain below. Weights alone could not fix that. Measurements are on the card `wfc-1-1-demo-weights`.
- Same seed, same result still holds within 1.1. Results **differ from 1.0** for the same tile set and seed. The CHANGELOG says so, and the release stays a minor version (1.1.0).
- The demo drops its copies of the air tiles and uses weights instead.

#### 11.2 Regenerate without undoing

- **Generate replaces the previous result by default.** Cells written by the last generation are free again. Hand-placed cells stay fixed. There is no new option, and Clear Bounds First still removes everything.
- A generated cell whose item or orientation no longer matches what was written counts as **hand-placed**. Erased cells are simply empty.
- What was written is stored on the GridMap in metadata, compressed, and changed in the same undo action as the cells.

#### 11.3 Not in 1.1

Boundary sockets stay in the backlog. The Copies field is dropped, because weights replace it.

---

### 12. Release 1.2

Decided by the board on 2026-11-10. Tracked under the label `v1.2.0`. One release with all items below, follow-ups first. Everything is additive: tile sets and scenes from 1.1 keep working and give the same result when the new options are unused.

#### 12.1 Boundary sockets

- `WfcSettings` gets six optional socket fields, one per bounds face. Empty keeps the face unconstrained, as in 1.0 and 1.1.
- The solver treats the outside of a face as a fixed neighbour with that socket. Edge cells lose the options that do not match before the first choice. Labels follow the normal syntax; the top and bottom faces use vertical sockets.
- A placed cell that cannot face a boundary socket is reported like a conflict between placed cells, with its coordinates.

#### 12.2 Per-height weights

- `WfcTile` gets an optional **Height Curve**. Its value at the cell's relative height (0 = bottom layer of the bounds, 1 = top layer) multiplies the tile's weight. No curve means a multiplier of 1.
- A multiplier of 0 at a height means the tile is never generated there, but may be placed by hand. Negative curve values are errors, and so is a layer where every tile ends up at weight 0.
- Weights differ per layer for both the choice and the remaining-weight cell order. Results stay deterministic.

#### 12.3 Bounds and pin box in the viewport

- While the dialog is open, the bounds box has handles in the 3D viewport, snapped to cells. Drags go through undo and update the dialog.
- A second **pin box**, stored in `WfcSettings` next to the bounds and drawn in its own colour, with the same handles. **Pin** turns the generated cells inside it into hand-placed cells (they leave the regenerate record), so a regenerate keeps them. **Unpin** puts unchanged cells back. Each is one undo action.

#### 12.4 Marker tiles

- `WfcTile` gets an optional **Marker** scene. After a successful solve, it is instanced at each generated cell of that tile, with the cell's transform and rotation, in the same undo action.
- Markers live in a `WfcMarkers` Node3D child of the GridMap, owned by the edited scene so they are saved. Empty tiles (Item -1) may have markers, for example spawn points.
- Markers are tagged with metadata. Generate always removes the tagged markers on the cells it regenerates, even ones the user moved or edited (a marker belongs to the cell it is in now). Markers on cells that stay (hand-placed, edited or pinned) stay with them. To keep a marker, move it out of the container. Markers are not placed for hand-placed cells.

#### 12.5 Follow-ups from 1.1

- Undoing a first generation must not leave an empty settings metadata entry.
- The synthetic Core performance sets get back within 10% of 1.0, or the board accepts the gap.

#### 12.6 Demo

- `hillside_ground` uses a `rock` bottom boundary socket instead of a painted rock layer.
- Some tiles get markers, for example a light on tower tops.

#### 12.7 Not in 1.2

Region weight masks, measured tile shares, multi-cell tiles, guided paths, the 2D adapter and socket editing stay in *WFC 2.0*.

---

### 13. Release 1.3

Decided by the board on 2026-11-10. Tracked under the label `v1.3.0`. One release, in this order: socket editing and measured shares, then guided paths. The 2D adapter, first planned here, became its own release on 2026-11-10 (§14). Everything is additive: 1.2 tile sets and scenes keep working and give the same result when the new options are unused.

#### 13.1 Socket editing in the preview

- Clicking a face label in the tile set preview edits that socket in place. A suggestion list shows the labels already used in the tile set, including the partner of one-sided labels (`path+` suggests `path-`).
- The edit applies to the tile's own face, also when the preview shows a rotated variant, and goes through `EditorUndoRedoManager` as a change of the `WfcTile` property.
- A face label is drawn in red when no variant of any tile can face it.
- Not in 1.3: renaming a label across the tile set, and thumbnails of matching faces.

#### 13.2 Measured tile shares

- The preview gets a **Measure** button with a sample size (default 16×8×16 in 3D and 32×32 in 2D) and a seed. It runs one solve in the background with the same `SolveJob` and cancellation as Generate; changing the tile set cancels it and clears the result.
- Each tile shows "weight share / measured share", flagged when one is more than twice the other. Tiles with a height curve also show their measured share in the bottom, middle and top third of the sample.
- A sample that cannot be solved shows the failure message instead.

#### 13.3 Guided paths

- `WfcSettings` gets a **Paths** group: **Path Socket**, a socket name such as `path` (empty turns paths off), and **Path Wander**, 0 to 1 (default 0.3).
- A variant's **path faces** are its faces whose socket has the path socket's name, ignoring a `+`/`-` suffix. Path faces may be vertical, which is how a path climbs.
- **Endpoints** are the placed cells in the bounds (hand-placed or pinned) that have at least one path face. Placed path cells that face each other through path faces form one group. All groups are joined into one connected network: each next group, nearest first, routes to the network built so far. Two placed path cells give one path; three or more give branches. Every free path face of a placed cell (one that does not face another placed path cell) must end up on a route, so an end tile has one route and a straight tile has two.
- The **route** runs from cell to cell through faces, inside the bounds. A route cell must end up with path faces exactly towards its route neighbours, so a step is only allowed when the tile set has a path variant with that pair of faces (or, where a route joins another one, the larger set, such as a T). That is how climbing is learned from the tile set: a path through stairs needs, for example, a stairs variant with path faces on its low side and top, and a variant above it with path faces on its bottom and high side.
- The route is the cheapest one over seeded random cell costs. Path Wander 0 gives the shortest route, and higher values meander. Same seed, same route.
- The route does not place tiles. It restricts each route cell to the path variants with exactly its path faces, and every other free cell to variants without a path face, so path tiles only appear on routes. The normal solve then picks everything, so the result is always valid. A path face out of the bounds counts as connected, so a path can enter from outside.
- Errors before solving: a path socket that no tile uses, a group of placed path cells with no free path face while other groups need joining, and free path faces that cannot be connected (with their coordinates). A route that the solve cannot fill is reported like any other failure.
- A route can leave the tiles around it no way to fit (for example a climb next to a hill edge). When the solve then proves impossible, up to 8 routes are tried, each from a seed derived from the Generate seed, so the same seed still gives the same result. Ties between equally short routes are also broken by the seed.
- Route cells are generated cells: regenerate routes again, and Pin keeps them.
- Decided on 2026-11-10: the route is laid out before the terrain, not found over a generated terrain with A*. Route-first is always valid; terrain-first goes to the README backlog.

#### 13.4 Demo

- The hillside ruin gets path tiles (straight, corner, T, cross, end, and path stairs with the cell above them) in a second tile set, `ruin_paths_tileset`, so the existing scenes do not change, and a `hillside_paths` scene with two placed path ends on different heights. The path stairs are cut into a ramp (ramp sockets on their sides), because stairs with open sides cannot meet the edge of a hill.

#### 13.5 Not in 1.3

The 2D adapter moves to 1.4 (§14), later 1.5 (§15). Region weight masks and multi-cell tiles stay in *WFC 2.0*. Renaming sockets across a tile set, matching-face thumbnails and suggesting sockets from terrain sets go to the README backlog.

#### 13.6 Patch 1.3.1

Requested on 2026-11-14, tracked under the label `v1.3.1`, before 1.4 work starts. Both options default to off, so settings stored by 1.3.0 behave as before.

- **Randomise Seed** is a checkbox instead of a button. On: the Seed row is read-only and Generate picks a new seed first; the stored settings and status line show it. Off: the seed is editable and Generate keeps using it.
- **Keep Placed Cells**, shown only while Clear Bounds First is on (amends §11.2, "Clear Bounds First still removes everything"): clear frees every generated cell, pinned ones too, and keeps hand-placed cells and generated cells edited since.

#### 13.7 Patch 1.3.2

Requested on 2026-11-14, tracked under the label `v1.3.2`.

- **Failed seed:** with Randomise Seed on, a Generate that does not succeed (no solution, out of attempts, path errors, solver error, stopped or cancelled) puts Seed back to the seed before it, since the GridMap was not changed. Seeds that failed are not picked again while the dialog stays open.
- **Clear Bounds** button: clears the bounds by the rules of Clear Bounds First and Keep Placed Cells, without solving, as one undo step with the regenerate record (entries in the bounds go, pins too) and the tagged markers on cleared cells. Needs no tile set. Keep Placed Cells is therefore always shown.

#### 13.8 Patches 1.3.3 and 1.3.4

Both came with godoodungeon (dungeon brief) on 2026-11-15, tracked under the labels `v1.3.3` and `v1.3.4`.

- **1.3.3:** the solver, resources, adapters, the tile set preview and the bounds gizmo moved to `addons/editor_tools_common/Wfc/`, with legacy classes at the old resource paths. The GridMap menu and the tile set preview are shared with godoodungeon. Generation is unchanged.
- **1.3.4: External Sockets** on `WfcTileSet`: sockets that something outside the tiles supplies (a boundary socket, or the `outside-` of a godoodungeon room's bounds). The tile set check, its edge-only analysis and the preview's red faces count them as partners. Generation ignores them, and an empty list changes nothing.

---

### 14. Release 1.7

Decided by the board on 2026-11-10: the 2D adapter, planned in §13, is its own release after 1.3. It was planned as 1.4, moved to 1.5 on 2026-11-15, when 1.4.0 became the rename (§15), to 1.6 on 2026-11-20, when 1.5.0 became pin box nodes (§16), and to 1.7 on 2026-11-24, when 1.6.0 became socket lists (§17). Tracked under the label `v1.7.0`. Everything is additive: 1.5 tile sets and scenes keep working unchanged.

#### 14.1 2D TileMapLayer adapter

- New resources **`WfcTile2D`** (atlas source id, atlas coordinates, alternative tile, sockets on the four edges, Rotation, **Mirror**, Weight, Marker) and **`WfcTileSet2D`** (a `TileSet` and the tiles). The 3D resources do not change. Core is shared, and the 2D grid is the existing X/Z grid with +Z = down.
- **Mirror** (none, horizontal, vertical, both) adds mirrored variants next to the rotations. Mirroring swaps `+` and `-` on every one-sided socket. Rotations and mirrors are written as the `TileMapLayer` flip and transpose flags.
- Selecting a `TileMapLayer` shows a **Generate WFC (GDDS)** button in the 2D editor toolbar. It opens the same dialog, which shows only the settings that apply in 2D (no height, no top and bottom boundary, no height curves).
- Everything else works as in 3D: fixed cells, unknown cells, undo, cancel, failure messages, weights, the generated record and regenerate, pin box nodes (`WfcPinBox2D`, a `Node2D` child of the `TileMapLayer`, §16), bounds and pin box handles in the 2D viewport, boundary sockets per edge, markers (Node2D scenes in a `WfcMarkers` child) and guided paths (§13.3).
- The inspector shows a 2D tile set preview with the same parts as the 3D one, including socket editing and measured shares.
- The facts this relies on (flip and transpose flags, metadata on `TileMapLayer`, the 2D toolbar and viewport input) are checked first and recorded on the first 2D card.

#### 14.2 Demo

- A new top-down 2D demo. Its atlas is drawn by the demo builder. It has one-sided shore edges, mirrored tiles, a path, and a marker.

#### 14.3 Not in 1.7

Suggesting sockets from Godot terrain sets stays in the README backlog. Region weight masks and multi-cell tiles stay in *WFC 2.0*.

---

### 15. Release 1.4: rename to godoowave

Requested on 2026-11-15, to match the `godoo<name>` scheme of godoodungeon. Tracked under the label `v1.4.0`. Nothing else changes: External Sockets already shipped in 1.3.4 (§13.8). Generation does not change: the same settings and seed give the same cells.

- The addon moves from `addons/wfc_generator/` to `addons/godoowave/`, `demo/` and `tests/` likewise. The plugin is called **godoowave**, release tags are `godoowave-v<version>`, and namespaces are `Godoodads.Wave` (was `Godoodads.WfcGenerator`). Class names stay, because saved files name them as `script_class`.
- `addons/wfc_generator/Resources/` stays with the legacy classes added in 1.3.3 (dungeon brief §4.1), now in `Godoodads.Wave.Legacy`, and ships in the godoowave zip (`tools/package_addon.sh`), so files that name the old script paths still load. The `wfc_1_3_2` test fixture keeps checking this.
- godoodungeon keeps checking `addons/wfc_generator/plugin.cfg` for versions before 1.3.3, and its warning names godoowave as the replacement.
- The dialog size is stored under `godoowave_dialog_size`, so the dialog opens at its default size once.
- Updating needs the old folder deleted and **godoowave** enabled in the plugin list (README, CHANGELOG).
- **1.4.3** (2026-11-16): the legacy classes in `addons/wfc_generator/Resources/` are removed, with their folder in the zip. The only project using the addon had no file that needed them, and release downloads showed no other users. Files without a uid that name the old paths need their paths changed (README, CHANGELOG). The `wfc_1_3_2` fixture now names the moved scripts and keeps checking that 1.3.2 data generates the same cells.

---

### 16. Release 1.5: pin box nodes

Requested by the board on 2026-11-20. The single pin box in `WfcSettings` and its Pin and Unpin buttons (§12.3) are replaced by **pin box nodes**: children of the GridMap, as many as you like, whose cells Generate and Clear never change. Tracked under the label `v1.5.0`. The 2D adapter moves to 1.6 (§14).

Decisions (board, 2026-11-20):
- A pin box protects everything inside it: occupied cells stay and constrain their neighbours, and **empty cells stay empty**.
- **No migration.** The old pin box and the pin flags in the generated record are dropped. Cells pinned with 1.2–1.4 are ordinary generated cells again, so the next Generate replaces them unless a pin box node covers them. CHANGELOG and README say so.
- **Clear never touches pin boxes:** Clear Bounds and Clear Bounds First keep the cells in every enabled pin box, with or without Keep Placed Cells. To free them, disable or delete the box.

#### 16.1 The pin box node

- New node **`WfcPinBox`** (`[Tool, GlobalClass]`, a `Node3D`) in `addons/godoowave/Nodes/`, outside `#if TOOLS`, since scenes save it. It has no runtime behaviour.
- **Shared base** (decided 2026-11-20, with godoodungeon room boxes, dungeon brief §13): Origin, Size, Enabled, the transform sync and the configuration warning live in a base node class **`GridMapBox`** in `addons/editor_tools_common/Nodes/` (namespace `Godoodads.Common`, not a `[GlobalClass]`, so it is not offered in *Add Child Node*), with a shared gizmo plugin that draws and drags any `GridMapBox` and takes its colour from the subclass. `WfcPinBox` and `DungeonRoomBox` derive from it. Settling the script paths now avoids legacy classes later (CONVENTIONS.md).
- Exports: **Origin** and **Size** (`Vector3I`, GridMap cells, like the bounds) and **Enabled** (default on). Only direct children of the GridMap count. A disabled box, or one with a zero size, does nothing. Boxes may overlap each other and stick out of the bounds; only their cells inside the bounds matter.
- The node's position follows Origin and Size: it sits at the centre of the box's floor (centred on X and Z, on the bottom face on Y; changed from the minimum corner in godoowave 1.5.1 and godoodungeon 1.2.1, card `dgn-1-2-1-box-pivot-centre`), so it sits where it shows. Moving it keeps Size. Moving the node with the move tool snaps Origin to the nearest cell; rotation and scale are ignored and reset. If the transform cannot be kept in sync reliably (checked first, 16.6), the transform is locked and only the handles move the box.
- The dialog's **Pin Boxes** group has an **Add Pin Box** button: it adds a `WfcPinBox` child owned by the scene, centred in the bounds at half their X and Z size and their full height, selects it, and is one undo step. Adding the node with *Add Child Node* works too (Origin 0, Size 4 × 1 × 4).

#### 16.2 Generate and Clear

- Every bounds cell inside an enabled pin box is out of the solve. An occupied cell with a variant is fixed (as hand-placed cells are), an occupied cell without one is excluded (as today), and an empty cell is **excluded**: it stays empty and puts no constraint on its neighbours, as excluded cells do in godoodungeon's corridor solve.
- Generated cells inside a pin box stay in the generated record as generated, so after the box is disabled, moved or deleted, the next Generate replaces them. Edited and hand-placed cells inside behave as outside.
- Clear Bounds, and Clear Bounds First before a solve, keep every cell in an enabled pin box, and their record entries.
- Guided paths (§13.3): path cells in a pin box are placed cells, so they are endpoints, as pinned cells were.
- Markers (§12.4) on cells in a pin box stay.
- The status reports the cells kept by pin boxes ("312 cells in 3 pin boxes kept").
- Determinism: the same settings, seed, placed cells and pin boxes give the same cells.

#### 16.3 Handles and outlines

- **GridMap selected, dialog open:** the bounds handles, plus the outline (in the pin colour) and six face handles of every enabled pin box child, numbered after the bounds in child order. A drag is one undo step on that `WfcPinBox` node (Origin and Size), not on the settings. During a solve there are no handles, as today.
- **A pin box selected**, dialog open or not: a gizmo plugin for `WfcPinBox` (its own entry in the viewport's gizmo menu, *godoowave pin boxes*) shows that box's six face handles only, snapped to cells, with Godot's own handle API (`_SetHandle`, `_CommitHandle`): the GridMap editor does not take input while a child is selected. The gizmo draws the outline of every pin box, selected or not, so pinned areas stay visible; the gizmo menu hides them.
- A pin box whose parent is not a GridMap draws a warning (`_GetConfigurationWarnings`) and no handles.

#### 16.4 Removed

- `WfcSettings.PinOrigin`, `PinSize`, *Place Around Bounds*, *Pin Generated Cells*, *Unpin* and the `PinRequested` signal; box 1 of `SetBox`/`BoxOrigin`/`BoxSize`.
- `GridMapCells.Pin`, `GeneratedRecord.SetPinnedInside` and `IsPinned`, the `clearPins` and `keepPinned` parameters and the `Pinned`/`ClearedPins` counts. The record format keeps its pinned bit for reading old records, which is ignored, and writes it as 0, so godoodungeon records (which never set it) are unchanged.
- Saved settings from 1.4 with `PinOrigin` and `PinSize` load without errors (checked first, 16.6; a fixture from 1.4.x keeps checking it).

#### 16.5 Shared code

- `editor_tools_common` is replaced by the newest copy when two addons are installed, so `BoundsGizmoPlugin` and `IBoundsBoxes` keep their API: godoodungeon 1.x keeps working with the 1.5 copy. Changes to `GridMapCells` and `GeneratedRecord` keep the methods godoodungeon calls.
- `GridMapBox` and its gizmo plugin are new shared code that godoodungeon 1.2 builds on (room boxes). They keep their API from 1.5 on.
- godoodungeon does not read pin boxes (its hand-placed cells already cover this; room boxes come in godoodungeon 1.2).

#### 16.6 Facts checked first

On the first 1.5 card, in the editor:
1. Gizmo handles on a selected `WfcPinBox` child of a GridMap can be dragged (the GridMap editor does not take the input).
2. Keeping the transform in sync with Origin (`NotificationTransformChanged` in a `[Tool]` node, snapping, undo of a move) works, or the lock fallback is chosen.
3. A 1.4 `WfcSettings` with `PinOrigin`/`PinSize` and a record with pinned bits load and generate without errors.
4. The node survives an assembly reload (`[Tool]`, parameterless constructor, no C# events; CONVENTIONS.md).

**Checked 2026-11-20 (card `wfc-1-5-pinbox-facts`), all true.** Handles on a selected pin box drag and undo with the dialog open or closed. The transform stays in sync with Origin (move tool, undo of a move, rotation and scale reset), so the node is **not** locked. Settings with properties the class no longer has load without errors. The node survives an assembly reload and a save and reopen.

#### 16.7 Demo

- The hillside ruin demo gets two pin boxes, one around the ruin and one around an empty clearing, built by `demo/godoowave/tools/build_demo.tscn`.

#### 16.8 Not in 1.5

Pin boxes in godoodungeon, non-box pin shapes, rotated pin boxes, and pin boxes in 2D (they come with the 2D adapter in 1.7).

---

### 17. Release 1.6: several socket labels on one face

Decided by the board on 2026-11-24 (card `wfc-1-6-socket-lists`). Tracked under the label `v1.6.0`; the 2D adapter moves to 1.7 (§14). Lives in the shared solver, so godoodungeon gets it too.

- A face takes one or more labels separated by commas: `wall, window` and `wall,window` are the same. Spaces around the commas are ignored, and the list is shown as `wall, window`.
- Two faces fit when any label on one matches any label on the other, by the rules of single labels (`a`/`a`, `a+`/`a-`, `a`/`a` on top and bottom, `a@n`/`a@n`). Kinds may be mixed in one list (`door+, wall`). Rotation turns each `@n` label, and the 1.7 Mirror (§14.1) will swap `+`/`-` on each label.
- Empty entries (`wall,,window`, a trailing comma) and a label listed twice are errors on that face.
- A face with one label behaves exactly as in 1.5, so existing tile sets give the same cells for the same settings and seed. Commas were not allowed in labels before, so no label changes meaning.
- A path face is a face with any label of the path socket's name. Boundary sockets, External Sockets, Room Edge Socket and Path Socket stay single labels; they meet a face when any of its labels matches.
- The inspector's socket fields show the placeholder *label, or labels separated by commas*. The preview's edit popup shows the same placeholder and a hint with the rule. Its suggestions filter on the entry after the last comma, and a picked suggestion replaces that entry, so `wall, ` plus `window` gives `wall, window`.
