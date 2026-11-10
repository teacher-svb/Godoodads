# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Godot Editor Tools** (working title, "Godoodads") is a suite of independent Godot editor addons in one repo, with one addon folder per sub-project and shared helpers. Sub-project 1 is **MultiMesh Scatter**, which fills a `MultiMeshInstance3D` with instances placed on a surface mesh, driven by geometry, mask textures and normal maps. It was **released as 1.0.0 on 2026-11-01** (all milestones M0–M5 accepted by the board). Its CHANGELOG lists what shipped. Sub-project 2 is **WFC Generator**, which fills a `GridMap` with modules using wave function collapse and socket rules, in true 3D. Its 1.0.0 completed milestones M0–M5 on 2026-11-09; 1.1.0 adds tile weights and regenerate (brief §11), 1.2.0 boundary sockets, height curves, viewport handles, pinning and marker tiles (brief §12), and 1.3.0 guided paths, socket editing and measured tile shares in the preview (brief §13). 1.4 is planned as the 2D `TileMapLayer` adapter (brief §14). New sub-projects get their own brief in `docs/briefs/`, addon folder and milestone epics on the kanban board, and reuse `addons/editor_tools_common/`.

- [docs/briefs/multimesh_scatter.md](docs/briefs/multimesh_scatter.md) is the board's brief for MultiMesh Scatter and the authoritative spec. Read the relevant section before implementing anything. Most important: §4 requirements, §6 reference bugs to avoid, §8 acceptance criteria.
- [docs/briefs/wfc_generator.md](docs/briefs/wfc_generator.md) is the brief for WFC Generator. §10 records the GridMap facts verified in M0, and the kanban cards record the board decisions (no tile weights in v1).
- [CONVENTIONS.md](CONVENTIONS.md) covers layout, namespaces, the UI pattern, testing and work tracking. Follow it.

## Commands

Godot 4.7 .NET binary: `~/Apps/Godot_v4.7-stable_mono_linux_x86_64/Godot_v4.7-stable_mono_linux.x86_64` (also set in `.runsettings`, which is git-ignored and created from `.runsettings.example`).

```bash
dotnet build Godoodads.csproj                                      # build the Godot project (addons + engine tests)
dotnet test tests/Core.Tests                                       # Core unit tests, no engine, ~1 s
dotnet test Godoodads.csproj --settings .runsettings               # gdUnit4 engine tests, headless Godot
dotnet test tests/Core.Tests --filter "FullyQualifiedName~Sampler" # single test or class
$GODOT --headless -e --path . --quit-after 200                     # load the editor headless to catch plugin load errors
$GODOT --headless --path . res://demo/multimesh_scatter/tools/build_demos.tscn  # rebuild the per-mode demo scenes
$GODOT --headless --path . res://demo/wfc_generator/tools/build_demo.tscn       # rebuild the WFC hillside ruin tile set and scenes
```

gdUnit4 versions are pinned as a pair (`gdUnit4.api` 5.1.0-rc5 with `gdUnit4.test.adapter` 3.1.1). Newer adapters need a newer api, so upgrade both together.

## Architecture

- **Core / Resources / Adapters / Editor split** per addon. `Core/` holds pure logic using only GodotSharp value types, and is compiled into `tests/Core.Tests` and tested without the engine. `Resources/` holds engine data types such as `ScatterSettings`. `Adapters/` holds engine glue (Mesh/CSG → `SurfaceData`, `MultiMeshWriter`). `Editor/` holds `#if TOOLS` plugin code.
- **UX:** the plugin adds "Populate Surface (GDDS)" (GDDS = Godoodads) to Godot's **built-in "MultiMesh" toolbar menu** through `SpatialMenuHook`, which finds the menu button by its `MultiMeshInstance3D` icon and falls back to its own button. While enabled, it relabels Godot's own item "Populate Surface (Godot)". The item opens a `ToolDialog`: a node picker header, an embedded `EditorInspector` editing the `ScatterSettings` resource, and a persistent status area. Options are added as exported properties on the resource, not as hand-built controls.
- **Pipeline** (brief §4.6), run in this order: surface data → triangle sampler → slope filter → mask filter → normal source → orientation → randomisation → spacing → write. Stages are independent and combinable.
- **Spacing uses sample elimination** (Yuksel 2015), not dart throwing. It generates about k×Amount candidates and removes the most crowded ones with a spatial grid and a heap until exactly Amount remain. Existing instances are fixed points.
- **Writing:** `Duplicate()` the existing MultiMesh (this keeps its flags), fill one `float[]` (row-major 3×4 per instance) and set `Buffer` once. Wrap it in `EditorUndoRedoManager`.
- **Orientation:** instance up = `slerp(N, V, b_i)`. The arc constructor `new Quaternion(from, to)` is verified to handle opposite vectors.
- **Never modify user assets.** Work on copies (tangents, decompressed textures).
- **Normal maps** use exactly the formulas of Godot's 4.7 forward shader: z is rebuilt from xy, `B = cross(N, T) * sign(w)`, and the result is `normalize(mix(N, TBN·n, strength))`. Instances follow the normal Godot renders. On a PlaneMesh, T = +X and B = −Z.
- **Placeholder tangents:** Godot 4.2+ stores placeholder tangents (`normalize(cross((n.z, −n.x, n.y), n))`, w = 1) for meshes without tangents, and `SurfaceGetArrays` returns them. `SurfaceSource.HasRealTangents` detects them.

## Testing gotchas

- The editor reloads the C# assembly after every rebuild, and editor code must survive that. Use `[Tool]` GodotObjects with parameterless constructors, `[Signal]` rather than C# events, and method-name Callables for editor-owned signals. See CONVENTIONS.md, "Surviving assembly reloads". Headless runs cannot trigger a reload, so the user has to verify it manually.

- **Reload timing:** after a build (also godot-mcp `build_dotnet`), the editor reloads the C# assembly only when its window gains focus. Re-enabling a plugin before that runs the **old** code. Ask the user to focus Godot first, then toggle the plugin.
- `ToolDialog` hides the inspector's inherited rows (Resource, RefCounted, Add Metadata) by walking its internal rows in display order and skipping invisible ones; Godot keeps a hidden Favorites block with a class header of its own. This depends on Godot internals: re-check it after Godot upgrades.
- Headless gdUnit4 runs use a dummy rendering server. `MultiMesh.GetInstanceTransform` returns zeros, so assert on `MultiMesh.Buffer`. Mesh surface arrays and `Image` work.
- Engine test classes live in `tests/<addon>/` and compile into the Godot assembly. `tests/Core.Tests/**` is excluded from it.

## Work tracking (required)

Plan and execute work through the kanban cards in `.devtool/features/` (VS Code extension *Kanban Markdown*), so the user can follow progress:

- Move a card to `in-progress` when starting it. Move it to `review` when it needs a manual editor check, or to `done` when it is verified. Done cards move to `.devtool/features/done/` and get `completedAt` set.
- Always update `modified`. Append results and findings to the card body.
- Create new cards in the existing frontmatter shape. `epic` is the milestone.
- Milestones M0–M5 end with a board review card, which the user moves to `done`. Do not start the next milestone's cards before that review.
- Out of scope for v1 (brief §9): runtime scattering, brush tools, LOD/culling/collision, GDScript/C++ ports, and store publication. Note these ideas in the README.md backlog instead of building them.
