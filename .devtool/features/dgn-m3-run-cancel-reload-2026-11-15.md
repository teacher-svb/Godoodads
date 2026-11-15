---
id: "dgn-m3-run-cancel-reload-2026-11-15"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:18:30.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor"]
order: "d4"
---
# DGN M3: Background run, cancel and reload

Generate runs the `DungeonJob` in the background with progress per stage. Cancel restores the earlier state. The run stops before an assembly reload, when the GridMap is removed and when the scene closes.

Brief: §4.5.

## Done when
- Manual checks: cancel mid-run; rebuild during a run; close the scene during a run. None leave cells behind or hang the editor.

## Results
Branch `godoodungeon-m1`.

- **Generate.** It checks the settings and tile sets (`DungeonTileSets.Check`, every error in the status, the first one also as a toast), picks a seed if Randomise Seed is on, reads the record and hand-placed cells, and starts a `DungeonJob`. `_Process` shows its stage every 0.1 s, for example "Layout 1: room 3 of 12 (0.4 s)".
- **Finish.** On Solved, it commits one undo action: cells (do/undo), the stored settings, the record, and the markers in `DungeonMarkers` (old ones replaced). The status gives rooms, doors, routes, time and seed, plus layout warnings and tile set warnings. On Failed, every attempt's message appears. On Invalid or InternalError, the errors appear. A failed randomised run restores the seed and does not pick it again.
- **Stopping.** Stop, closing the dialog, an assembly reload (`OnBeforeSerialize`) and the GridMap leaving the scene (which also covers closing the scene) all cancel the job, wait for its thread and write nothing.

Probe (headless editor, scratch copy). It opened `prototype_m2_dungeon.tscn`, selected the GridMap, pressed **GridMap → Generate Dungeon (GDDS)**, then drove the dialog:
- Open: the dialog shows the stored settings (seed 2026, 12 rooms). Status "Target: GridMap".
- Seed 77, Generate: "Generated 12 rooms, 20 doors and 19 corridor routes in 0.1 s. Seed 77." The GridMap changed, the record holds 646 cells, and the stored settings have seed 77.
- Undo (editor history): every cell restored, stored seed back to 2026. Redo reapplies.
- Clear Dungeon: "Cleared 646 cells of the dungeon." The 30 hand-placed crates stay and the record is empty. Undo brings all 676 cells back.
- A 220×220, 150-room run, Stop pressed at once: the button is back to Generate, "Generation stopped. The GridMap was not changed.", and the cells are unchanged.
- Two engine messages at the probe's forced quit (`HotReloadAssemblyWatcher`, debugger plugin) come from Godot itself, not from godoodungeon.

## Manual check (board)
1. Cancel mid-run: set the bounds to about 200 × 1 × 200 with 120 rooms, Generate, then Stop. The GridMap is unchanged.
2. Rebuild during a run (start the big run, rebuild, focus Godot). The status says the run was cancelled, and the editor does not hang.
3. Close the scene during a big run: no cells are left behind and the editor does not hang.
