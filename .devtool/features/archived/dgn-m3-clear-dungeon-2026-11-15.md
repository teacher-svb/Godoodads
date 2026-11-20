---
id: "dgn-m3-clear-dungeon-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:39:15.000Z"
completedAt: "2026-11-15T18:39:15.000Z"
labels: ["v1.0.0", "m3", "editor"]
order: "d6"
---
# DGN M3: Clear Dungeon

Clear Dungeon removes the recorded cells and markers in one undo action and keeps hand-placed cells.

Brief: §4.6.

## Done when
- Engine test for the cells; manual check of undo and redo.

## Results
Branch `godoodungeon-m1`.

- The **Clear Dungeon** button (top of the settings) removes the record's still-unchanged cells and the markers on them, and empties the record, as one undo step ("Clear Dungeon"). Edited and hand-placed cells stay. The status reports what was cleared and kept. It asks to stop a running generation first. With no dungeon it says there is nothing to clear.
- Engine test `Clear_dungeon_removes_recorded_cells_and_keeps_edits_and_hand_placed` (dgn-m2-gridmap-write) covers the cells. The probe on dgn-m3-dialog covers the button, undo and the 30 hand-placed crates staying.

## Manual check (board)
Generate, then press Clear Dungeon. The dungeon goes and the crates stay. Then undo (the dungeon is back) and redo (it goes again).

## Board check passed (2026-11-15)
After a Generate, Clear Dungeon removes the dungeon and the hand-placed crates stay. Ctrl+Z brings the dungeon back, and Ctrl+Shift+Z clears it again.
