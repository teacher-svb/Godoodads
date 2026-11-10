# Godot Editor Tools (Godoodads)

A suite of independent editor addons for Godot 4.7+ (.NET / C#). Each addon lives in its own folder under `addons/` and uses the shared helpers in `addons/editor_tools_common/`.

## Sub-projects

| Addon | Folder | Status | Brief |
|---|---|---|---|
| MultiMesh Scatter | `addons/multimesh_scatter/` | 1.0.1, see its [README](addons/multimesh_scatter/README.md) | [docs/briefs/multimesh_scatter.md](docs/briefs/multimesh_scatter.md) |
| WFC Generator | `addons/wfc_generator/` | 1.2.0, see its [README](addons/wfc_generator/README.md) | [docs/briefs/wfc_generator.md](docs/briefs/wfc_generator.md) |

## Install an addon

The addons need Godot 4.7 or later, the **.NET** edition, and the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or later.

1. **Download** the addon's latest zip from [Releases](https://github.com/teacher-svb/Godoodads/releases), for example `wfc_generator-1.2.0.zip` or `multimesh_scatter-1.0.1.zip`. Each release is named after its addon.
2. **Extract it into your project's root folder**, next to `project.godot`. Every zip contains the addon's folder and `editor_tools_common`, the shared helpers, both under `addons/`. When you install a second Godoodads addon, let the newer `editor_tools_common` replace the older one.
3. **Create a C# solution** if your project has none: *Project → Tools → C# → Create C# solution*.
4. **Build** the project (the hammer icon, or `dotnet build`).
5. **Enable** the addon in *Project → Project Settings → Plugins*.

If the addon is missing from the plugin list, the zip was extracted one folder too deep: the file must be at `addons/<addon>/plugin.cfg`. If enabling it reports "Unable to load addon script", build first and check the MSBuild panel for errors. The [WFC Generator README](addons/wfc_generator/README.md#install) has more fixes and how to update an addon.

## Working on this repository

1. Open this folder in Godot 4.7 .NET and build the project. For the engine tests, run `cp .runsettings.example .runsettings` and set `GODOT_BIN` in it to your Godot binary.
2. Open `demo/multimesh_scatter/demo.tscn` and select the `Scatter` node.
3. In the 3D toolbar, choose **MultiMesh > Populate Surface (GDDS)**. GDDS is short for Godoodads, the name of this suite.

Conventions, layout and test commands: [CONVENTIONS.md](CONVENTIONS.md). Progress is tracked as kanban cards in `.devtool/features/`.

## Backlog (ideas, not planned)

- Runtime (in-game) scattering and a scatter node that regenerates automatically
- Brush or paint-to-scatter tools; painting masks with Godot 4.7's `DrawableTexture2D`
- LOD, culling and collision generation for instances
- GDScript or GDExtension ports
- Publishing on the Godot Asset Store

### WFC Generator

Guided paths, socket editing and measured tile shares are planned for 1.3 (epic *WFC 1.3*), and the 2D `TileMapLayer` adapter for 1.4 (epic *WFC 1.4*). Region weights and multi-cell tiles are idea cards in the epic *WFC 2.0*. Not planned yet:

- Runtime (in-game) generation
- Streaming, chunks and infinite grids (needs a brief; would also help hard tile sets at large sizes)
- Guaranteed connected paths and reachability (needs a brief)
- Learning rules from example scenes, and the overlapping WFC model
- Suggesting sockets by comparing the outlines of mesh faces that meet (experimental; needs a brief)
- Terrain-first paths: generate the terrain, then find a route over it with A* using a cost per tile, and fit path tiles in afterwards (1.3 lays the route out first instead)
- Renaming a socket across a whole tile set, and thumbnails of matching faces in the preview
- Suggesting 2D sockets from Godot terrain sets
