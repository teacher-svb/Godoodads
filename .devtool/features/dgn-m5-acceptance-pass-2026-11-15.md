---
id: "dgn-m5-acceptance-pass-2026-11-15"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:20:16.000Z"
completedAt: null
labels: ["v1.0.0", "m5", "acceptance"]
order: "f1"
---
# DGN M5: Acceptance pass

Automated tests for every automated criterion in §8 (determinism, connectivity over 50 seeds, closed rooms, layout rules, hand-placed cells, user assets, reliability), and the manual checks recorded on this card.

Brief: §8.

## Done when
- Every criterion is listed on this card with its test or its manual result.

## Results
Branch `godoodungeon-m1`. Core tests 366 passed, engine tests 103 passed, build with 0 warnings.

| §8 criterion | How it is checked | Result |
|---|---|---|
| **Determinism** | `DungeonAcceptanceTests.Determinism_…`: 10 seeds at 64×64 / 20 rooms, with a line of hand-placed cells; rooms, doors and every solve's cells are identical across two runs. Also `DungeonJobTests.The_same_seed_gives_the_same_cells`, `DungeonPreviewTests` | pass |
| **Connectivity** (50 seeds) | `DungeonAcceptanceTests.Connectivity_…`: room-aware flood fill (`DungeonConnectivity`) over 50 seeds at 64×64 / 20 rooms; every run also does the check itself (`InternalError` otherwise) | pass |
| **Closed rooms**, every door leads to a corridor | `DungeonAcceptanceTests.Closed_rooms_…`: 50 seeds; every outward face of every room cell is `outside+`, none inwards; the corridor cell outside every door has a path face towards it | pass |
| **Layout rules** | `DungeonAcceptanceTests.Layout_rules_…` (50 seeds at the target size), `DungeonDoorTests` (450 runs over 6 settings), `DungeonRoomPlacementTests` (200 seeds) | pass |
| **Hand-placed cells** unchanged after Generate and Clear Dungeon | Engine: `DungeonGridMapTests.Hand_placed_cells_are_unchanged`, `…Regenerate_replaces_only_recorded_cells_and_keeps_edits`, `…Clear_dungeon_…`; `DungeonDemoTests` (no generated cell on the 29 hand-placed obstacle cells over 20 seeds); board check on `dgn-m3-clear-dungeon` | pass |
| **WFC unchanged** | `WfcSharedSolverMoveTests` (1.3.2 scene gives the 1.3.2 cells, legacy classes load); all WFC tests pass; WFC Generator 1.3.3 released | pass |
| **User assets** | Engine: `DungeonGridMapTests.The_mesh_library_and_tile_sets_are_unchanged` | pass |
| **Undo** (manual) | Board: generate, undo, redo on `dgn-m3-dialog` / `dgn-m3-clear-dungeon`; engine: `…Undo_restores_every_cell_and_redo_reapplies`; probe on `dgn-m3-dialog` (every cell restored) | pass |
| **Cancel and reload** (manual) | Runs finish too fast to interrupt by hand; the board accepted the automated coverage (`dgn-m3-run-cancel-reload`): probe Stop through the real dialog, `DungeonJobTests.Cancelling_stops_within_one_solve`, reload/scene-close code shared with WFC Generator | accepted |
| **Reliability** (≥ 19 of 20 at the target size, demo tile sets) | Engine: `DungeonDemoTests.The_demo_scene_generates_at_least_19_of_20_seeds`: dungeon_free 20 of 20, dungeon_obstacles 20 of 20 | pass |
| **Performance** (64×64, 20 rooms, < 10 s) | `dgn-m5-performance`: about 20 ms including the GridMap write (headless) | pass, board timing on its machine pending |
