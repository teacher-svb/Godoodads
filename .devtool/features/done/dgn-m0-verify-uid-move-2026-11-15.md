---
id: "dgn-m0-verify-uid-move-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-15T11:53:32.000Z"
labels: ["v1.0.0", "m0", "spike"]
order: "a1"
---
# DGN M0: Verify moving scripts with their uid

Check brief §10 items 1 and 2 on Godot 4.7 in a scratch copy of the project. Move a `[GlobalClass]` resource script together with its `.uid` file and load a `.tres` and a `.tscn` (with the resource in node metadata) that still use the old path. Then put a second script with the same class name next to it and record what the editor reports.

Brief: §4.1, §6.3, §10.

## Done when
- Findings are recorded on this card, with the exact errors for the duplicate class case.
- The brief is corrected if an assumption is wrong, and the move card gets the steps that keep resources loading.

## Results (Godot 4.7, headless, scratch copies of the project)

A check script loaded the 7 WFC demo files and reported the script behind each resource, including `WfcSettings` in GridMap metadata. Two of them (`hills_tileset.tres`, `generate_test.tscn`, saved in the editor) reference the scripts by uid; the other five (made by the demo build scenes) by path only.

| Scenario | uid references | path-only references |
|---|---|---|
| Baseline | load | load |
| S1: scripts moved with `.uid`, old cache, before an editor scan | **fail** (stale cache points to the old path) | **fail** |
| S1: after an editor scan | load, moved script | **fail for good** |
| S2: moved + stubs, no `.godot` cache, before a scan | "invalid UID, using text path" warning, loads the stub | load (stub) |
| S2: after a scan | load, moved script | load (stub) |
| S3: moved (new namespace) + old 1.3.2 copies left in place | "UID duplicate detected" warning per script; uid resolves to the **old** copy | load, old copy |
| S5: moved + stubs at the old paths | load, moved script, no warnings | load, stub |

Findings:
1. **Moving with the `.uid` is not enough.** It works for editor-saved files after a scan, but `ResourceSaver.Save` outside the editor writes no uids at all (checked by resaving `ruin_tileset.tres` from a headless script), so script-made files break.
2. **Stubs fix every case.** `addons/wfc_generator/Resources/<Class>.cs` as `[Tool] public partial class WfcTile : Godoodads.Common.Wfc.WfcTile { }`, in namespace `Godoodads.WfcGenerator.Legacy` (in `Godoodads.WfcGenerator` the stub would shadow the moved type for the WFC code), with a **new** `.uid` and no `[GlobalClass]`. Path-only files load the subclass, which the shared code accepts.
3. **Duplicates do not error but load the wrong types.** With an old copy next to the moved one, both carry the same uid, and Godot picks the old file; the global class cache lists `WfcTile` twice. So a godoodungeon install next to WFC 1.3.2 is broken. Extracting 1.3.3 over 1.3.2 is safe, because the stubs overwrite the old scripts and their `.uid` files.
4. **The namespace change does not affect loading** (scripts resolve by uid or path). Same-namespace duplicates would be a C# compile error, so the new namespace is required.

## Brief changes
- §4.1: legacy stubs, rerunning the demo build scenes, the version warning for WFC Generator older than 1.3.3.
- §6.3 and §10.1–2 rewritten with these results.
