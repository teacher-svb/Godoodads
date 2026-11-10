# Godot Editor Tools (Godoodads)

A suite of independent editor addons for Godot 4.7+ (.NET / C#). Each addon lives in its own folder under `addons/` and uses the shared helpers in `addons/editor_tools_common/`.

## Sub-projects

| Addon | Folder | Status | Brief |
|---|---|---|---|
| MultiMesh Scatter | `addons/multimesh_scatter/` | 1.0.1, see its [README](addons/multimesh_scatter/README.md) | [docs/briefs/multimesh_scatter.md](docs/briefs/multimesh_scatter.md) |
| WFC Generator | `addons/wfc_generator/` | 1.1.0, see its [README](addons/wfc_generator/README.md) | [docs/briefs/wfc_generator.md](docs/briefs/wfc_generator.md) |

## Install an addon

1. Download the addon's latest zip from [Releases](https://github.com/teacher-svb/Godoodads/releases), for example `multimesh_scatter-1.0.1.zip`, and extract it into your project's root folder. Its files go under `addons/`.
2. If your project has no C# solution yet, create one with *Project → Tools → C# → Create C# solution*.
3. Build the C# project (the hammer icon, or `dotnet build`).
4. Enable the addon in *Project → Project Settings → Plugins*.

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

Boundary sockets, multi-cell tiles, marker tiles, guided paths, the 2D adapter and socket editing are idea cards on the kanban board (epic *WFC 2.0*). Not planned yet:

- Runtime (in-game) generation
- Streaming, chunks and infinite grids (needs a brief; would also help hard tile sets at large sizes)
- Guaranteed connected paths and reachability (needs a brief)
- Learning rules from example scenes, and the overlapping WFC model
