# Project Godoodads: Godot Editor Tools

## Sub-project 2: WFC Generator

**Decisions by:** the board (Syndias)
**Status:** 1.0.0 complete (2026-11-09). Milestones M0–M5 were done; the board decided against tile weights and boundary sockets for v1 (M3 review).
**Reference material:** none. The demo tileset is created for this project.

---

### 1. Context

WFC Generator is an editor tool that fills a `GridMap` with modules using wave function collapse (WFC): each cell starts with every tile as an option, and the solver repeatedly picks a cell, collapses it to one tile and removes the options its neighbours can no longer use.

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
| Addon | `addons/wfc_generator/`, namespace `Godoodads.WfcGenerator` |
| Output in v1 | `GridMap` only |
| Authoring | Socket labels per tile face |
| Usage | Editor-only, with `Core/` kept free of editor and engine objects |
| Target size | 100×100 cells across X and Z, 16 layers on Y (160,000 cells). Size is freely configurable |
| Testing | Core unit tests without the engine, gdUnit4 for adapters, demo scenes for manual checks |

#### Layout

```
addons/wfc_generator/
  plugin.cfg, README.md, CHANGELOG.md
  Core/        Grid, directions, sockets, variants, solver. No engine objects
  Resources/   WfcTile, WfcTileSet, WfcSettings
  Adapters/    GridMap read and write, MeshLibrary lookup
  Editor/      Plugin, dialog, socket preview, solve runner, failure overlay
demo/wfc_generator/
tests/wfc_generator/
```

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
- Settings are stored as a `WfcSettings` resource in node metadata, as MultiMesh Scatter does, so a generation can be reproduced from the scene file.

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

Each milestone ends with a board review. Card epics use the prefix `WFC`, for example `WFC M1 Core solver`, to keep them apart from the Scatter milestones.

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
