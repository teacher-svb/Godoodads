# Changelog

All notable changes to WFC Generator are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

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
