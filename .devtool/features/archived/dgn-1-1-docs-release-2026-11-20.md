---
id: "dgn-1-1-docs-release-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T19:00:00.000Z"
completedAt: "2026-11-20T19:00:00.000Z"
labels: ["v1.1.0", "docs", "release"]
order: "g7"
---
# godoodungeon 1.1: README, CHANGELOG, version

README "Dungeon graph" section: data, junction caveat (§12.1), room numbering, API, GDScript and C# examples, addon folder needed in exports. CHANGELOG 1.1.0, plugin.cfg 1.1.0, CLAUDE.md project line. Acceptance pass of §12.6 recorded here.

Brief: §12.5, §12.6.

## Results (2026-11-20)
- `addons/godoodungeon/README.md`: new *Dungeon graph* section under Use (what it stores and where, addon folder needed in exports, the junction caveat, room numbering, a member table, `Data`, GDScript and C# examples); Preview Layout numbers, the graph status line, How it works step 8, the third demo scene and the Limitations line on roles updated.
- `CHANGELOG.md` 1.1.0 (dated 2026-11-20; change the date if the tag is pushed later), `plugin.cfg` 1.1.0, root README table, download example and godoodungeon backlog line, CLAUDE.md project paragraph. The root README and CLAUDE.md also had uncommitted edits from another session (godoowave 1.5 pin boxes); only this card's lines were committed, theirs stay in the working tree.

## Acceptance pass, brief §12.6
| # | Criterion | Result |
|---|---|---|
| 1 | Graph matches the generated GridMap | Pass: engine `The_graph_of_a_generated_dungeon_matches_its_cells` (3 seeds, written GridMap) and Core `A_generated_graph_has_every_room_and_one_corridor_per_edge_from_door_to_door` (6 seeds). |
| 2 | Save/reopen keeps the graph; undo of Generate and Clear | Save/reopen: pass (`A_graph_stored_on_a_gridmap_survives_a_save_and_reload_of_the_scene`, incl. `Data`). Undo/redo: **manual**, on `dgn-1-1-graph-write` (EditorUndoRedoManager cannot run headless). |
| 3 | Every query from GDScript equals C# | Pass: `Gdscript_reads_the_graph_from_the_gridmap_and_gets_the_csharp_answers` and `Every_query_answers_the_same_through_variant_calls`. |
| 4 | Core tests: distances, paths, diameter, ties; cell lookups on every cell | Pass: `DungeonGraphTests` (9 tests). |
| 5 | Same seed and settings, same graph | Pass: Core `The_same_seed_gives_the_same_graph`; the demo test regenerates the baked graph from its stored settings. |
| 6 | Loads in a game without the plugin and answers `RoomAtCell` | Pass as a headless **game run** of `dungeon_graph.tscn` (no editor, so no plugins), and the demo test checks `RoomAtCell`. Not tried as a real export: no 4.7 export templates or presets on this machine. |
