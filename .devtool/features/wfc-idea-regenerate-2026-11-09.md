---
id: "wfc-idea-regenerate-2026-11-09"
status: "backlog"
priority: "medium"
assignee: null
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "editor"]
order: "b2"
---
# WFC 1.1: Regenerate without undoing

Today a second Generate keeps the previous result, because generated cells count as fixed. Users must undo first or use Clear Bounds First, which also removes their own cells.

**Size:** S to M

## Design notes
- Remember which cells the last generation wrote, for example a packed cell list in the GridMap's settings metadata, which is already saved and undoable.
- A **Regenerate** option frees those cells but keeps hand-placed ones. A cell the user edited afterwards should count as hand-placed again.

## Open questions
- Should Regenerate be the default behaviour of Generate?
- How do we detect that a generated cell was edited by hand: compare item and orientation with what was written?
