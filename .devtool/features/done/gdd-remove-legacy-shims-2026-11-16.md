---
id: "gdd-remove-legacy-shims-2026-11-16"
status: "done"
priority: "low"
assignee: "claude"
epic: "Godoodads"
dueDate: null
created: "2026-11-16T16:20:56.000Z"
modified: "2026-11-16T16:36:00.000Z"
completedAt: "2026-11-16T16:36:00.000Z"
labels: ["v1.4.3", "v1.3.2", "resources", "infra", "release"]
order: "a5"
---
# godoowave 1.4.3 / godooscatter 1.3.2: Remove the legacy shim folders

Decided by the user on 2026-11-16: keep `addons/wfc_generator/Resources/` and `addons/multimesh_scatter/Resources/` until the next major release of godoowave and godooscatter, then remove them.

## Why they are still needed
- They load resource files that name a script by its old path without a uid: WFC Generator 1.3.2 or older tile sets and settings, files written by `ResourceSaver.Save` outside the editor, hand-written `.tres` files.
- Upgrading the addon does not change those files. Re-saving one in the editor does not help either: the loaded object is the shim class, so Godot writes the shim's path and uid back into the file.

## Removal in 2.0
- A migration tool first (editor menu or a one-off tool scene) that rewrites `ext_resource` lines naming `res://addons/wfc_generator/Resources/*.cs` or `res://addons/multimesh_scatter/Resources/ScatterSettings.cs` (by path or by the shim uids `uid://bu5gpb3t1gw2f`, `uid://c47gw2abwolly`, `uid://6q5g6kennjxq`, `uid://dtyne4qwqdouy`) to the real scripts and their uids. Run it in the 1.x line, so users can migrate before 2.0.
- Then delete both folders and the legacy list in `tools/package_addon.sh`, and turn the compatibility tests (`wfc_1_3_2` fixture, `WfcAdapterTests.Tiles_saved_by_1_0_load_with_weight_one`, `PresetTests.Settings_saved_at_the_old_path_load`) into migration tests.
- CHANGELOG *Updating* note in both addons: run the migration tool on 1.x before updating.
- godoodungeon's check of `addons/wfc_generator/plugin.cfg` (old WFC Generator before 1.3.3) is separate and can stay.

## Decision update (user, 2026-11-16)
The only project that uses the addons has neither shim folder and loads cleanly; release downloads (1 to 3 per zip, 0 stars, 0 forks) show no other users. Remove the shims now, as patch releases godoowave 1.4.3 and godooscatter 1.3.2. No migration tool.

## Tasks
- [x] Delete `addons/wfc_generator/` and `addons/multimesh_scatter/`, and the legacy list in `tools/package_addon.sh`
- [x] Tests: drop the old-path ScatterSettings test; point the 1.0 tile test and the 1.3.2 regression fixture at the real scripts
- [x] Docs: CLAUDE.md, CONVENTIONS.md, READMEs, CHANGELOGs, briefs
- [x] Build, tests, headless load, package + smoke test

## Results (2026-11-16)
- Deleted `addons/wfc_generator/` and `addons/multimesh_scatter/`, and the legacy list in `tools/package_addon.sh`. Zips now hold only `addons/<addon>/` and `addons/editor_tools_common/`.
- Tests: the path-only case of `PresetTests` is gone; its uid case stays as `Settings_saved_in_the_editor_at_the_old_path_load_by_uid` (editor-saved files keep loading). `WfcAdapterTests` (1.0 tile, weight 1) and the `wfc_1_3_2` fixture (`WfcSharedSolverMoveTests`, same cells as 1.3.2) now name the moved scripts.
- godoowave 1.4.3 and godooscatter 1.3.2: plugin.cfg, CHANGELOG *Removed* + *Updating* (the one-line path fix), README install and update text, root README, briefs §15 / §11, CLAUDE.md, CONVENTIONS.md (legacy subclass rule kept, with the removal note).
- godoodungeon's check of `addons/wfc_generator/plugin.cfg` stays: it detects a WFC Generator older than 1.3.3, not the shims.

## Verification
- `dotnet build`: 0 warnings, 0 errors. Core tests 501/501, engine tests 126/126. Headless editor load: no errors.
- Packaged `godoowave-1.4.3` and `godooscatter-1.3.2` from a temporary commit; both smoke tests loaded cleanly.

## Review
- Release review: approve, then merge and tag `godoowave-v1.4.3` and `godooscatter-v1.3.2`.

Approved by the board on 2026-11-16.
