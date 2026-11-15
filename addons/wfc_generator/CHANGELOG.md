# Changelog

All notable changes to WFC Generator are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- **External Sockets** on a tile set: sockets that something outside the tiles supplies, such as a boundary socket or the `outside-` of a godoodungeon room's bounds. The tile set check and the preview count them as partners, so faces that only ever meet them are no longer reported or drawn in red. Generation ignores them; an empty list changes nothing.

## [1.3.3] - 2026-11-15

### Changed
- The solver, the tile set resources (`WfcTile`, `WfcTileSet`, `WfcSettings`), the GridMap adapters, the tile set preview and the bounds handles moved to `addons/editor_tools_common/Wfc/`, so that godoodungeon can share them. Generation is unchanged: the same settings and seed give the same cells.
- Tile sets, settings and scenes from earlier versions keep loading. Files saved in the editor find the moved scripts by uid. Files saved by your own scripts name the old paths, where small legacy classes now stand in.
- The **GridMap** toolbar menu is shared with other Godoodads addons that use it, and a tile set shows one preview however many of them are enabled.

### Updating
- Delete `addons/wfc_generator/` before you extract this version, and let the zip's `editor_tools_common` replace yours. Files left behind from 1.3.2 cause build errors and "UID duplicate detected" warnings.

## [1.3.2] - 2026-11-14

### Added
- **Clear Bounds** button: clears the bounds right away by the rules of Clear Bounds First and Keep Placed Cells, with the markers on cleared cells, as one undo step. Needs no tile set.

### Changed
- Keep Placed Cells is always shown, because it also applies to Clear Bounds.

### Fixed
- With Randomise Seed on, a Generate that fails or is stopped puts Seed back to the seed before it, which still matches the GridMap. A seed that failed is not picked again while the dialog stays open.

## [1.3.1] - 2026-11-14

### Changed
- **Randomise Seed is a checkbox** instead of a button. On, every Generate picks a new seed, shown read-only under Seed. Off, Generate keeps using the seed, which you can type. Off by default, so stored settings behave as in 1.3.0.

### Added
- **Keep Placed Cells**, shown with Clear Bounds First: clear removes every generated cell, pinned ones too, and keeps the cells you placed and generated cells you edited since. Off by default, which clears everything as before.

### Fixed
- A pinned cell freed by Clear Bounds First no longer leaves a stale pin in the regenerate record where the new solve put an empty tile.

## [1.3.0] - 2026-11-10

### Added
- **Guided paths:** set a path socket, place two or more path cells, and Generate lays routes between them before solving, so the path is always connected. Routes branch through T and cross tiles, climb where the tile set has path faces on top, and meander with **Path Wander**. Path tiles appear only on routes. Up to 8 routes are tried when the tiles around one cannot be filled in. Problems are reported with their cells.
- **Socket editing in the preview:** click a face label to edit it, with suggestions from the labels already used and undo. Labels no tile can face are shown in red.
- **Measured tile shares:** the preview solves a sample in the background and shows each tile's real share next to its weight share, flagging big gaps.
- Demo: `hillside_paths` with path tiles in a second tile set, `ruin_paths_tileset`, and path stairs cut into the slope.

### Changed
- Nothing changes for tile sets and scenes without paths: the same seed gives the same result as in 1.2.

## [1.2.0] - 2026-11-10

### Added
- **Boundary sockets:** an optional socket per face of the bounds, as if a fixed neighbour sat outside it. A placed cell that cannot face it is reported with its coordinates.
- **Height curves** on tiles: the weight is multiplied by height, from the bottom layer of the bounds to the top. Layers where nothing can be generated are reported before solving.
- **Handles in the viewport** for the bounds, with undo.
- **Pin box:** pin generated cells so the next Generate keeps them, and unpin them again. Pins are stored in the regenerate record.
- **Marker tiles:** a scene per tile, placed and rotated at every generated cell of that tile in a `WfcMarkers` container, replaced by the next generation and part of the undo step.
- Demo: the ground scene rests on a `rock` boundary socket instead of a painted layer, and tower tops carry a light.

### Changed
- Propagation handles variants with identical neighbour rules together. Results are unchanged, and solving is about 2.5 times faster on the demo.

### Fixed
- Undoing a first generation no longer leaves an empty settings entry on the GridMap.

## [1.1.0] - 2026-11-10

### Added
- **Weight** on each tile: how often the solver chooses it, shared by its rotations. Weight 0 means the tile is never generated, but it can still be placed by hand. Negative weights, and tile sets where every tile has weight 0, are reported as errors.
- The tile set preview shows each tile's weight and its share of the total.
- **Generate replaces the previous result.** The cells the last generation wrote are stored compressed on the GridMap, in the same undo step, and the next Generate fills them again. Cells you placed, and generated cells you changed since, are kept. The status area reports both counts, and mentions tiles of weight 0.

### Changed
- **Results differ from 1.0 for the same seed.** The solver now fills the cell with the least total weight left first, instead of the cell with the fewest options, and chooses in proportion to weight. With equal weights the order is the same as before, but the random choices are not.
- The demo tile set uses weights instead of 80 copies of its air tiles and 12 of its 16 grass variants: 54 variants instead of 144. It looks the same and solves about twice as fast.

## [1.0.0] - 2026-11-09

First release.

### Added
- **Generate WFC (GDDS)** in a **GridMap** menu in the 3D toolbar, opening a dialog with an embedded inspector and a status area that stays visible after errors.
- `WfcTileSet` and `WfcTile` resources: sockets per face, no, half-turn or quarter-turn rotation around Y, and empty (air) tiles. The MeshLibrary is never modified.
- Sockets: symmetric and one-sided side sockets, and any-rotation and rotation-specific top and bottom sockets.
- A solver for 2D and 3D grids: bitset domains, propagation, chronological backtracking and seed-derived restarts. Results are reproducible from the seed.
- Generation in the background with a Stop button, shown as it runs at a chosen speed.
- Cells already in the bounds stay fixed, with a Clear Bounds First option. Cells without a matching tile are left alone and reported.
- Undo and redo for every generation, and settings stored on the GridMap.
- Bounds drawn in the viewport, and failing cells marked in red.
- Failure reports that tell impossible placed cells, impossible tile sets and exhausted attempts apart, with the cells and sockets involved.
- Tile set preview in the inspector: 3D view with socket labels, valid neighbours per face, and a check for sockets without a partner and tiles that only fit at the edges.
- Demo: a stacked hillside ruin tile set with two scenes and a build script.
