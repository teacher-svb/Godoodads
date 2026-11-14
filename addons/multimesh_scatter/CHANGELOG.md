# Changelog

All notable changes to MultiMesh Scatter are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.2.0] - 2026-11-14

### Added
- **Wildcards in the GridMap item filter:** entries in *GridMap → Items* may be patterns with Godot's `String.match` rules, `*` for any run of characters and `?` for one character, so `border_*` covers every border variant of a `MeshLibrary` and excluding `path_*` keeps every path tile clear. An item counts as listed when it matches any entry. Matching is case-sensitive and done once per item, not per cell, so large GridMaps are not slower.
- The status area warns about each item entry that matches no item in the `MeshLibrary`, such as `GridMap item 'bordr_*' matches no item.`, so typos show. With Include and no matching cells, the "No cells … match the GridMap filter and box." error is still shown.

### Changed
- Exact item names keep working: the same settings and seed give the same result as in 1.1.0. Only an entry that contains `*` or `?` itself can now match more than the item of that exact name.

## [1.1.0] - 2026-11-10

### Added
- **GridMap surfaces:** pick a `GridMap` as the surface. Every used cell's item mesh is placed as GridMap draws it, and faces hidden between occupied cells are left out, so nothing is scattered inside stacked tiles. A *GridMap* group, shown only for GridMap surfaces, filters cells by item name (include or exclude) and by a box of cells. The status area reports the cells read and the covered triangles left out.
- **Projected masks:** *Placement Mask → Projection → Top Down* lays a mask over the surface from above, over its area seen from above or the GridMap's cell box, so one mask can cover a whole map. Works for every surface kind.
- Demo: `demo_gridmap.tscn`, grass on a small GridMap with ramps, a paving strip and a projected path mask.

### Changed
- The install instructions describe installing from the release zip.
- Nothing changes for mesh and CSG surfaces: the same settings and seed give the same result as in 1.0.1.

## [1.0.1] - 2026-11-09

### Fixed
- The install instructions now cover creating a C# solution, which projects without C# need before they can build the addon.

## [1.0.0] - 2026-11-01

First release. It replaces the *UpFaceMultiMesh* plugin.

### Added
- **Populate Surface (GDDS)** item in Godot's built-in *MultiMesh* menu in the 3D toolbar, opening a resizable dialog with an embedded inspector and a status area that stays visible after errors.
- Surface picker for `MeshInstance3D` and root CSG nodes. All surfaces are used, indexed or not, measured in world space.
- Area-weighted and uniform-per-triangle sampling.
- Seed with a randomise button; results are reproducible.
- Replace and Append output modes.
- Undo and redo for every populate.
- Settings stored on the node and restored when the dialog opens.
- Slope filter with min and max angle against the target direction or a custom direction.
- Placement mask: channel, UV set, Threshold and Density modes, invert, retries up to an attempt limit, and shortfall reporting.
- Normal source: face, smooth or normal map (tangent or object space, flip green, strength). Tangents are generated on a copy when a mesh has none.
- Orientation blend between the surface normal and a target direction, with per-instance randomness and a selectable mesh up axis.
- Minimum distance through sample elimination, keeping distance from existing and sibling instances.
- Presets: load and save `.tres`, plus four shipped presets ("Upward faces (legacy)", "Follow surface", "Grass on terrain", "Masked scatter").
- Demo scenes for each mode.
- Godot's built-in *Populate Surface* item is labelled "(Godot)" while the plugin is enabled.

### Fixed (compared with UpFaceMultiMesh)
- Triangle areas are measured in world space, so non-uniformly scaled surfaces are sampled evenly.
- Existing MultiMesh settings (`use_colors`, `use_custom_data`) are kept.
- Minimum distance no longer silently places fewer instances; any shortfall is reported.
- Errors are shown in the dialog instead of closing it.
