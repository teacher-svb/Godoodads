# Changelog

All notable changes to MultiMesh Scatter are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

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
