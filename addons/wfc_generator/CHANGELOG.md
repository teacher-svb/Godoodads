# Changelog

All notable changes to WFC Generator are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

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
