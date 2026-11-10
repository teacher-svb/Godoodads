---
id: "wfc-1-1-regenerate-read-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T09:01:43.000Z"
completedAt: "2026-11-10T09:01:43.000Z"
labels: ["engine"]
order: "c4"
---
# WFC 1.1: Free previously generated cells on Generate

When reading fixed cells, a cell that matches the stored record (same item and orientation) is free again. A cell that differs is hand-placed and stays fixed. Clear Bounds First is unchanged.

Brief: §11.2.

## Done when
- Engine tests: an unchanged generated cell is freed, an edited one is kept, a hand-placed one is kept, cells outside the bounds are untouched.
- Generating twice with different seeds gives the second result without undoing (manual check).

## Result
- `ReadFixed` takes the previous record. A cell that still matches it is free again (counted as Replaced). A recorded cell that differs is kept as hand-placed (counted as Edited). Clear Bounds First is unchanged.
- Engine tests, passing: an unchanged generated cell is freed, edited and hand-placed cells are kept, an erased cell is free, the record outside the bounds is kept, and generating with seed 1 then seed 2 gives exactly the seed-2 result on a map with one placed cell.
- Waiting on: the manual check in the editor.

## Result
- Manual check passed (2026-11-10): Generate twice replaces the first result, hand edits are kept with the edited-cells message, and undo then Generate behaves.
