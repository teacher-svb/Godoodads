---
id: "gdd-rename-godoowave-godooscatter-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Godoodads"
dueDate: null
created: "2026-11-15T20:11:45.000Z"
modified: "2026-11-15T20:25:41.000Z"
completedAt: "2026-11-15T20:25:41.000Z"
labels: ["v1.4.0", "v1.3.0", "infra", "release"]
order: "a4"
---
# Rename addons: wfc_generator → godoowave, multimesh_scatter → godooscatter

Requested by the user on 2026-11-15, to match the `godoo<name>` scheme of godoodungeon.

## Decisions (user, 2026-11-15)
- **Full rename**, like godoodungeon: folders (`addons/`, `demo/`, `tests/`), plugin.cfg names, tag prefixes (`godoowave-v*`, `godooscatter-v*`), namespaces (`Godoodads.WfcGenerator` → `Godoodads.Wave`, `Godoodads.MultiMeshScatter` → `Godoodads.Scatter`), briefs, READMEs and kanban epics. C# class names stay (`WfcSettings`, `ScatterSettings`, the plugin classes), because user files store `script_class`.
- **Shim folders** keep old path-only files loading: `addons/wfc_generator/Resources/` (the existing legacy classes) and a new `addons/multimesh_scatter/Resources/ScatterSettings.cs`, shipped in the new zips. See CONVENTIONS.md, legacy subclasses.
- **Minor versions:** godoowave 1.4.0, godooscatter 1.3.0. The planned 2D `TileMapLayer` adapter moves from 1.4 to 1.5.
- Done cards keep their history text; only their `epic` field changes.

## Tasks
- [x] Move folders with `git mv`, keep `.uid` files with their scripts
- [x] Shim for `ScatterSettings` at the old path
- [x] Namespaces, `res://` paths, plugin.cfg, dialog-size setting keys
- [x] Packaging script ships the shim folders
- [x] godoodungeon: dependency check and links (Unreleased)
- [x] Docs: CLAUDE.md, CONVENTIONS.md, README.md, addon READMEs, CHANGELOGs, briefs
- [x] Kanban: epics, 2D adapter cards to `v1.5.0`
- [x] Build, Core tests, engine tests, headless editor load, package + smoke test both zips

## Results (2026-11-15)
- Folders moved with `git mv`, every script keeps its `.uid`. `addons/wfc_generator/` keeps only `Resources/` (legacy classes, now `Godoodads.Wave.Legacy`). New shim `addons/multimesh_scatter/Resources/ScatterSettings.cs` (`Godoodads.Scatter.Legacy`, uid `uid://dtyne4qwqdouy`, real class keeps `uid://ch36n85vf88pf`).
- `tools/package_addon.sh` ships the old folder with each renamed addon (legacy list). The release workflow needs no change: the tag prefix is the folder name.
- godoodungeon still checks `addons/wfc_generator/plugin.cfg` (only versions before 1.3.3 warn); message now names godoowave. In its CHANGELOG under Unreleased.
- Persisted meta keys were already suite-wide (`godoodads_*`), so stored settings and generated records are unaffected. Dialog-size keys renamed (`godoowave_dialog_size`, `godooscatter_dialog_size`).
- Historical text kept: CHANGELOG entries before the rename, done card bodies (only `epic` changed), dungeon brief M0 paths (note added in its status line).
- New engine test `PresetTests.Settings_saved_at_the_old_path_load`: path-only file loads the legacy subclass, uid file loads the real class.

## Verification
- `dotnet build`: 0 warnings, 0 errors. Core tests 366/366. Engine tests 105/105 (twice the adapter's Godot rebuild step exited 139 before the run, then passed with no change; the log shows "Failed to bind socket", probably another Godot instance).
- Headless editor load of this project: no errors (only the known missing `godot_mcp` addon warning). Demo PNGs reimported, `.import` files rewritten for the new paths.
- Packaged `godoowave-1.4.0`, `godooscatter-1.3.0` and `godoodungeon-1.0.0` from a temporary commit; each smoke test in a clean project loaded cleanly. Zips contain the shim folders.
- Manual check left (release review cards): plugin list and menus in a focused editor after the assembly reload.

## Board review (2026-11-15T20:25:41.000Z)
Checked in the editor and approved by the board.
