# Changelog

All notable changes to godooscatter (called MultiMesh Scatter up to 1.2.0) are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.3.1] - 2026-11-16

### Fixed
- **A large Min Distance with a large Amount no longer freezes the editor.** When Min Distance is much wider than the spacing Amount needs, the spacing stage keeps a random subset of the candidates. Amount 100,000 at Min Distance 3 m on a 100 × 100 m plane took 80 s and 12 GB, and around 5 m the editor ran out of memory; it now takes under half a second. When Amount fits at the chosen Min Distance, the same settings and seed give the same result as in 1.3.0. When it doesn't fit, the instances can differ and their number stays about the same.
- **Populate writes to the node the dialog was opened for.** The dialog does not block the editor, and selecting another node while it was open made Populate write to that node instead, or report "The target MultiMeshInstance3D is no longer in the scene." when the new selection was not a MultiMeshInstance3D.
- **Errors during Populate show in the dialog.** Append on a MultiMesh with 2D transforms and instances, and an Amount too large to run, now report in the status area and as a toast instead of only in the Output panel. Nothing is changed.

## [1.3.0] - 2026-11-15

### Changed
- **Renamed to godooscatter.** The addon lives in `addons/godooscatter/`, the plugin is called **godooscatter** and releases are named `godooscatter-<version>`, to match the other Godoodads addons. The same settings and seed give the same result as in 1.2.0.
- Settings stored on MultiMeshInstance3D nodes and saved presets keep loading. Files saved in the editor find `ScatterSettings` by uid; files without one load a small legacy class left at `addons/multimesh_scatter/Resources/`, which ships in the zip.

### Updating
- Delete `addons/multimesh_scatter/` before you extract this version, then build and enable **godooscatter** in the plugin list. Godot drops the old **MultiMesh Scatter** entry because its folder is gone. If both are left installed, the MultiMesh menu shows two **Populate Surface (GDDS)** items.
- The dialog opens at its default size once, because its saved size is stored under the new name.

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
