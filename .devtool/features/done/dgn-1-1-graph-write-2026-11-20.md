---
id: "dgn-1-1-graph-write-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T21:30:00.000Z"
completedAt: "2026-11-20T21:30:00.000Z"
labels: ["v1.1.0", "adapters", "editor"]
order: "g4"
---
# godoodungeon 1.1: Store the graph on Generate, Clear and undo

Generate writes the graph to GridMap metadata `godoodads_dungeon_graph` in the same undo action as cells and record; Clear Dungeon removes it; undo/redo restore the previous graph or none. Dialog warns before Generate when the old graph has `Data` set. Status adds "graph: N rooms, M corridors (K loops)".

Brief: §12.2, §12.4.

## Done when
- Engine tests: Generate → graph present and matches acceptance 1 (§12.6); undo → previous graph or no meta; Clear → none; undo of Clear → back.
- Manual (review): status line, Data warning, undo in the editor.

## Results (2026-11-20)
- `GodoodungeonPlugin.Commit`: the Generate undo action also does `ApplyGraph(map, new graph)` and undoes `ApplyGraph(map, previous graph or null)`, next to the settings and record. `OnClearRequested` does `ApplyGraph(map, null)` and undoes it with the stored graph; a GridMap with only a graph left counts as something to clear.
- Status after Generate: "Stored the dungeon graph in the GridMap's metadata (godoodads_dungeon_graph): N rooms, M corridors (K loops)." When the replaced graph had `Data`, a warning says how many rooms and corridors had it and that Ctrl+Z brings it back. The viewer cannot block on a confirm, so it is a note after the fact, not a question before. Clear says it removed the graph (with its Data, if any).
- `DungeonGridMap.Graph(result)` and `DungeonGridMap.EntriesWithData(graph)` in Adapters.
- Engine test `The_graph_of_a_generated_dungeon_matches_its_cells` (§12.6 item 1, 3 seeds on a written GridMap): room and corridor counts, every footprint cell recorded and mapped to its room, door cells hold the door tile, every route cell recorded, not in a room, and found by `CorridorsAtCell`. Engine suite 133/133, Core 510/510, headless editor load clean.
- **Not testable headless:** the undo/redo itself runs through `EditorUndoRedoManager`. It follows the same pattern as `ApplySettings`.

## Manual check (board)
Build, **focus the Godot window** so it reloads the assembly, then:
1. Open `demo/godoodungeon/` dungeon scene, select the GridMap, Generate: the status shows the graph line; Inspector → Metadata shows `godoodads_dungeon_graph` with Rooms and Corridors.
2. Ctrl+Z: the graph goes back to the previous one (or none); Ctrl+Shift+Z brings the new one back.
3. Set `Data` on a room (e.g. `role` = `boss`), Generate again: the warning names 1 entry; Ctrl+Z restores the room's Data.
4. Clear Dungeon: graph removed from metadata; Ctrl+Z brings it back.
5. Save the scene, reopen: graph still there.

## Board check (2026-11-20)
Passed at the release review: status line, metadata, undo/redo of Generate, Data warning with restore by undo, Clear Dungeon and its undo, save and reopen.
