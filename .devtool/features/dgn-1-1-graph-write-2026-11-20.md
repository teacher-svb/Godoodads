---
id: "dgn-1-1-graph-write-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T10:00:00.000Z"
modified: "2026-11-20T10:00:00.000Z"
completedAt: null
labels: ["v1.1.0", "adapters", "editor"]
order: "g4"
---
# godoodungeon 1.1: Store the graph on Generate, Clear and undo

Generate writes the graph to GridMap metadata `godoodads_dungeon_graph` in the same undo action as cells and record; Clear Dungeon removes it; undo/redo restore the previous graph or none. Dialog warns before Generate when the old graph has `Data` set. Status adds "graph: N rooms, M corridors (K loops)".

Brief: §12.2, §12.4.

## Done when
- Engine tests: Generate → graph present and matches acceptance 1 (§12.6); undo → previous graph or no meta; Clear → none; undo of Clear → back.
- Manual (review): status line, Data warning, undo in the editor.
